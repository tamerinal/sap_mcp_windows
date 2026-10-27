using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace SapMcp.Core;

public static class SdkRuntimeEnvironment
{
    public static Dictionary<string, string> Create(string sdkDirectory, string? inheritedPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PYTHONIOENCODING"] = "utf-8", ["PYTHONUTF8"] = "1"
        };
        if (string.IsNullOrWhiteSpace(sdkDirectory)) return result;
        var home = Path.GetFullPath(sdkDirectory);
        var lib = Path.Combine(home, "lib");
        static string Comparable(string value) => value.Trim().Trim('"').TrimEnd('\\', '/');
        var retained = (string.IsNullOrEmpty(inheritedPath) ? [] : inheritedPath.Split(Path.PathSeparator))
            .Where(p => !Comparable(p).Equals(Comparable(lib), StringComparison.OrdinalIgnoreCase));
        result["SAPNWRFC_HOME"] = home;
        result["PATH"] = string.Join(Path.PathSeparator, new[] { lib }.Concat(retained));
        return result;
    }

    public static void Apply(ProcessStartInfo start, string sdkDirectory)
    {
        foreach (var (key, value) in Create(sdkDirectory, start.Environment["PATH"])) start.Environment[key] = value;
    }
}

// Only fixed identifiers and numeric process metadata cross the diagnostic boundary.
public sealed record ProcessDiagnostic(string Code, string Stage, string ErrorClass, int? ExitCode = null)
{
    public string? ExitCodeHex => ExitCode is { } code ? $"0x{unchecked((uint)code):X8}" : null;
    public string Source => Stage is "host" or "upstream" ? Stage : "sdkProbe";
    public string? Signal => null; // Windows does not report a POSIX termination signal.
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSafe => Codes.Contains(Code) && Stages.Contains(Stage) && Classes.Contains(ErrorClass);
    internal static readonly string[] Codes = ["ADT_PACKAGE_UPGRADE_REQUIRED", "SDK_DLL_MISSING", "SDK_ARCH_MISMATCH", "SDK_DLL_INVALID", "SDK_FILE_UNREADABLE", "SDK_PROBE_FAILED", "SDK_PROBE_TIMEOUT", "SDK_PROBE_INVALID_OUTPUT", "SDK_PROBE_START_FAILED", "HOST_PROCESS_EXIT", "UPSTREAM_PROCESS_EXIT"];
    internal static readonly string[] Stages = ["files", "start", "icu.load", "icu.init", "pyrfc.import", "sdk.version", "sdk.init", "complete", "host", "upstream"];
    internal static readonly string[] Classes = ["CapabilityMismatch", "MissingDll", "Architecture", "BadImageFormat", "IOError", "ProcessExit", "Timeout", "InvalidOutput", "StartFailure", "OSError", "ImportError", "RuntimeError", "UnexpectedResult", "Exception"];
    public string Summary => UiText.Get("Sdk.Diagnostic", Code, Stage, ErrorClass,
        ExitCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—", ExitCodeHex ?? "—");
}

public sealed class SdkUnavailableException(Requirement check) : IOException(check.Detail)
{
    public ProcessDiagnostic? Diagnostic { get; } = check.Diagnostic;
}

public static class SdkValidation
{
    public static readonly IReadOnlyList<string> RequiredDlls = Array.AsReadOnly(new[] { "sapnwrfc.dll", "libsapucum.dll", "icuuc57.dll", "icudt57.dll", "icuin57.dll" });

