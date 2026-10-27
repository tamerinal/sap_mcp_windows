using System.Text.Json;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class ConnectionTests
{
    private static SettingsStore Store(TestDirectory temp) => new(Path.Combine(temp.Root, "config", "settings.json"));
    private static ManagedConnection System(string host = "sap.example", string number = "00") => new() { Name = "Geliştirme & Test", Host = host, Sysnr = number, Sid = "DEV", Router = "/H/router.example/S/3299" };

    [Fact] public async Task EmptyManagedCatalogAndCrudRoundtripWithoutGui()
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        var settings = await manager.SelectSourceAsync(AppSettings.CreateDefault(temp.Root), "managed");
        Assert.Empty(await ConnectionManager.Source(settings).LoadAsync(settings));
        var entry = System("SAP.EXAMPLE", "04"); settings = await manager.SaveSystemAsync(settings, entry);
        var loaded = (await ConnectionManager.Source(await store.LoadAsync()).LoadAsync(settings)).Single();
        Assert.Equal(entry.Id, loaded.Id); Assert.Equal("sap.example:04", loaded.TargetKey); Assert.Equal(3204, loaded.Port);
        var xml = LandscapeService.Load(settings.LandscapeFile).Single(); Assert.Equal(loaded, xml);
        var json = await File.ReadAllTextAsync(ConnectionManager.CatalogPath(settings)); Assert.DoesNotContain("password", json); Assert.DoesNotContain("secretRef", json);
        entry.Name = "Renamed"; settings = await manager.SaveSystemAsync(settings, entry);
        Assert.Equal("Renamed", LandscapeService.Load(settings.LandscapeFile).Single().Name);
        settings = await manager.DeleteSystemAsync(settings, entry.Id);
        Assert.Empty(LandscapeService.Load(settings.LandscapeFile));
        Assert.Empty((await new ManagedJsonConnectionSource().ReadCatalogAsync(settings)).Systems);
    }

    [Fact] public async Task RenameTargetMovesProfilesAndDeleteRetainsDpapiFiles()
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        var settings = await manager.SelectSourceAsync(AppSettings.CreateDefault(temp.Root), "managed"); var entry = System();
        settings = await manager.SaveSystemAsync(settings, entry);
        var secret = await new DpapiSecretStore().ProtectAsync("  Örnek şifre  ", settings.SecretsDirectory);
        settings.Profiles.Add(new() { Id = "p1", Name = "First", User = "DEMO", TargetKey = entry.TargetKey, SecretRef = secret });
        settings.Profiles.Add(new() { Id = "p2", Name = "Second", User = "OTHER", TargetKey = entry.TargetKey, SecretRef = secret });
        settings.ActiveProfiles[entry.TargetKey] = "p2"; await store.SaveAsync(settings);
        entry.Host = "changed.example"; entry.Sysnr = "05"; settings = await manager.SaveSystemAsync(settings, entry);
        Assert.All(settings.Profiles, p => Assert.Equal("changed.example:05", p.TargetKey));
        Assert.Equal("p2", settings.ActiveProfiles[entry.TargetKey]); Assert.False(settings.ActiveProfiles.ContainsKey("sap.example:00"));
        Assert.Contains("changed.example:05", await File.ReadAllTextAsync(settings.CredentialsFile));
        settings = await manager.DeleteSystemAsync(settings, entry.Id);
        Assert.Empty(settings.Profiles); Assert.Empty(settings.ActiveProfiles); Assert.True(File.Exists(secret));
        Assert.Equal("  Örnek şifre  ", await new DpapiSecretStore().ReadAsync(secret));
        Assert.DoesNotContain("changed.example", await File.ReadAllTextAsync(settings.CredentialsFile));
    }

    [Fact] public async Task ExternalSourceIsNeverWrittenAndSwitchRetainsAccounts()
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        var external = Path.Combine(temp.Root, "external.xml");
        await File.WriteAllTextAsync(external, "<Landscape><Services><Service type='SAPGUI' name='GUI' server='sap.example:3200'/></Services></Landscape>");
        var before = await File.ReadAllBytesAsync(external); var settings = AppSettings.CreateDefault(temp.Root); settings.LandscapeFile = external;
        Assert.Equal("sapGui", ConnectionManager.Mode(settings));
        settings.Profiles.Add(new() { Name = "Existing", User = "DEMO", TargetKey = "sap.example:00", SecretRef = "unchanged.dpapi" });
        settings.ActiveProfiles["sap.example:00"] = settings.Profiles[0].Id;
        settings = await manager.SelectSourceAsync(settings, "sapGui");
        Assert.False(ConnectionManager.Source(settings).CanEdit);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveSystemAsync(settings, System()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.DeleteSystemAsync(settings, "none"));
        settings = await manager.SelectSourceAsync(settings, "managed");
        settings = await manager.SaveSystemAsync(settings, System());
        settings = await manager.SelectSourceAsync(settings, "sapGui");
        Assert.Equal(external, settings.LandscapeFile); Assert.Single(settings.Profiles); Assert.Single(settings.ActiveProfiles);
        Assert.Equal(before, await File.ReadAllBytesAsync(external));
    }

    [Theory]
    [InlineData("sap.example", "0")]
    [InlineData("https://sap.example", "00")]
    [InlineData("sap.example:3300", "00")]
    [InlineData("bad host", "00")]
    [InlineData("sap.example", "100")]
    [InlineData("sap.example", "A0")]
    public void InvalidConnectionRejected(string host, string number) => Assert.Throws<InvalidDataException>(() => System(host, number).NormalizeAndValidate());

    [Theory]
    [InlineData("/H/router/W/example")]
    [InlineData("/H/router/P/example")]
    public async Task RouterSecretsNeverEnterManagedFiles(string router)
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        var settings = await manager.SelectSourceAsync(AppSettings.CreateDefault(temp.Root), "managed");
        var entry = System(); entry.Router = router;
        await Assert.ThrowsAsync<InvalidDataException>(() => manager.SaveSystemAsync(settings, entry));
        Assert.DoesNotContain(router, await File.ReadAllTextAsync(ConnectionManager.CatalogPath(settings)));
    }

    [Fact] public async Task DuplicateAndExistingProfileTargetCannotBeOverwritten()
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        var settings = await manager.SelectSourceAsync(AppSettings.CreateDefault(temp.Root), "managed"); var entry = System();
        settings = await manager.SaveSystemAsync(settings, entry);
        await Assert.ThrowsAsync<InvalidDataException>(() => manager.SaveSystemAsync(settings, System("SAP.EXAMPLE")));
        settings.Profiles.Add(new() { Name = "Existing", User = "DEMO", TargetKey = "other.example:00", SecretRef = "unchanged.dpapi" });
        entry.Host = "other.example";
        await Assert.ThrowsAsync<InvalidDataException>(() => manager.SaveSystemAsync(settings, entry));
        Assert.Equal("sap.example", LandscapeService.Load(settings.LandscapeFile).Single().Host);
    }

    [Fact] public async Task FailedBundleWriteRestoresEveryFileAndLeavesCallerUntouched()
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        var settings = await manager.SelectSourceAsync(AppSettings.CreateDefault(temp.Root), "managed");
        var paths = new[] { store.FilePath, settings.CredentialsFile, settings.LandscapeFile, ConnectionManager.CatalogPath(settings) };
        var snapshots = paths.ToDictionary(p => p, File.ReadAllText);
        await Assert.ThrowsAsync<IOException>(() => new FailingManager(store).SaveSystemAsync(settings, System()));
        foreach (var path in paths) Assert.Equal(snapshots[path], await File.ReadAllTextAsync(path));
        Assert.Empty(settings.Profiles); Assert.Empty(LandscapeService.Load(settings.LandscapeFile));
    }
    private sealed class FailingManager(SettingsStore store) : ConnectionManager(store)
    {
        private int _count;
        protected override Task WriteAsync(string path, string content, CancellationToken ct) => ++_count == 3 ? throw new IOException("Injected write failure") : base.WriteAsync(path, content, ct);
    }

    [Fact] public async Task MissingCatalogDoesNotOverwriteLastProjection()
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        var settings = await manager.SelectSourceAsync(AppSettings.CreateDefault(temp.Root), "managed");
        settings = await manager.SaveSystemAsync(settings, System()); var xml = await File.ReadAllTextAsync(settings.LandscapeFile);
        File.Delete(ConnectionManager.CatalogPath(settings));
        await Assert.ThrowsAsync<FileNotFoundException>(() => manager.SelectSourceAsync(settings, "managed"));
        Assert.Equal(xml, await File.ReadAllTextAsync(settings.LandscapeFile));
    }

    [Fact] public async Task SourceHostCasingKeepsTheSameActiveAccount()
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        var settings = AppSettings.CreateDefault(temp.Root); settings.LandscapeFile = Path.Combine(temp.Root, "gui.xml");
        await File.WriteAllTextAsync(settings.LandscapeFile, "<Landscape><Services><Service type='SAPGUI' server='SAP.EXAMPLE:3200'/></Services></Landscape>");
        var profile = new ConnectionProfile { Name = "Existing", User = "DEMO", TargetKey = "SAP.EXAMPLE:00", SecretRef = "kept.dpapi" };
        settings.Profiles.Add(profile); settings.ActiveProfiles[profile.TargetKey] = profile.Id;
        settings = await manager.SelectSourceAsync(settings, "managed"); settings = await manager.SaveSystemAsync(settings, System());
        using (var mapping = JsonDocument.Parse(await File.ReadAllTextAsync(settings.CredentialsFile)))
            Assert.Equal("DEMO", mapping.RootElement.GetProperty("overridesByTarget").GetProperty("sap.example:00").GetProperty("user").GetString());
        settings = await manager.SelectSourceAsync(settings, "sapGui");
        using (var mapping = JsonDocument.Parse(await File.ReadAllTextAsync(settings.CredentialsFile)))
            Assert.Equal("DEMO", mapping.RootElement.GetProperty("overridesByTarget").GetProperty("SAP.EXAMPLE:00").GetProperty("user").GetString());
        Assert.Single(settings.Profiles); Assert.Single(settings.ActiveProfiles);
    }

    [Fact] public async Task FolderSelectionBrokenSourcesAndPathCollisionAreHandled()
    {
        using var temp = new TestDirectory(); var store = Store(temp); var manager = new ConnectionManager(store);
        Assert.Empty(SapGuiLandscapeSource.FindInFolder(temp.Root));
        var xml = "<Landscape><Services/></Landscape>";
        foreach (var name in new[] { "SAPUILandscape.xml", "SAPUILandscapeGlobal.xml" }) await File.WriteAllTextAsync(Path.Combine(temp.Root, name), xml);
        Assert.Equal(2, SapGuiLandscapeSource.FindInFolder(temp.Root).Count);
        var settings = AppSettings.CreateDefault(temp.Root);
        await Assert.ThrowsAsync<FileNotFoundException>(() => manager.SelectSourceAsync(settings, "sapGui", Path.Combine(temp.Root, "missing.xml")));
        Assert.False(File.Exists(store.FilePath));
        settings.LandscapeFile = Path.Combine(temp.Root, "SAPUILandscape.xml"); settings.ManagedConnectionsFile = settings.LandscapeFile;
        await Assert.ThrowsAsync<JsonException>(() => manager.SelectSourceAsync(settings, "managed"));
        Assert.Equal(xml, await File.ReadAllTextAsync(settings.LandscapeFile));
        settings.ManagedConnectionsFile = store.FilePath;
        await Assert.ThrowsAsync<InvalidDataException>(() => manager.SelectSourceAsync(settings, "managed"));
    }
}
