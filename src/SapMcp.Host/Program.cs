using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SapMcp.Core;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8; Console.OutputEncoding = new UTF8Encoding(false);
        string Value(string key, string fallback = "") { var i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        try
        {
            var path = Value("--config", SettingsStore.DefaultPath);
            var store = new SettingsStore(path); var settings = await store.LoadAsync();
            UiText.SetLanguage(settings.UiLanguage);
            if (args.Contains("--unregister"))
            {
                foreach (var registration in settings.Registrations) await ClientIntegration.RemoveOwnedAsync(registration, settings.HostExecutable);
                return 0;
            }
            if (args.Contains("--diagnose"))
            {
                var service = new PrerequisiteService(new PackageService(Path.Combine(AppContext.BaseDirectory, "payload")));
                var checks = await service.CheckAsync(settings);
                Console.WriteLine(JsonSerializer.Serialize(new { schemaVersion = 1, version = "0.1.0-preview.3", checks }, JsonDefaults.Options));
                return checks.Any(c => c.State is CheckState.Blocked or CheckState.Missing or CheckState.Waiting) ? 2 : 0;
            }
            var component = Value("--component");
            if (component is not ("rfc" or "adt")) throw new InvalidDataException(UiText.Get("Text249"));
            var sdk = settings.Rfc.Selected && settings.Rfc.Installed ? await PrerequisiteService.ValidateSdkAsync(settings) : null;
            var rfcReady = sdk?.State == CheckState.Ready;
            if (component == "rfc" && sdk is { State: not CheckState.Ready }) throw new SdkUnavailableException(sdk);
            var start = LaunchConfiguration.Create(settings, component, rfcReady);
            start.Environment["SAP_ADT_RFC_MCP_ARGS"] = JsonSerializer.Serialize(new[] { "--component", "rfc", "--config", path });
            return await ProxyAsync(start, settings.Component(component), component, rfcReady);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(JsonSerializer.Serialize(new { code = "SAP_MCP_START_FAILED", message = Redaction.Text(ex.Message), diagnostic = (ex as SdkUnavailableException)?.Diagnostic }, JsonDefaults.Options));
            return 2;
        }
    }

    private static async Task<int> ProxyAsync(ProcessStartInfo start, ComponentSettings options, string component, bool rfcReady)
    {
        using var protocolInput = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false), false, 8192, true);
        using var protocolOutput = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false), 8192, true) { NewLine = "\n", AutoFlush = true };
        using var child = Process.Start(start) ?? throw new IOException(UiText.Get("Text244"));
        using var stop = new CancellationTokenSource();
        var outputGate = new SemaphoreSlim(1, 1);
        var operations = new RollingLog(options.LogDirectory, options.LogLevel, options.MaxFileBytes, options.MaxFiles, options.RetentionDays, "operations.jsonl");
        var sessions = new RollingLog(options.LogDirectory, options.LogLevel, options.MaxFileBytes, options.MaxFiles, options.RetentionDays, "sessions.jsonl");
        var pending = new ConcurrentDictionary<string, (string Method, string? Tool, string? Target, Stopwatch Clock)>();

        async Task Log(RollingLog log, LogLevel level, string name, object? detail = null)
        {
            try { await log.WriteAsync(level, name, detail); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { await Console.Error.WriteLineAsync(UiText.Get("Text245")); }
        }
        async Task Output(string line)
        {
            await outputGate.WaitAsync();
            try { await protocolOutput.WriteLineAsync(line); await protocolOutput.FlushAsync(); }
            finally { outputGate.Release(); }
        }
        await Log(sessions, LogLevel.Info, "session.started", new { component, version = options.Version, processId = child.Id });

        var incoming = Task.Run(async () =>
        {
            string? line;
            while ((line = await protocolInput.ReadLineAsync(stop.Token)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Length > 16 * 1024 * 1024) throw new IOException(UiText.Get("Text246"));
                using var document = JsonDocument.Parse(line); var request = document.RootElement;
                var method = request.TryGetProperty("method", out var m) ? m.GetString() ?? "" : "";
                JsonElement? id = request.TryGetProperty("id", out var ident) ? ident : null;
                string? tool = null, target = null; JsonElement? arguments = null;
                if (method == "tools/call" && request.TryGetProperty("params", out var param))
                {
                    tool = param.TryGetProperty("name", out var name) ? name.GetString() : null;
                    arguments = param.TryGetProperty("arguments", out var a) ? a : null;
                    if (arguments is { } ar && ar.TryGetProperty("target_key", out var t)) target = t.GetString();
                }
                if (component == "adt" && !rfcReady && LaunchConfiguration.RequiresRfc(tool, arguments))
                {
                    if (id is { } rid)
                        await Output(JsonSerializer.Serialize(new { jsonrpc = "2.0", id = rid, result = new { isError = true, content = new[] { new { type = "text", text = UiText.Get("Text247") } } } }));
                    await Log(operations, LogLevel.Warn, "tool.blocked", new { tool, target, code = "RFC_UNAVAILABLE" });
                    continue;
                }
                if (id is { } key) pending[key.GetRawText()] = (method, tool, target, Stopwatch.StartNew());
                if (method == "tools/call") await Log(operations, LogLevel.Debug, "tool.started", new { tool, target });
                // Trace captures metadata only. Tool payloads may contain business data or secrets with arbitrary field names.
                await Log(operations, LogLevel.Trace, "protocol.request", new { method, tool, target });
                await child.StandardInput.WriteLineAsync(line); await child.StandardInput.FlushAsync();
            }
            child.StandardInput.Close();
        });
        var outgoing = Task.Run(async () =>
        {
            string? line;
            while ((line = await child.StandardOutput.ReadLineAsync(stop.Token)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Length > 32 * 1024 * 1024) throw new IOException(UiText.Get("Text016"));
                using var document = JsonDocument.Parse(line); var response = document.RootElement;
                if (response.TryGetProperty("id", out var id) && pending.TryRemove(id.GetRawText(), out var request))
                {
                    var failure = response.TryGetProperty("error", out _) || (response.TryGetProperty("result", out var result) && result.TryGetProperty("isError", out var isError) && isError.ValueKind == JsonValueKind.True);
                    if (request.Method == "tools/call")
                        await Log(operations, failure ? LogLevel.Error : LogLevel.Info, failure ? "tool.failed" : "tool.completed",
                            new { tool = request.Tool, target = request.Target, durationMs = request.Clock.ElapsedMilliseconds, success = !failure });
                }
                await Output(line);
            }
        });
        var errorReader = Task.Run(async () =>
        {
            // Legacy upstream stderr is unstructured and may contain credentials. Never persist or forward it verbatim.
            while (await child.StandardError.ReadLineAsync(stop.Token) != null)
                await Log(operations, LogLevel.Debug, "upstream.stderr", new { message = UiText.Get("Text248") });
        });
        try
        {
            var finished = await Task.WhenAny(incoming, outgoing, child.WaitForExitAsync());
            if (finished.IsFaulted) await finished;
            if (finished == incoming)
            {
                using var grace = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                try { await child.WaitForExitAsync(grace.Token); } catch (OperationCanceledException) { if (!child.HasExited) child.Kill(true); }
            }
            else if (!child.HasExited) child.Kill(true);
            await child.WaitForExitAsync();
            await outgoing;
            if (child.ExitCode != 0)
            {
                var diagnostic = new ProcessDiagnostic("UPSTREAM_PROCESS_EXIT", "upstream", "ProcessExit", child.ExitCode);
                await Log(operations, LogLevel.Error, "process.failed", diagnostic);
                await Console.Error.WriteLineAsync(JsonSerializer.Serialize(new { code = "SAP_MCP_PROCESS_FAILED", diagnostic }, JsonDefaults.Options));
            }
            return child.ExitCode;
        }
        finally
        {
            stop.Cancel();
            if (!child.HasExited) child.Kill(true);
            try { await Task.WhenAll(incoming, outgoing, errorReader).WaitAsync(TimeSpan.FromSeconds(2)); } catch (Exception ex) when (ex is OperationCanceledException or TimeoutException) { }
            await Log(sessions, LogLevel.Info, "session.closed", new { component });
            outputGate.Dispose();
        }
    }
}