    // The probe has no destination, credentials or mapping. Its only SDK open is rejected
    // locally for missing host parameters. Progress survives a native process termination.
    internal const string ProbeScript = """
import ctypes, json, os, sys
stage = 'start'
def emit(**values):
    print(json.dumps(dict(schemaVersion=1, stage=stage, **values)), flush=True)
def step(value):
    global stage
    stage = value
    emit()
try:
    ctypes.windll.kernel32.SetErrorMode(0x0001 | 0x0002 | 0x8000)
    step('icu.load')
    libraries = {}
    get_name = ctypes.windll.kernel32.GetModuleFileNameW
    get_name.argtypes = [ctypes.c_void_p, ctypes.c_wchar_p, ctypes.c_uint]
    get_name.restype = ctypes.c_uint
    for name in ('icudt57.dll', 'icuuc57.dll', 'icuin57.dll'):
        handle = ctypes.WinDLL(name, winmode=0)
        resolved = ctypes.create_unicode_buffer(32768)
        if not get_name(handle._handle, resolved, len(resolved)):
            raise OSError()
        expected = os.path.join(os.environ['SAPNWRFC_HOME'], 'lib', name)
        if os.path.normcase(os.path.realpath(resolved.value)) != os.path.normcase(os.path.realpath(expected)):
            raise OSError()
        libraries[name] = handle
    step('icu.init')
    init = libraries['icuuc57.dll'].u_init_57
    init.argtypes = [ctypes.POINTER(ctypes.c_int32)]
    init.restype = None
    status = ctypes.c_int32(0)
    init(ctypes.byref(status))
    if status.value > 0:
        raise RuntimeError()
    step('pyrfc.import')
    from pyrfc import Connection, get_nwrfclib_version
    step('sdk.version')
    version = get_nwrfclib_version()
    if isinstance(version, dict):
        version = '.'.join(str(version[k]) for k in ('major', 'minor', 'patchLevel'))
    else:
        version = '.'.join(str(v) for v in version)
    step('sdk.init')
    try:
        connection = Connection(ashost='', sysnr='00')
    except Exception as error:
        if getattr(error, 'code', None) != 20 or getattr(error, 'key', None) != 'RFC_INVALID_PARAMETER':
            raise RuntimeError() from None
    else:
        connection.close()
        raise RuntimeError()
    stage = 'complete'
    emit(ok=True, version=version)
except Exception as error:
    kind = 'ImportError' if isinstance(error, ImportError) else 'OSError' if isinstance(error, OSError) else 'RuntimeError' if isinstance(error, RuntimeError) else 'Exception'
    emit(ok=False, errorClass=kind)
    sys.exit(2)
""";

    public static Task<Requirement> CheckAsync(AppSettings settings, CancellationToken ct = default)
        => CheckAsync(settings, (executable, environment, directory, token) => ProcessRunner.RunAsync(executable, ["-c", ProbeScript],
            environment, TimeSpan.FromSeconds(20), stdin: "", ct: token, workingDirectory: directory), ct);

