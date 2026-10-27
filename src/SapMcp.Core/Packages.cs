using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SapMcp.Core;

public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string executable, IEnumerable<string> arguments,
        IDictionary<string, string>? environment = null, TimeSpan? timeout = null, string? stdin = null, CancellationToken ct = default, string? workingDirectory = null)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = stdin != null,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        if (stdin != null) start.StandardInputEncoding = new UTF8Encoding(false);
        if (workingDirectory != null) start.WorkingDirectory = workingDirectory;
        foreach (var arg in arguments) start.ArgumentList.Add(arg);
        if (OperatingSystem.IsWindows() && Path.GetFileName(executable).Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
            start.Environment["PSModulePath"] = WindowsPowerShellModules;
        if (environment != null) foreach (var (k, v) in environment) start.Environment[k] = v;
        using var process = Process.Start(start) ?? throw new IOException(UiText.Get("Text033"));
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));
        var output = ReadBoundedAsync(process.StandardOutput, limit.Token);
        var error = ReadBoundedAsync(process.StandardError, limit.Token);
        try
        {
            if (stdin != null) { await process.StandardInput.WriteAsync(stdin.AsMemory(), limit.Token); process.StandardInput.Close(); }
            await process.WaitForExitAsync(limit.Token);
            return new(process.ExitCode, await output, await error);
        }
        catch
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync(CancellationToken.None);
            try { await Task.WhenAll(output, error); } catch { }
            throw;
        }
    }

    public static string WindowsPowerShellModules => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "Modules");

    private static async Task<string> ReadBoundedAsync(StreamReader reader, CancellationToken ct)
    {
        var buffer = new char[4096]; var text = new StringBuilder(); int n;
        while ((n = await reader.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            if (text.Length + n > 4 * 1024 * 1024) throw new InvalidDataException(UiText.Get("Text034"));
            text.Append(buffer, 0, n);
        }
        return text.ToString();
    }
}

