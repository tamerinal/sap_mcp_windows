namespace SapMcp.Core;

public sealed class PrerequisiteService(PackageService packages)
{
    public static Task<Requirement> ValidateSdkAsync(AppSettings s, CancellationToken ct = default)
        => SdkValidation.CheckAsync(s, ct);

    public async Task<List<Requirement>> CheckAsync(AppSettings s, CancellationToken ct = default)
    {
        var result = new List<Requirement>();
        var supported = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19045) && System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X64;
        result.Add(new("windows", UiText.Get("Text250"), Environment.OSVersion.VersionString, supported ? CheckState.Ready : CheckState.Blocked, UiText.Get("Text251")));
        try
        {
            Directory.CreateDirectory(s.DataDirectory);
            var probe = Path.Combine(s.DataDirectory, ".write-" + Guid.NewGuid().ToString("N")); await File.WriteAllTextAsync(probe, "probe", ct); File.Delete(probe);
            var drive = new DriveInfo(Path.GetPathRoot(s.DataDirectory)!);
            result.Add(new("storage", UiText.Get("Text252"), UiText.Get("Text061" , drive.AvailableFreeSpace / 1024 / 1024), drive.AvailableFreeSpace > 1024L * 1024 * 1024 ? CheckState.Ready : CheckState.Blocked, UiText.Get("Text062")));
            var directories = new List<string> { Path.GetDirectoryName(s.CredentialsFile)!, s.SecretsDirectory, s.PackageDirectory };
            if (s.Rfc.Selected) directories.Add(s.Rfc.LogDirectory);
            if (s.Adt.Selected) directories.Add(s.Adt.LogDirectory);
            foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
            { Directory.CreateDirectory(directory); var test = Path.Combine(directory, ".write-" + Guid.NewGuid().ToString("N")); await File.WriteAllTextAsync(test, "probe", ct); File.Delete(test); }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { result.Add(new("storage", UiText.Get("Text252"), "", CheckState.Blocked, UiText.Get("Text063"))); }
        var powershellReady = false;
        if (File.Exists(s.PowerShellExecutable))
        {
            try
            {
                var run = await ProcessRunner.RunAsync(s.PowerShellExecutable, ["-NoProfile", "-NonInteractive", "-Command", "$ErrorActionPreference='Stop'; Import-Module Microsoft.PowerShell.Security; [Console]::Write('ready')"], timeout: TimeSpan.FromSeconds(10), ct: ct);
                powershellReady = run.ExitCode == 0 && run.Output.Trim() == "ready";
            }
            catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or OperationCanceledException) { ct.ThrowIfCancellationRequested(); }
        }
        result.Add(new("powershell", "Windows PowerShell", "DPAPI", powershellReady ? CheckState.Ready : CheckState.Blocked, UiText.Get("Text064")));
        PackageCatalog? catalog = null;
        try { catalog = packages.LoadCatalog(); result.Add(new("catalog", UiText.Get("Text065"), catalog.Version, CheckState.Ready, UiText.Get("Text066"))); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException) { result.Add(new("catalog", UiText.Get("Text065"), "", CheckState.Blocked, ex.Message)); }
        if (!s.Rfc.Selected && !s.Adt.Selected) return result;
        result.Add(await ExecutableAsync("node", "Node.js", s.NodeExecutable, ["--version"], "v24.", catalog?.Assets.Any(a => a.Id == "node") == true, ct));
        if (s.Rfc.Selected)
        {
            result.Add(await ExecutableAsync("python", "Python", s.PythonExecutable, ["--version"], "Python 3.11.", catalog?.Assets.Any(a => a.Id == "python") == true, ct));
            var pyrfcPresent = File.Exists(Path.Combine(Path.GetDirectoryName(s.PythonExecutable)!, "Lib", "site-packages", "pyrfc", "_cyrfc.cp311-win_amd64.pyd"));
            result.Add(new("pyrfc", "PyRFC", catalog?.Assets.FirstOrDefault(a => a.Id == "pyrfc")?.Version ?? "3.4", pyrfcPresent ? CheckState.Ready : CheckState.Missing, UiText.Get("Text067"), catalog?.Assets.Any(a => a.Id == "pyrfc") == true));
            foreach (var (id, name, dll) in new[] { ("vc-runtime", "Visual C++ 2015–2022 x64", "vcruntime140.dll"), ("vc2013", "Visual C++ 2013 x64", "msvcr120.dll") })
            {
                var present = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), dll));
                result.Add(new(id, name, "x64", present ? CheckState.Ready : CheckState.Missing, UiText.Get("Text068"), catalog?.Assets.Any(a => a.Id == id) == true));
            }
            result.Add(await ValidateSdkAsync(s, ct));
        }
        return result;
    }

    private static async Task<Requirement> ExecutableAsync(string id, string name, string file, string[] args, string prefix, bool installable, CancellationToken ct)
    {
        if (!File.Exists(file)) return new(id, name, prefix.TrimEnd('.'), CheckState.Missing, UiText.Get("Text069"), installable);
        try
        {
            var run = await ProcessRunner.RunAsync(file, args, timeout: TimeSpan.FromSeconds(10), ct: ct);
            var version = (run.Output + run.Error).Trim();
            var ok = run.ExitCode == 0 && version.StartsWith(prefix, StringComparison.Ordinal);
            return new(id, name, version, ok ? CheckState.Ready : CheckState.Missing, ok ? UiText.Get("Text070") : UiText.Get("Text071"), installable);
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or OperationCanceledException)
        { ct.ThrowIfCancellationRequested(); return new(id, name, "", CheckState.Missing, UiText.Get("Text072"), installable); }
    }

    public async Task InstallAsync(AppSettings s, IEnumerable<string> ids, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var catalog = packages.LoadCatalog();
        var order = new[] { "node", "python", "pyrfc", "vc-runtime", "vc2013" };
        var chosen = ids.ToHashSet();
        foreach (var id in order.Where(chosen.Contains))
        {
            var asset = catalog.Find(id);
            if (id == "pyrfc" && !File.Exists(s.PythonExecutable)) throw new InvalidOperationException(UiText.Get("Text073"));
            if (id.StartsWith("vc", StringComparison.Ordinal))
            {
                var file = await packages.GetAssetAsync(asset, Path.Combine(s.DataDirectory, "cache"), progress, ct);
                var info = new System.Diagnostics.ProcessStartInfo(file) { UseShellExecute = true, Verb = "runas" };
                foreach (var arg in new[] { "/install", "/passive", "/norestart" }) info.ArgumentList.Add(arg);
                using var process = System.Diagnostics.Process.Start(info) ?? throw new IOException(UiText.Get("Text074"));
                await process.WaitForExitAsync(ct);
                if (process.ExitCode is not (0 or 3010 or 1638)) throw new IOException(UiText.Get("Text075" , process.ExitCode));
                progress?.Report(process.ExitCode == 3010 ? UiText.Get("Text076") : UiText.Get("Text077"));
                continue;
            }
            var destination = id switch { "node" => Path.GetDirectoryName(s.NodeExecutable)!, "python" => Path.GetDirectoryName(s.PythonExecutable)!, "pyrfc" => Path.Combine(Path.GetDirectoryName(s.PythonExecutable)!, "Lib", "site-packages"), _ => throw new InvalidDataException() };
            if (id == "python" && Directory.Exists(Path.Combine(destination, "Lib", "site-packages", "pyrfc"))) chosen.Add("pyrfc");
            await packages.InstallAssetAsync(asset, destination, Path.Combine(s.DataDirectory, "cache"), progress, ct);
        }
    }
}
