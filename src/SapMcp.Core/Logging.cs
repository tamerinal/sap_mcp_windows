using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SapMcp.Core;

public static partial class Redaction
{
    private static readonly string[] Keys = ["password", "passwd", "secret", "secretref", "authorization", "cookie", "token", "credentials", "connection", "source"];
    [GeneratedRegex(@"(?i)(\b(?:password|passwd|authorization|cookie|token|secret)\b\s*[:=]\s*)(?:""[^""]*""|'[^']*'|[^\s,;}]+)")]
    private static partial Regex SecretPattern();
    [GeneratedRegex(@"(?i)\b(Basic|Bearer)\s+[A-Za-z0-9+/=._~\-]+")]
    private static partial Regex AuthorizationPattern();
    [GeneratedRegex(@"(?i)(https?://)[^\s/@]+:[^\s/@]+@")]
    private static partial Regex UrlPasswordPattern();
    public static string Text(string value) => UrlPasswordPattern().Replace(AuthorizationPattern().Replace(SecretPattern().Replace(value, "$1[redacted]"), "$1 [redacted]"), "$1[redacted]@");
    public static JsonNode? Sanitize(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var result = new JsonObject();
            foreach (var (key, value) in obj)
                result[key] = Keys.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase)) ? JsonValue.Create("[redacted]") : Sanitize(value);
            return result;
        }
        if (node is JsonArray array) return new JsonArray(array.Select(Sanitize).ToArray());
        return node is JsonValue val && val.TryGetValue<string>(out var text) ? JsonValue.Create(Text(text)) : node?.DeepClone();
    }
}

public sealed class RollingLog(string directory, LogLevel threshold = LogLevel.Info, long maxBytes = 10 * 1024 * 1024, int maxFiles = 10, int retentionDays = 30, string fileName = "manager.jsonl")
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);
    public async Task WriteAsync(LogLevel level, string eventName, object? detail = null, CancellationToken ct = default)
    {
        if (threshold == LogLevel.Off || level < threshold) return;
        var gate = Gates.GetOrAdd(Path.GetFullPath(directory), _ => new(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(directory);
            var lockPath = Path.Combine(directory, ".writer.lock");
            await using var lease = await AcquireAsync(lockPath, ct);
            var file = Path.Combine(directory, fileName);
            if (File.Exists(file) && new FileInfo(file).Length >= maxBytes)
            {
                var oldest = file + "." + maxFiles;
                if (File.Exists(oldest)) File.Delete(oldest);
                for (var i = maxFiles - 1; i >= 1; i--)
                    if (File.Exists(file + "." + i)) File.Move(file + "." + i, file + "." + (i + 1), true);
                File.Move(file, file + ".1", true);
            }
            foreach (var old in Directory.EnumerateFiles(directory, fileName + "*"))
                if (File.GetLastWriteTimeUtc(old) < DateTime.UtcNow.AddDays(-retentionDays)) File.Delete(old);
            var entry = new JsonObject {
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"), ["level"] = level.ToString(), ["event"] = eventName,
                ["detail"] = detail is null ? null : Redaction.Sanitize(JsonSerializer.SerializeToNode(detail, JsonDefaults.Options))
            };
            await File.AppendAllTextAsync(file, entry.ToJsonString() + Environment.NewLine, ct);
        }
        finally { gate.Release(); }
    }

    private static async Task<FileStream> AcquireAsync(string file, CancellationToken ct)
    {
        for (var i = 0; ; i++)
        {
            ct.ThrowIfCancellationRequested();
            try { return new(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (i < 100) { await Task.Delay(50, ct); }
        }
    }
}
