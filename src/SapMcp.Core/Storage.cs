using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace SapMcp.Core;

public static class AtomicFile
{
    public static async Task WriteAsync(string path, string content, CancellationToken ct = default, bool privateData = false)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(parent);
        var temp = Path.Combine(parent, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
            if (OperatingSystem.IsWindows())
            {
                if (privateData) RestrictFileToCurrentUser(temp);
                else if (File.Exists(path)) new FileInfo(temp).SetAccessControl(new FileInfo(path).GetAccessControl(AccessControlSections.Access));
            }
            await File.WriteAllTextAsync(temp, content, new UTF8Encoding(false), ct);
            using (var stream = new FileStream(temp, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) stream.Flush(true);
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void RestrictFileToCurrentUser(string file)
    {
        if (!OperatingSystem.IsWindows()) return;
        var sid = WindowsIdentity.GetCurrent().User!;
        var security = new FileSecurity();
        security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
        new FileInfo(file).SetAccessControl(security);
    }
}

public sealed class SettingsStore(string path)
{
    public string FilePath { get; } = Path.GetFullPath(path);
    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(FilePath)) return AppSettings.CreateDefault(Path.GetDirectoryName(Path.GetDirectoryName(FilePath)));
        var settings = JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(FilePath, ct), JsonDefaults.Options)
            ?? throw new InvalidDataException(UiText.Get("Text078"));
        settings.Validate();
        return settings;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        settings.Validate();
        await AtomicFile.WriteAsync(FilePath, JsonSerializer.Serialize(settings, JsonDefaults.Options), ct, privateData: true);
    }

    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SapMcpManager", "config", "settings.json");
}

public interface ISecretStore
{
    Task<string> ProtectAsync(string password, string directory, CancellationToken ct = default);
    Task<string> ReadAsync(string file, CancellationToken ct = default);
}

public sealed class DpapiSecretStore : ISecretStore
{
    public async Task<string> ProtectAsync(string password, string directory, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException(UiText.Get("Text079"));
        if (password.Length == 0) throw new InvalidDataException(UiText.Get("Text080"));
        Directory.CreateDirectory(directory);
        // ConvertFrom-SecureString (without -Key) stores hex-encoded DPAPI over UTF-16LE.
        var bytes = Encoding.Unicode.GetBytes(password);
        try
        {
            var cipher = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            var file = Path.Combine(directory, $"{Guid.NewGuid():N}.password.dpapi");
            await AtomicFile.WriteAsync(file, Convert.ToHexString(cipher).ToLowerInvariant(), ct, privateData: true);
            return file;
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public async Task<string> ReadAsync(string file, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (new FileInfo(file).Length > 1024 * 1024) throw new InvalidDataException(UiText.Get("Text081"));
        try
        {
            var cipher = Convert.FromHexString((await File.ReadAllTextAsync(file, ct)).Trim());
            var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            try { return Encoding.Unicode.GetString(plain); }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        { throw new InvalidDataException(UiText.Get("Text082")); }
    }
}

public static class LandscapeService
{
    public static IReadOnlyList<string> Discover()
    {
        var candidates = new List<string>();
        var dirs = new[] {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP", "Common"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SAP", "SAP GUI"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Documents", "SAP", "SAP GUI")
        };
        foreach (var dir in dirs) foreach (var name in new[] { "SAPUILandscape.xml", "SAPUILandscapeGlobal.xml" })
            if (File.Exists(Path.Combine(dir, name))) candidates.Add(Path.Combine(dir, name));
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static IReadOnlyList<SapSystem> Load(string path)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20 * 1024 * 1024 });
        return Parse(XDocument.Load(reader));
    }

    public static IReadOnlyList<SapSystem> Parse(XDocument xml)
    {
        var routers = xml.Descendants().Where(e => e.Name.LocalName == "Router" && e.Attribute("uuid") != null)
            .GroupBy(e => (string)e.Attribute("uuid")!).ToDictionary(g => g.Key, g => (string?)g.First().Attribute("router") ?? "");
        var result = new List<SapSystem>();
        foreach (var s in xml.Descendants().Where(e => e.Name.LocalName == "Service" && (string?)e.Attribute("type") == "SAPGUI"))
        {
            var address = (string?)s.Attribute("server") ?? "";
            var colon = address.LastIndexOf(':');
            if (colon <= 0 || !int.TryParse(address[(colon + 1)..], out var port) || port is < 1 or > 65535) continue;
            var host = address[..colon];
            var router = routers.GetValueOrDefault((string?)s.Attribute("routerid") ?? "", "");
            result.Add(new((string?)s.Attribute("uuid") ?? address, (string?)s.Attribute("name") ?? address,
                (string?)s.Attribute("systemid") ?? "", host, port, port is >= 3200 and <= 3299 ? (port % 100).ToString("00") : null, router));
        }
        return result;
    }
}

public sealed class ProfileService(ISecretStore secrets)
{
    public async Task SaveAsync(AppSettings settings, ConnectionProfile profile, string? password, bool activate, CancellationToken ct = default)
    {
        profile.Validate();
        if (!string.IsNullOrEmpty(password))
        {
            var file = await secrets.ProtectAsync(password, settings.SecretsDirectory, ct);
            profile.SecretRef = Path.GetRelativePath(Path.GetDirectoryName(settings.CredentialsFile)!, file).Replace('\\', '/');
        }
        if (string.IsNullOrWhiteSpace(profile.SecretRef)) throw new InvalidDataException(UiText.Get("Text083"));
        await secrets.ReadAsync(ResolveSecret(settings.CredentialsFile, profile.SecretRef), ct);
        var existing = settings.Profiles.FindIndex(p => p.Id == profile.Id);
        if (existing >= 0) settings.Profiles[existing] = profile; else settings.Profiles.Add(profile);
        if (activate || !settings.ActiveProfiles.ContainsKey(profile.TargetKey)) settings.ActiveProfiles[profile.TargetKey] = profile.Id;
        await MaterializeAsync(settings, ct);
    }

