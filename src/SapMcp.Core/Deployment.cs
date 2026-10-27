using System.Diagnostics;
using System.Text.Json;

namespace SapMcp.Core;

public sealed class DeploymentService(PackageService packages, PrerequisiteService prerequisites)
{
    public async Task<List<string>> InstallSelectedAsync(AppSettings s, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!s.Rfc.Selected && !s.Adt.Selected) throw new InvalidOperationException(UiText.Get("Text009"));
        var checks = await prerequisites.CheckAsync(s, ct);
        var blockers = checks.Where(r => r.Id is not ("sdk" or "vc-runtime" or "vc2013") && r.State is CheckState.Blocked or CheckState.Missing).ToArray();
        if (blockers.Length > 0) throw new InvalidOperationException(UiText.Get("Text010") + string.Join(", ", blockers.Select(r => r.Name)));
        var catalog = packages.LoadCatalog(); var installed = new List<string>();
        foreach (var name in new[] { "rfc", "adt" }.Where(n => s.Component(n).Selected))
        {
            var asset = catalog.Find(name);
            await packages.InstallAssetAsync(asset, Path.Combine(s.PackageDirectory, name), Path.Combine(s.DataDirectory, "cache"), progress, ct);
            s.Component(name).Installed = true; s.Component(name).Version = asset.Version;
            installed.Add(name);
        }
        if (!File.Exists(s.CredentialsFile)) await ProfileService.MaterializeAsync(s, ct);
        return installed;
    }

    public static async Task RemoveAsync(AppSettings s, string component, CancellationToken ct = default)
    {
        var folder = Path.GetFullPath(Path.Combine(s.PackageDirectory, component));
        if (component is not ("rfc" or "adt") || !folder.StartsWith(Path.GetFullPath(s.PackageDirectory).TrimEnd('\\') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
        if (Directory.Exists(folder)) await Task.Run(() => Directory.Delete(folder, true), ct);
        s.Component(component).Installed = false; s.Component(component).Version = "";
    }
}

public static class LaunchConfiguration
{
    public static ProcessStartInfo Create(AppSettings s, string component, bool rfcReady)
    {
        var options = s.Component(component);
        if (!options.Installed || !options.Selected) throw new InvalidOperationException(UiText.Get("Text011" , component.ToUpperInvariant()));
        if (component == "rfc" && !rfcReady) throw new InvalidOperationException(UiText.Get("Text012"));
        var root = Path.Combine(s.PackageDirectory, component);
        var entry = Path.Combine(root, "dist", "server.js");
        if (!File.Exists(entry) || !File.Exists(s.NodeExecutable)) throw new FileNotFoundException(UiText.Get("Text013"));
        Directory.CreateDirectory(options.LogDirectory);
        var start = new ProcessStartInfo(s.NodeExecutable) { WorkingDirectory = component == "rfc" ? options.LogDirectory : root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(entry);
        void Set(string key, string value) => start.Environment[key] = value;
        if (OperatingSystem.IsWindows()) Set("PSModulePath", ProcessRunner.WindowsPowerShellModules);
        // Use the host's central logging policy for legacy releases too. Server stdout stays protocol-only.
        foreach (var name in new[] { "RFC", "ADT" })
        {
            var c = s.Component(name.ToLowerInvariant()); var p = "SAP_" + name + "_";
            Set(p + "LANDSCAPE_FILE", s.LandscapeFile); Set(p + "CREDENTIALS_FILE", s.CredentialsFile);
            Set(p + "POWERSHELL", s.PowerShellExecutable);
            Set(p + "OPERATION_LOG_ENABLED", "false"); Set(p + "TRACE_LOG_ENABLED", "false");
            Set(p + "OPERATION_LOG_FILE", Path.Combine(c.LogDirectory, "operations.jsonl"));
            Set(p + "SESSION_LOG_FILE", Path.Combine(c.LogDirectory, "sessions.jsonl"));
            Set(p + "LOG_LEVEL", c.LogLevel.ToString());
            Set(p + "LOG_MAX_FILE_BYTES", c.MaxFileBytes.ToString()); Set(p + "LOG_RETENTION_DAYS", c.RetentionDays.ToString());
            Set(p + "LOG_MAX_FILES", c.MaxFiles.ToString());
            Set(p + "BUILD_VERSION", c.Version);
        }
        Set("SAPNWRFC_HOME", s.SdkDirectory); Set("PYTHONIOENCODING", "utf-8"); Set("PYTHONUTF8", "1");
        Set("SAP_RFC_PYTHON_EXECUTABLE", s.PythonExecutable); Set("SAP_RFC_PYTHON_WORKER_FILE", Path.Combine(s.PackageDirectory, "rfc", "python", "worker.py"));
        Set("SAP_RFC_DEFAULT_TIMEOUT_SECONDS", s.Rfc.TimeoutSeconds.ToString()); Set("SAP_RFC_FUNCTION_ALLOWLIST", s.Rfc.FunctionAllowlist);
        Set("SAP_ADT_RFC_ENABLED", rfcReady ? "true" : "false");
        Set("SAP_ADT_RFC_MCP_COMMAND", s.HostExecutable);
        Set("SAP_ADT_RFC_MCP_CWD", Path.Combine(s.PackageDirectory, "rfc"));
        // The host populates the config argument; never start RFC with a machine-specific fallback.
        return start;
    }

    public static bool RequiresRfc(string? tool, JsonElement? arguments)
    {
        if (tool is "activate_function_module" or "update_function_module_source") return true;
        return tool == "create_function_module" && arguments is { } args && args.TryGetProperty("remote_enabled", out var remote) && remote.ValueKind == JsonValueKind.True;
    }
}