public sealed class PackageService(string payloadDirectory)
{
    public string PayloadDirectory { get; } = Path.GetFullPath(payloadDirectory);
    public PackageCatalog LoadCatalog()
    {
        var file = Path.Combine(PayloadDirectory, "catalog.json");
        if (!File.Exists(file)) throw new InvalidDataException(UiText.Get("Text035"));
        var catalog = JsonSerializer.Deserialize<PackageCatalog>(File.ReadAllText(file), JsonDefaults.Options) ?? throw new InvalidDataException(UiText.Get("Text036"));
        if (catalog.SchemaVersion != 1) throw new InvalidDataException(UiText.Get("Text037"));
        foreach (var a in catalog.Assets)
        {
            if (Path.GetFileName(a.FileName) != a.FileName || string.IsNullOrEmpty(a.FileName) || a.Sha256.Length != 64 || !a.Sha256.All(Uri.IsHexDigit))
                throw new InvalidDataException(UiText.Get("Text038"));
            if (a.Url.Length > 0 && (!Uri.TryCreate(a.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length > 0))
                throw new InvalidDataException(UiText.Get("Text039"));
        }
        return catalog;
    }

    public async Task<string> GetAssetAsync(PackageAsset asset, string cacheDirectory, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        foreach (var folder in new[] { Path.Combine(PayloadDirectory, "packages"), Path.Combine(PayloadDirectory, "cache"), cacheDirectory })
        {
            var existing = Path.Combine(folder, asset.FileName);
            if (File.Exists(existing)) { await VerifyAsync(existing, asset.Sha256, ct); return existing; }
        }
        if (asset.Url.Length == 0) throw new IOException(UiText.Get("Text040" , asset.Id));
        Directory.CreateDirectory(cacheDirectory);
        var target = Path.Combine(cacheDirectory, asset.FileName);
        var temp = target + "." + Guid.NewGuid().ToString("N") + ".download";
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        try
        {
            progress?.Report(UiText.Get("Text041" , asset.Id, asset.Version));
            using var response = await client.GetAsync(asset.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri?.Scheme != "https") throw new IOException(UiText.Get("Text042"));
            await using (var input = await response.Content.ReadAsStreamAsync(ct))
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920]; long total = 0; int n;
                while ((n = await input.ReadAsync(buffer, ct)) > 0)
                {
                    total += n; if (total > 1024L * 1024 * 1024) throw new IOException(UiText.Get("Text043"));
                    await output.WriteAsync(buffer.AsMemory(0, n), ct);
                }
            }
            await VerifyAsync(temp, asset.Sha256, ct);
            File.Move(temp, target, true);
            return target;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public static async Task VerifyAsync(string file, string hash, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(file);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
        if (!actual.Equals(hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(UiText.Get("Text044" , Path.GetFileName(file)));
    }

    public async Task InstallAssetAsync(PackageAsset asset, string destination, string cacheDirectory, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var archive = await GetAssetAsync(asset, cacheDirectory, progress, ct);
        var parent = Path.GetDirectoryName(Path.GetFullPath(destination))!;
        Directory.CreateDirectory(parent);
        var stage = Path.Combine(parent, ".stage-" + Guid.NewGuid().ToString("N"));
        var old = Path.Combine(parent, ".previous-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(stage);
            progress?.Report(UiText.Get("InstallingPackage", asset.Id, asset.Version));
            await Task.Run(() => Extract(archive, stage, asset.Format, asset.StripPrefix, ct), ct);
            await AtomicFile.WriteAsync(Path.Combine(stage, ".sapmcp-package.json"), JsonSerializer.Serialize(asset, JsonDefaults.Options), ct);
            ct.ThrowIfCancellationRequested();
            if (Directory.Exists(destination)) Directory.Move(destination, old);
            try { Directory.Move(stage, destination); }
            catch { if (Directory.Exists(old)) Directory.Move(old, destination); throw; }
            if (Directory.Exists(old)) Directory.Delete(old, true);
        }
        finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
    }

    public static void Extract(string archive, string destination, string format = "zip", string stripPrefix = "", CancellationToken ct = default)
    {
        Directory.CreateDirectory(destination);
        var root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        long total = 0; int count = 0;
        string? Target(string entry, long size)
        {
            ct.ThrowIfCancellationRequested();
            if (++count > 200_000 || (total += size) > 2L * 1024 * 1024 * 1024) throw new InvalidDataException(UiText.Get("Text045"));
            entry = entry.Replace('\\', '/');
            if (stripPrefix.Length > 0)
            {
                var prefix = stripPrefix.Trim('/') + "/";
                if (!entry.StartsWith(prefix, StringComparison.Ordinal)) return null;
                entry = entry[prefix.Length..];
            }
            if (entry.Length == 0) return null;
            if (entry.Contains(':') || entry.StartsWith('/') || entry.Split('/').Any(p => p is "..")) throw new InvalidDataException(UiText.Get("Text046"));
            var target = Path.GetFullPath(Path.Combine(root, entry));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(UiText.Get("Text047"));
            return target;
        }
        if (format == "zip")
        {
            using var zip = ZipFile.OpenRead(archive);
            foreach (var entry in zip.Entries)
            {
                if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000) throw new InvalidDataException(UiText.Get("Text048"));
                var target = Target(entry.FullName, entry.Length); if (target == null) continue;
                if (entry.FullName.EndsWith('/')) Directory.CreateDirectory(target);
                else { Directory.CreateDirectory(Path.GetDirectoryName(target)!); using var input = entry.Open(); using var output = File.Create(target); input.CopyTo(output); }
            }
        }
        else if (format == "tar.gz")
        {
            using var input = File.OpenRead(archive); using var gzip = new GZipStream(input, CompressionMode.Decompress); using var tar = new TarReader(gzip);
            TarEntry? entry;
            while ((entry = tar.GetNextEntry()) != null)
            {
                if (entry.EntryType is not (TarEntryType.Directory or TarEntryType.RegularFile or TarEntryType.V7RegularFile)) throw new InvalidDataException(UiText.Get("Text049"));
                var target = Target(entry.Name, entry.Length); if (target == null) continue;
                if (entry.EntryType == TarEntryType.Directory) Directory.CreateDirectory(target);
                else { Directory.CreateDirectory(Path.GetDirectoryName(target)!); using var output = File.Create(target); entry.DataStream?.CopyTo(output); }
            }
        }
        else throw new InvalidDataException(UiText.Get("Text050"));
    }
}