    public static string ResolveSecret(string credentialsFile, string secretRef)
    {
        var file = Path.GetFullPath(secretRef, Path.GetDirectoryName(credentialsFile)!);
        if (new Uri(file).IsUnc) throw new InvalidDataException(UiText.Get("Text084"));
        return file;
    }

    public static async Task MaterializeAsync(AppSettings settings, CancellationToken ct = default)
    {
        var entries = new JsonObject();
        foreach (var (target, id) in settings.ActiveProfiles)
        {
            var p = settings.Profiles.Single(x => x.Id == id);
            if (p.SecretRef.Length == 0) throw new InvalidDataException(UiText.Get("Text085"));
            var node = new JsonObject();
            foreach (var (key, value) in p.Extra) node[key] = JsonNode.Parse(value.GetRawText());
            node["user"] = p.User; node["client"] = p.Client; node["lang"] = p.Language; node["secretRef"] = p.SecretRef;
            node.Remove("passwd"); node.Remove("password");
            if (p.AdtBaseUrl.Length > 0) { node["adtBaseUrl"] = p.AdtBaseUrl; node.Remove("adtBaseUrls"); }
            if (p.BwModelingBaseUrl.Length > 0) { node["bwModelingBaseUrl"] = p.BwModelingBaseUrl; node.Remove("bwModelingBaseUrls"); }
            node["allowInsecureTls"] = p.AllowInsecureTls;
            entries[target] = node;
        }
        await AtomicFile.WriteAsync(settings.CredentialsFile, new JsonObject { ["overridesByTarget"] = entries }.ToJsonString(JsonDefaults.Options), ct, privateData: true);
    }

    public async Task<int> ImportAsync(AppSettings settings, string source, IReadOnlyList<SapSystem> systems, CancellationToken ct = default)
    {
        var root = JsonNode.Parse(await File.ReadAllTextAsync(source, ct))?.AsObject() ?? throw new InvalidDataException(UiText.Get("Text086"));
        var imported = new List<ConnectionProfile>();
        foreach (var system in systems.DistinctBy(s => s.TargetKey))
        {
            var effective = new JsonObject();
            foreach (var node in new[] { root["default"], root["overrides"]?[system.Sid], root["overrides"]?[system.Name], root["overridesByTarget"]?[system.TargetKey] })
                if (node is JsonObject obj) foreach (var (key, value) in obj) effective[key] = value?.DeepClone();
            var user = (string?)effective["user"];
            if (string.IsNullOrEmpty(user)) continue;
            var password = (string?)effective["passwd"];
            if (string.IsNullOrEmpty(password) && (string?)effective["secretRef"] is { Length: > 0 } reference)
                password = await secrets.ReadAsync(ResolveSecret(source, reference), ct);
            if (string.IsNullOrEmpty(password)) continue;
            var p = new ConnectionProfile { Name = UiText.Get("Text087") + system.Name, TargetKey = system.TargetKey,
                User = user, Client = (string?)effective["client"] ?? "100", Language = (string?)effective["lang"] ?? "EN",
                AdtBaseUrl = (string?)effective["adtBaseUrl"] ?? "", BwModelingBaseUrl = (string?)effective["bwModelingBaseUrl"] ?? "",
                AllowInsecureTls = (bool?)effective["allowInsecureTls"] ?? false };
            foreach (var (k, v) in effective)
                if (!new[] { "passwd", "password", "secretRef", "user", "client", "lang", "adtBaseUrl", "bwModelingBaseUrl", "allowInsecureTls" }.Contains(k) && v != null)
                    p.Extra[k] = JsonSerializer.SerializeToElement(v);
            p.Validate();
            var secretFile = await secrets.ProtectAsync(password, settings.SecretsDirectory, ct);
            p.SecretRef = Path.GetRelativePath(Path.GetDirectoryName(settings.CredentialsFile)!, secretFile).Replace('\\', '/');
            imported.Add(p);
        }
        foreach (var p in imported) { settings.Profiles.Add(p); settings.ActiveProfiles.TryAdd(p.TargetKey, p.Id); }
        await MaterializeAsync(settings, ct);
        return imported.Count;
    }
}
