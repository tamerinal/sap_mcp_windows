using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.Win32;

namespace SapMcp.Core;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ManagedConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public string Sysnr { get; set; } = "00";
    public string Sid { get; set; } = "";
    public string Router { get; set; } = "";
    [JsonIgnore] public string TargetKey => $"{Host}:{Sysnr}";
    public SapSystem ToSystem() => new(Id, Name, Sid, Host, 3200 + int.Parse(Sysnr, CultureInfo.InvariantCulture), Sysnr, Router);
    public void NormalizeAndValidate()
    {
        if (Name == null || Host == null || Sysnr == null || Sid == null || Router == null) throw new InvalidDataException(UiText.Get("Connections.InvalidSystem"));
        Name = Name.Trim(); Host = Host.Trim().ToLowerInvariant(); Sysnr = Sysnr.Trim(); Sid = Sid.Trim().ToUpperInvariant(); Router = Router.Trim();
        if (Router.Contains("/W/", StringComparison.OrdinalIgnoreCase) || Router.Contains("/P/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(UiText.Get("Connections.RouterPassword"));
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Name) || Uri.CheckHostName(Host.Trim('[', ']')) == UriHostNameType.Unknown || Host.Any(char.IsWhiteSpace) || Host.Contains('/') || Host.Contains('\\'))
            throw new InvalidDataException(UiText.Get("Connections.InvalidSystem"));
        if (Sysnr.Length != 2 || !Sysnr.All(char.IsAsciiDigit)) throw new InvalidDataException(UiText.Get("Connections.InvalidNumber"));
        if (Name.Length > 200 || Host.Length > 253 || Sid.Length > 3 || Router.Length > 2048 || Sid.Any(c => !char.IsAsciiLetterOrDigit(c)) || Router.Any(char.IsControl))
            throw new InvalidDataException(UiText.Get("Connections.InvalidSystem"));
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ConnectionCatalog
{
    public int SchemaVersion { get; set; } = 1;
    public List<ManagedConnection> Systems { get; set; } = [];
    public void Validate()
    {
        if (SchemaVersion != 1) throw new InvalidDataException(UiText.Get("Text019"));
        if (Systems == null || Systems.Any(s => s == null)) throw new InvalidDataException(UiText.Get("Connections.InvalidCatalog"));
        foreach (var system in Systems) system.NormalizeAndValidate();
        if (Systems.Select(s => s.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Systems.Count ||
            Systems.Select(s => s.TargetKey).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Systems.Count)
            throw new InvalidDataException(UiText.Get("Connections.Duplicate"));
    }
}

public interface IConnectionSource
{
    bool CanEdit { get; }
    Task<IReadOnlyList<SapSystem>> LoadAsync(AppSettings settings, CancellationToken ct = default);
}

public sealed class SapGuiLandscapeSource : IConnectionSource
{
    public bool CanEdit => false;
    public Task<IReadOnlyList<SapSystem>> LoadAsync(AppSettings settings, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(LandscapeService.Load(ConnectionManager.ExternalPath(settings)));
    }
    public static IReadOnlyList<string> FindInFolder(string folder) =>
        new[] { "SAPUILandscape.xml", "SAPUILandscapeGlobal.xml" }.Select(n => Path.Combine(folder, n)).Where(File.Exists).ToArray();
}

public sealed class ManagedJsonConnectionSource : IConnectionSource
{
    public bool CanEdit => true;
    public async Task<ConnectionCatalog> ReadCatalogAsync(AppSettings settings, CancellationToken ct = default, bool createIfMissing = false)
    {
        var path = ConnectionManager.CatalogPath(settings);
        if (!File.Exists(path))
        {
            if (createIfMissing || settings.ConnectionSourceMode == null) return new();
            throw new FileNotFoundException(UiText.Get("Connections.MissingCatalog"));
        }
        if (new FileInfo(path).Length > 20 * 1024 * 1024) throw new InvalidDataException(UiText.Get("Connections.InvalidCatalog"));
        var catalog = JsonSerializer.Deserialize<ConnectionCatalog>(await File.ReadAllTextAsync(path, ct), JsonDefaults.Options)
            ?? throw new InvalidDataException(UiText.Get("Connections.InvalidCatalog"));
        catalog.Validate(); return catalog;
    }
    public async Task<IReadOnlyList<SapSystem>> LoadAsync(AppSettings settings, CancellationToken ct = default) =>
        (await ReadCatalogAsync(settings, ct)).Systems.Select(s => s.ToSystem()).ToArray();

    public static string ExportXml(ConnectionCatalog catalog)
    {
        catalog.Validate();
        var routers = new XElement("Routers"); var services = new XElement("Services");
        foreach (var system in catalog.Systems)
        {
            var service = new XElement("Service", new XAttribute("uuid", system.Id), new XAttribute("type", "SAPGUI"),
                new XAttribute("name", system.Name), new XAttribute("systemid", system.Sid), new XAttribute("server", $"{system.Host}:{3200 + int.Parse(system.Sysnr, CultureInfo.InvariantCulture)}"));
            if (system.Router.Length > 0)
            {
                var id = "router-" + system.Id;
                routers.Add(new XElement("Router", new XAttribute("uuid", id), new XAttribute("router", system.Router)));
                service.Add(new XAttribute("routerid", id));
            }
            services.Add(service);
        }
        return new XDocument(new XElement("Landscape", routers, services)).ToString();
    }
}

public static class SapGuiInstallation
{
    public static bool IsDetected()
    {
        if (!OperatingSystem.IsWindows()) return false;
        foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            if (root.Length > 0 && File.Exists(Path.Combine(root, "SAP", "FrontEnd", "SAPgui", "saplogon.exe"))) return true;
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
                try
                {
                    using var key = RegistryKey.OpenBaseKey(hive, view);
                    using var installed = key.OpenSubKey(@"SOFTWARE\SAP\SAPGUI Front\SAP Frontend Server");
                    if (installed != null) return true;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
        return false;
    }
}

/// <summary>Owns catalog/projection changes, including profile migration and rollback. External XML is never a write target.</summary>
public class ConnectionManager(SettingsStore store)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public static string Mode(AppSettings settings) => settings.ConnectionSourceMode ?? (settings.LandscapeFile.Length > 0 ? "sapGui" : "managed");
    public static string ExternalPath(AppSettings settings) => settings.ExternalLandscapeFile ?? (settings.ConnectionSourceMode != "managed" ? settings.LandscapeFile : "");
    public static string CatalogPath(AppSettings settings) => settings.ManagedConnectionsFile ?? Path.Combine(settings.DataDirectory, "config", "connections.json");
    public static string GeneratedPath(AppSettings settings) => Path.Combine(settings.DataDirectory, "config", "SAPMcpLandscape.xml");
    public static IConnectionSource Source(AppSettings settings) => Mode(settings) == "managed" ? new ManagedJsonConnectionSource() : new SapGuiLandscapeSource();
    private static AppSettings Copy(AppSettings settings) => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings, JsonDefaults.Options), JsonDefaults.Options)!;

    public async Task<AppSettings> SelectSourceAsync(AppSettings current, string mode, string? externalFile = null, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var next = Copy(current);
            next.ExternalLandscapeFile = externalFile ?? ExternalPath(current);
            next.ConnectionSourceMode = mode; next.ManagedConnectionsFile = CatalogPath(current);
            if (mode == "sapGui")
            {
                var systems = LandscapeService.Load(next.ExternalLandscapeFile);
                next.LandscapeFile = next.ExternalLandscapeFile;
                next.Validate();
                EnsureDistinct(next, [store.FilePath, next.CredentialsFile, CatalogPath(next), GeneratedPath(next)]);
                var changes = new List<(string, string)>();
                if (next.Profiles.Count > 0) changes.Add((next.CredentialsFile, ProfileService.SerializeMapping(next, systems)));
                changes.Add((store.FilePath, JsonSerializer.Serialize(next, JsonDefaults.Options)));
                await CommitAsync(changes.ToArray(), ct);
            }
            else if (mode == "managed")
            {
                var catalog = await new ManagedJsonConnectionSource().ReadCatalogAsync(next, ct, createIfMissing: !File.Exists(GeneratedPath(current)));
                await CommitManagedAsync(next, catalog, ct);
            }
            else throw new InvalidDataException(UiText.Get("Connections.InvalidMode"));
            return next;
        }
        finally { Gate.Release(); }
    }

    public async Task<AppSettings> SaveSystemAsync(AppSettings current, ManagedConnection entry, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            RequireManaged(current); var next = Copy(current);
            var catalog = await new ManagedJsonConnectionSource().ReadCatalogAsync(current, ct);
            entry = JsonSerializer.Deserialize<ManagedConnection>(JsonSerializer.Serialize(entry, JsonDefaults.Options), JsonDefaults.Options)!;
            entry.NormalizeAndValidate();
            var old = catalog.Systems.SingleOrDefault(s => s.Id == entry.Id);
            if (old != null && !old.TargetKey.Equals(entry.TargetKey, StringComparison.Ordinal))
            {
                if (!old.TargetKey.Equals(entry.TargetKey, StringComparison.OrdinalIgnoreCase) &&
                    (next.Profiles.Any(p => p.TargetKey.Equals(entry.TargetKey, StringComparison.OrdinalIgnoreCase)) || next.ActiveProfiles.ContainsKey(entry.TargetKey)))
                    throw new InvalidDataException(UiText.Get("Connections.TargetConflict"));
                foreach (var profile in next.Profiles.Where(p => p.TargetKey.Equals(old.TargetKey, StringComparison.OrdinalIgnoreCase))) profile.TargetKey = entry.TargetKey;
                if (next.ActiveProfiles.Remove(old.TargetKey, out var active)) next.ActiveProfiles[entry.TargetKey] = active;
            }
            catalog.Systems.RemoveAll(s => s.Id == entry.Id); catalog.Systems.Add(entry);
            await CommitManagedAsync(next, catalog, ct); return next;
        }
        finally { Gate.Release(); }
    }

    public async Task<AppSettings> DeleteSystemAsync(AppSettings current, string id, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            RequireManaged(current); var next = Copy(current);
            var catalog = await new ManagedJsonConnectionSource().ReadCatalogAsync(current, ct);
            var old = catalog.Systems.Single(s => s.Id == id);
            catalog.Systems.Remove(old);
            next.Profiles.RemoveAll(p => p.TargetKey.Equals(old.TargetKey, StringComparison.OrdinalIgnoreCase));
            next.ActiveProfiles.Remove(old.TargetKey);
            await CommitManagedAsync(next, catalog, ct); return next;
        }
        finally { Gate.Release(); }
    }

    private static void RequireManaged(AppSettings settings)
    {
        if (Mode(settings) != "managed") throw new InvalidOperationException(UiText.Get("Connections.ReadOnly"));
    }
    private static void EnsureDistinct(AppSettings settings, IEnumerable<string> paths)
    {
        var full = paths.Select(Path.GetFullPath).ToArray();
        var external = ExternalPath(settings);
        if (full.Distinct(StringComparer.OrdinalIgnoreCase).Count() != full.Length ||
            (external.Length > 0 && full.Any(p => p.Equals(Path.GetFullPath(external), StringComparison.OrdinalIgnoreCase))))
            throw new InvalidDataException(UiText.Get("Connections.PathConflict"));
    }
    private async Task CommitManagedAsync(AppSettings settings, ConnectionCatalog catalog, CancellationToken ct)
    {
        catalog.Validate(); settings.ManagedConnectionsFile = CatalogPath(settings);
        settings.ConnectionSourceMode = "managed"; settings.LandscapeFile = GeneratedPath(settings); settings.Validate();
        var paths = new[] { settings.ManagedConnectionsFile, settings.LandscapeFile, settings.CredentialsFile, store.FilePath };
        EnsureDistinct(settings, paths);
        var xml = ManagedJsonConnectionSource.ExportXml(catalog);
        // Read our own projection with the same parser used by the desktop.
        if (LandscapeService.Parse(XDocument.Parse(xml)).Count != catalog.Systems.Count) throw new InvalidDataException(UiText.Get("Connections.InvalidCatalog"));
        var mapping = ProfileService.SerializeMapping(settings, catalog.Systems.Select(s => s.ToSystem()).ToArray());
        await CommitAsync([(settings.ManagedConnectionsFile, JsonSerializer.Serialize(catalog, JsonDefaults.Options)),
            (settings.CredentialsFile, mapping), (settings.LandscapeFile, xml), (store.FilePath, JsonSerializer.Serialize(settings, JsonDefaults.Options))], ct);
    }
    protected virtual Task WriteAsync(string path, string content, CancellationToken ct) => AtomicFile.WriteAsync(path, content, ct, privateData: true);
    private async Task CommitAsync((string Path, string Content)[] changes, CancellationToken ct)
    {
        var previous = changes.Select(c => (c.Path, Data: File.Exists(c.Path) ? File.ReadAllText(c.Path) : null)).ToArray();
        var written = 0;
        try { foreach (var change in changes) { written++; await WriteAsync(change.Path, change.Content, ct); } }
        catch (Exception original)
        {
            var failures = new List<Exception>();
            foreach (var old in previous.Take(written).Reverse())
                try { if (old.Data == null) File.Delete(old.Path); else await WriteAsync(old.Path, old.Data, CancellationToken.None); }
                catch (Exception rollback) { failures.Add(rollback); }
            if (failures.Count > 0) throw new AggregateException(UiText.Get("Connections.RollbackFailed"), new[] { original }.Concat(failures));
            throw;
        }
    }
}
