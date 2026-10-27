using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SapMcp.Core;

public sealed class McpSession : IAsyncDisposable
{
    private static readonly JsonSerializerOptions WireOptions = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
    private readonly Process _process;
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly SemaphoreSlim _writer = new(1, 1);
    private readonly Task _reader;
    private readonly Task _stderr;
    private ProcessDiagnostic? _diagnostic;
    private int _sequence;

    private McpSession(Process process)
    {
        _process = process;
        _stderr = Task.Run(async () =>
        {
            try
            {
                while (await _process.StandardError.ReadLineAsync(_stop.Token) is { } line)
                    if (ReadHostDiagnostic(line) is { } diagnostic) _diagnostic = diagnostic;
            }
            catch (OperationCanceledException) { }
        });
        _reader = Task.Run(ReadAsync);
    }

    internal static ProcessDiagnostic? ReadHostDiagnostic(string line)
    {
        if (line.Length > 8192) return null;
        try
        {
            using var doc = JsonDocument.Parse(line); var root = doc.RootElement;
            if (root.GetProperty("code").GetString() is not ("SAP_MCP_START_FAILED" or "SAP_MCP_PROCESS_FAILED")) return null;
            var diagnostic = root.GetProperty("diagnostic").Deserialize<ProcessDiagnostic>(JsonDefaults.Options);
            return diagnostic is { IsSafe: true } ? diagnostic : null;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { return null; }
    }

    public static async Task<McpSession> StartAsync(AppSettings settings, string configPath, string component, CancellationToken ct = default, string? profileId = null)
    {
        var start = new ProcessStartInfo(settings.HostExecutable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardInputEncoding = new System.Text.UTF8Encoding(false), StandardOutputEncoding = System.Text.Encoding.UTF8, StandardErrorEncoding = System.Text.Encoding.UTF8 };
        foreach (var arg in new[] { "--component", component, "--config", configPath }) start.ArgumentList.Add(arg);
        if (profileId != null) { start.ArgumentList.Add("--profile"); start.ArgumentList.Add(profileId); }
        var session = new McpSession(Process.Start(start) ?? throw new IOException(UiText.Get("Text014")));
        try
        {
            await session.RequestAsync("initialize", new { protocolVersion = "2024-11-05", capabilities = new { }, clientInfo = new { name = "SapMcpManager", version = "0.1.0" } }, ct);
            await session.WriteAsync(new { jsonrpc = "2.0", method = "notifications/initialized" }, ct);
            return session;
        }
        catch { await session.DisposeAsync(); throw; }
    }

    public async Task<JsonElement> RequestAsync(string method, object? parameters = null, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _sequence);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;
        try
        {
            await WriteAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, ct);
            var response = await completion.Task.WaitAsync(TimeSpan.FromSeconds(120), ct);
            if (response.TryGetProperty("error", out _)) throw new IOException(UiText.Get("Text015"));
            return response.TryGetProperty("result", out var result) ? result.Clone() : response;
        }
        finally { _pending.TryRemove(id, out _); }
    }

    private async Task WriteAsync(object message, CancellationToken ct)
    {
        await _writer.WaitAsync(ct);
        try { await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(message, WireOptions).AsMemory(), ct); await _process.StandardInput.FlushAsync(ct); }
        finally { _writer.Release(); }
    }

    private async Task ReadAsync()
    {
        try
        {
            string? line;
            while ((line = await _process.StandardOutput.ReadLineAsync(_stop.Token)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Length > 16 * 1024 * 1024) throw new IOException(UiText.Get("Text016"));
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("id", out var id) && id.TryGetInt32(out var number) && _pending.TryGetValue(number, out var pending))
                    pending.TrySetResult(doc.RootElement.Clone());
            }
            try { await Task.WhenAll(_process.WaitForExitAsync(_stop.Token), _stderr).WaitAsync(TimeSpan.FromSeconds(2), _stop.Token); }
            catch (TimeoutException) { }
            var detail = _diagnostic ?? (_process.HasExited ? new ProcessDiagnostic("HOST_PROCESS_EXIT", "host", "ProcessExit", _process.ExitCode) : null);
            var message = detail?.Code == "ADT_PACKAGE_UPGRADE_REQUIRED" ? UiText.Get("Adt.PackageUpgrade") : detail == null ? UiText.Get("Text017") : UiText.Get("Sdk.HostFailed", detail.Summary);
            foreach (var pending in _pending.Values) pending.TrySetException(new IOException(message));
        }
        catch (Exception ex) { foreach (var pending in _pending.Values) pending.TrySetException(ex); }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        try { _process.StandardInput.Close(); if (!_process.HasExited) { using var timeout = new CancellationTokenSource(1500); try { await _process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { _process.Kill(true); } } } catch (InvalidOperationException) { }
        await Task.WhenAll(_reader, _stderr);
        _process.Dispose(); _stop.Dispose(); _writer.Dispose();
    }
}