    internal static async Task<Requirement> CheckAsync(AppSettings settings,
        Func<string, Dictionary<string, string>, string, CancellationToken, Task<ProcessResult>> runner, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(settings.SdkDirectory)) return new("sdk", "SAP NetWeaver RFC SDK", "x64", CheckState.Waiting, UiText.Get("Text051"));
        foreach (var name in RequiredDlls)
        {
            var file = Path.Combine(settings.SdkDirectory, "lib", name);
            if (!File.Exists(file)) return Blocked("SDK_DLL_MISSING", "files", "MissingDll", detail: UiText.Get("Text052", name));
            try
            {
                using var stream = File.OpenRead(file); using var pe = new PEReader(stream);
                if (pe.PEHeaders.CoffHeader.Machine != Machine.Amd64) return Blocked("SDK_ARCH_MISMATCH", "files", "Architecture", detail: UiText.Get("Text053"));
                if (pe.PEHeaders.PEHeader == null || !pe.PEHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.Dll))
                    return Blocked("SDK_DLL_INVALID", "files", "BadImageFormat", detail: UiText.Get("Text054"));
            }
            catch (BadImageFormatException) { return Blocked("SDK_DLL_INVALID", "files", "BadImageFormat", detail: UiText.Get("Text054")); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return Blocked("SDK_FILE_UNREADABLE", "files", "IOError", detail: UiText.Get("Text055")); }
        }
        if (!File.Exists(settings.PythonExecutable)) return new("sdk", "SAP NetWeaver RFC SDK", "x64", CheckState.Waiting, UiText.Get("Text056"));
        string? scratch = null;
        try
        {
            var parent = Path.GetFullPath(Path.Combine(settings.DataDirectory, "cache", "sdk-probes"));
            Directory.CreateDirectory(parent);
            scratch = Path.Combine(parent, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(scratch);
            var result = await runner(settings.PythonExecutable,
                SdkRuntimeEnvironment.Create(settings.SdkDirectory, Environment.GetEnvironmentVariable("PATH")), scratch, ct);
            return Evaluate(result);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Blocked("SDK_PROBE_TIMEOUT", "start", "Timeout"); }
        catch (InvalidDataException) { return Blocked("SDK_PROBE_INVALID_OUTPUT", "start", "InvalidOutput"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        { return Blocked("SDK_PROBE_START_FAILED", "start", "StartFailure"); }
        finally
        {
            // This directory is created above, under our fixed probe root, never under the SDK.
            if (scratch != null)
            {
                var parent = Path.GetFullPath(Path.Combine(settings.DataDirectory, "cache", "sdk-probes")) + Path.DirectorySeparatorChar;
                if (Path.GetFullPath(scratch).StartsWith(parent, StringComparison.OrdinalIgnoreCase))
                    try { Directory.Delete(scratch, true); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    internal static Requirement Evaluate(ProcessResult result)
    {
        var stage = "start"; var errorClass = "ProcessExit"; var complete = false; var failed = false; string? version = null;
        string[] order = ["icu.load", "icu.init", "pyrfc.import", "sdk.version", "sdk.init", "complete"];
        var next = 0;
        try
        {
            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                using var doc = JsonDocument.Parse(line); var value = doc.RootElement;
                if (complete || failed || value.GetProperty("schemaVersion").GetInt32() != 1) throw new JsonException();
                var reportedStage = value.GetProperty("stage").GetString() ?? "";
                if (next < order.Length && reportedStage == order[next]) { stage = reportedStage; next++; }
                else if (reportedStage != stage || !value.TryGetProperty("ok", out _)) throw new JsonException();
                if (value.TryGetProperty("errorClass", out var kind))
                {
                    errorClass = kind.GetString() ?? "";
                    if (!ProcessDiagnostic.Classes.Contains(errorClass)) throw new JsonException();
                }
                if (value.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
                {
                    version = value.GetProperty("version").GetString();
                    if (stage != "complete" || version == null || version.Length is 0 or > 60 || !version.Any(char.IsAsciiDigit) || !version.All(c => char.IsAsciiDigit(c) || c == '.')) throw new JsonException();
                    complete = true;
                }
                else if (value.TryGetProperty("ok", out var failure) && failure.ValueKind == JsonValueKind.False) failed = true;
                if (stage == "complete" && !complete) throw new JsonException();
            }
            if (result.ExitCode != 0) return Blocked("SDK_PROBE_FAILED", stage, errorClass, result.ExitCode);
            if (complete) return new("sdk", "SAP NetWeaver RFC SDK", version!, CheckState.Ready, UiText.Get("Sdk.Ready"));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException) { }
        return Blocked("SDK_PROBE_INVALID_OUTPUT", "start", "InvalidOutput", result.ExitCode);
    }

    private static Requirement Blocked(string code, string stage, string kind, int? exit = null, string? detail = null)
    {
        var diagnostic = new ProcessDiagnostic(code, stage, kind, exit);
        return new("sdk", "SAP NetWeaver RFC SDK", "x64", CheckState.Blocked,
            (detail ?? UiText.Get("Sdk.Failed")) + " " + diagnostic.Summary, Diagnostic: diagnostic);
    }
}
