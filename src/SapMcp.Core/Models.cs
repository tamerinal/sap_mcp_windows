using System.Text.Json;
using System.Text.Json.Serialization;

namespace SapMcp.Core;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}

public enum LogLevel { Trace, Debug, Info, Warn, Error, Off }
public enum CheckState { Ready, Missing, Blocked, Waiting, Warning }

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public string? UiLanguage { get; set; }
    public string? UiMode { get; set; }
    public string? BasicProfileId { get; set; }
    public string AppliedSetupId { get; set; } = "";
    public string DataDirectory { get; set; } = "";
    public string LandscapeFile { get; set; } = "";
    public string? ConnectionSourceMode { get; set; }
    public string? ExternalLandscapeFile { get; set; }
    public string? ManagedConnectionsFile { get; set; }
    public string CredentialsFile { get; set; } = "";
    public string SecretsDirectory { get; set; } = "";
    public string SdkDirectory { get; set; } = "";
    public string NodeExecutable { get; set; } = "";
    public string PythonExecutable { get; set; } = "";
    public string PowerShellExecutable { get; set; } = "";
    public string PackageDirectory { get; set; } = "";
    public string HostExecutable { get; set; } = "";
    public ComponentSettings Rfc { get; set; } = new();
    public ComponentSettings Adt { get; set; } = new();
    public List<ConnectionProfile> Profiles { get; set; } = [];
    private Dictionary<string, string> _activeProfiles = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ActiveProfiles
    {
        get => _activeProfiles;
        set => _activeProfiles = new(value, StringComparer.OrdinalIgnoreCase);
    }
    public bool CodexEnabled { get; set; } = true;
    public bool ClaudeEnabled { get; set; } = true;
    public List<ClientRegistration> Registrations { get; set; } = [];

    public static AppSettings CreateDefault(string? root = null)
    {
        root ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SapMcpManager");
        return new()
        {
            DataDirectory = root,
            CredentialsFile = Path.Combine(root, "config", "sap_credentials.json"),
            SecretsDirectory = Path.Combine(root, "secrets"),
            NodeExecutable = Path.Combine(root, "runtimes", "node", "node.exe"),
            PythonExecutable = Path.Combine(root, "runtimes", "python", "python.exe"),
            PowerShellExecutable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
            PackageDirectory = Path.Combine(root, "packages"),
            HostExecutable = Path.Combine(AppContext.BaseDirectory, "SapMcpHost.exe"),
            Rfc = new() { LogDirectory = Path.Combine(root, "logs", "rfc") },
            Adt = new() { LogDirectory = Path.Combine(root, "logs", "adt") }
        };
    }

    public ComponentSettings Component(string name) => name switch
    {
        "rfc" => Rfc, "adt" => Adt, _ => throw new ArgumentException(UiText.Get("Text018"))
    };

    public void Validate()
    {
        if (UiMode is not (null or "basic" or "advanced")) throw new InvalidDataException("INVALID_UI_MODE");
        if (ConnectionSourceMode is not (null or "sapGui" or "managed")) throw new InvalidDataException(UiText.Get("Connections.InvalidMode"));
        foreach (var path in new[] { ExternalLandscapeFile, ManagedConnectionsFile })
            if (!string.IsNullOrEmpty(path) && !Path.IsPathFullyQualified(path)) throw new InvalidDataException(UiText.Get("Text020"));
        if (UiLanguage is not (null or "en" or "tr")) throw new InvalidDataException(UiText.Get("InvalidUiLanguage"));
        if (SchemaVersion != 1) throw new InvalidDataException(UiText.Get("Text019"));
        foreach (var p in new[] { DataDirectory, CredentialsFile, SecretsDirectory, NodeExecutable, PythonExecutable, PowerShellExecutable, PackageDirectory, HostExecutable, Rfc.LogDirectory, Adt.LogDirectory })
            if (!Path.IsPathFullyQualified(p)) throw new InvalidDataException(UiText.Get("Text020"));
        if (LandscapeFile.Length > 0 && !Path.IsPathFullyQualified(LandscapeFile)) throw new InvalidDataException(UiText.Get("Text021"));
        var external = ExternalLandscapeFile ?? (ConnectionSourceMode != "managed" ? LandscapeFile : "");
        if (external.Length > 0 && new[] { CredentialsFile, ManagedConnectionsFile }.Where(p => !string.IsNullOrEmpty(p)).Any(p => Path.GetFullPath(p!).Equals(Path.GetFullPath(external), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException(UiText.Get("Connections.PathConflict"));
        if (SdkDirectory.Length > 0 && !Path.IsPathFullyQualified(SdkDirectory)) throw new InvalidDataException(UiText.Get("Text022"));
        foreach (var c in new[] { Rfc, Adt })
            if (c.RetentionDays is < 1 or > 3650 || c.MaxFileBytes is < 1024 or > 1_073_741_824 || c.MaxFiles is < 1 or > 100 || c.TimeoutSeconds is < 1 or > 3600)
                throw new InvalidDataException(UiText.Get("Text023"));
        foreach (var folder in new[] { DataDirectory, SecretsDirectory })
            if (Path.GetFullPath(folder).TrimEnd('\\', '/') == Path.GetPathRoot(folder)?.TrimEnd('\\', '/'))
                throw new InvalidDataException(UiText.Get("Text024"));
        if (Profiles.Select(p => p.Id).Distinct().Count() != Profiles.Count) throw new InvalidDataException(UiText.Get("Text025"));
        foreach (var profile in Profiles) profile.Validate();
        foreach (var (target, id) in ActiveProfiles)
            if (!Profiles.Any(p => p.Id == id && p.TargetKey.Equals(target, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException(UiText.Get("Text026"));
    }
}

public sealed class ComponentSettings
{
    public bool Selected { get; set; } = true;
    public bool Installed { get; set; }
    public string Version { get; set; } = "";
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    public string LogDirectory { get; set; } = "";
    public int RetentionDays { get; set; } = 30;
    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxFiles { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 30;
    public string FunctionAllowlist { get; set; } = "";
}

public sealed class ConnectionProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string TargetKey { get; set; } = "";
    public string User { get; set; } = "";
    public string Client { get; set; } = "100";
    public string Language { get; set; } = "TR";
    public string SecretRef { get; set; } = "";
    public string AdtBaseUrl { get; set; } = "";
    public string BwModelingBaseUrl { get; set; } = "";
    public bool AllowInsecureTls { get; set; }
    public bool AdtAutoDiscovery { get; set; }
    public int AdtTimeoutSeconds { get; set; } = 15;
    public List<TrustedCertificate> AdtCertificates { get; set; } = [];
    public BasicValidation? BasicValidation { get; set; }
    public Dictionary<string, JsonElement> Extra { get; set; } = [];

    public void Validate()
    {
        if (AdtTimeoutSeconds is < 1 or > 120 || AdtCertificates == null || AdtCertificates.Count > 20 || AdtCertificates.Any(c => !Path.IsPathFullyQualified(c.File) || c.Sha256.Length != 64 || !c.Sha256.All(char.IsAsciiHexDigit)))
            throw new InvalidDataException(UiText.Get("Adt.InvalidCertificate"));
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(TargetKey) || string.IsNullOrWhiteSpace(User))
            throw new InvalidDataException(UiText.Get("Text027"));
        if (Client.Length != 3 || !Client.All(char.IsAsciiDigit)) throw new InvalidDataException(UiText.Get("Text028"));
        if (Language.Length is < 1 or > 2) throw new InvalidDataException(UiText.Get("Text029"));
        foreach (var url in new[] { AdtBaseUrl, BwModelingBaseUrl }.Where(u => u.Length > 0))
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http") || uri.UserInfo.Length > 0)
                throw new InvalidDataException(UiText.Get("Text030"));
        foreach (var key in Extra.Keys)
            if (key.Equals("passwd", StringComparison.OrdinalIgnoreCase) || key.Equals("password", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(UiText.Get("Text031"));
    }
    public override string ToString() => $"{Name} · {User} / {Client}";
}

public sealed record SapSystem(string Id, string Name, string Sid, string Host, int Port, string? Sysnr, string Router)
{
    public string TargetKey => $"{Host}:{Sysnr ?? "??"}";
    public override string ToString() => $"{Name}  ·  {Sid}  ·  {TargetKey}";
}

public sealed record Requirement(string Id, string Name, string Version, CheckState State, string Detail, bool Installable = false, ProcessDiagnostic? Diagnostic = null);
public sealed record ProcessResult(int ExitCode, string Output, string Error);
public sealed record ClientRegistration(string Client, string ConfigPath, List<string> ServerNames);

public sealed class PackageCatalog
{
    public int SchemaVersion { get; set; } = 1;
    public string Version { get; set; } = "0.1.0";
    public List<PackageAsset> Assets { get; set; } = [];
    public PackageAsset Find(string id) => Assets.SingleOrDefault(a => a.Id == id) ?? throw new InvalidDataException(UiText.Get("Text032" , id));
}

public sealed class PackageAsset
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string Url { get; set; } = "";
    public string Format { get; set; } = "zip";
    public string StripPrefix { get; set; } = "";
    public string Source { get; set; } = "";
    public Dictionary<string, string> Capabilities { get; set; } = [];
}
