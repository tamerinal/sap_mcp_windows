using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class TestDirectory : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "SapMcp tests boşluk " + Guid.NewGuid().ToString("N"));
    public TestDirectory() => Directory.CreateDirectory(Root);
    public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
}

public sealed class StorageTests
{
    [Fact] public async Task SettingsRoundTripAndInvalidConfigurationDoesNotOverwrite()
    {
        using var temp = new TestDirectory(); var store = new SettingsStore(Path.Combine(temp.Root, "config", "settings.json")); var s = AppSettings.CreateDefault(temp.Root);
        await store.SaveAsync(s); var before = await File.ReadAllTextAsync(store.FilePath); s.Rfc.RetentionDays = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync(s)); Assert.Equal(before, await File.ReadAllTextAsync(store.FilePath));
        Assert.Equal(30, (await store.LoadAsync()).Rfc.RetentionDays);
    }

    [Fact] public async Task UnsupportedSchemaFailsWithoutResettingSettings()
    {
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "settings.json"); await File.WriteAllTextAsync(file, "{\"schemaVersion\":9}");
        await Assert.ThrowsAsync<InvalidDataException>(() => new SettingsStore(file).LoadAsync()); Assert.Contains("9", await File.ReadAllTextAsync(file));
    }

    [Fact] public void LandscapeExtractsTargetRouterAndPreservesAlias()
    {
        var xml = XDocument.Parse("""
            <Landscape><Routers><Router uuid="r1" router="/H/router/S/3299"/></Routers><Services>
            <Service type="SAPGUI" uuid="a" name="Geliştirme &amp; Test" systemid="DEV" server="sap.local:3204" routerid="r1"/>
            <Service type="SAPGUI" uuid="b" name="Alias" systemid="DEV" server="sap.local:3204"/>
            <Service type="SAPGUI" uuid="x" server="invalid"/><Service type="OTHER" server="other:3200"/>
            </Services></Landscape>
            """);
        var systems = LandscapeService.Parse(xml); Assert.Equal(2, systems.Count); Assert.Equal("sap.local:04", systems[0].TargetKey); Assert.Equal("/H/router/S/3299", systems[0].Router); Assert.Equal("Geliştirme & Test", systems[0].Name);
    }

    [Fact] public void LandscapeRejectsExternalEntities()
    {
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "bad.xml"); File.WriteAllText(file, "<!DOCTYPE a [<!ENTITY e SYSTEM 'file:///C:/secret'>]><Landscape>&e;</Landscape>");
        Assert.Throws<System.Xml.XmlException>(() => LandscapeService.Load(file));
    }

    [Fact] public async Task DpapiIsCompatibleWithPowerShellInBothDirections()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var temp = new TestDirectory(); var secretStore = new DpapiSecretStore(); var password = "  ŞifreİıÖçğü!42  ";
        var file = await secretStore.ProtectAsync(password, temp.Root); Assert.Equal(password, await secretStore.ReadAsync(file));
        var ps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        var readScript = "[Console]::InputEncoding=[Text.Encoding]::UTF8; $secure = Get-Content -LiteralPath ([Console]::In.ReadToEnd()) | ConvertTo-SecureString; $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure); try { $s=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr); [Console]::Out.Write([Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($s))) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }";
        var run = await ProcessRunner.RunAsync(ps, ["-NoProfile", "-NonInteractive", "-Command", readScript], stdin: file);
        Assert.True(run.ExitCode == 0, run.Error); Assert.Equal(password, Encoding.Unicode.GetString(Convert.FromBase64String(run.Output.Trim())));
        var writeScript = "$s=[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String([Console]::In.ReadToEnd())); ConvertTo-SecureString -String $s -AsPlainText -Force | ConvertFrom-SecureString";
        var encrypted = await ProcessRunner.RunAsync(ps, ["-NoProfile", "-NonInteractive", "-Command", writeScript], stdin: Convert.ToBase64String(Encoding.Unicode.GetBytes(password)));
        Assert.Equal(0, encrypted.ExitCode); var other = Path.Combine(temp.Root, "from-powershell.dpapi"); await File.WriteAllTextAsync(other, encrypted.Output.Trim()); Assert.Equal(password, await secretStore.ReadAsync(other));
        Assert.DoesNotContain(password, await File.ReadAllTextAsync(file));
    }

    [Fact] public async Task CorruptDpapiGivesActionableError()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "secret.dpapi"); await File.WriteAllTextAsync(file, "xyz");
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => new DpapiSecretStore().ReadAsync(file)); Assert.Equal(UiText.Get("Text082"), error.Message);
    }

    [Fact] public async Task MultipleProfilesOnlyMaterializeActiveProfileWithoutPlaintext()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var temp = new TestDirectory(); var s = AppSettings.CreateDefault(temp.Root); var service = new ProfileService(new DpapiSecretStore());
        var first = new ConnectionProfile { Name = "Geliştirici", TargetKey = "host:04", User = "DEV", Client = "100" };
        var second = new ConnectionProfile { Name = "Danışman", TargetKey = "host:04", User = "CONS", Client = "200" };
        await service.SaveAsync(s, first, "first-password", true); await service.SaveAsync(s, second, "second-password", false);
        var json = JsonNode.Parse(await File.ReadAllTextAsync(s.CredentialsFile))!; Assert.Equal("DEV", (string?)json["overridesByTarget"]?["host:04"]?["user"]);
        s.ActiveProfiles[first.TargetKey] = second.Id; await ProfileService.MaterializeAsync(s);
        var contents = await File.ReadAllTextAsync(s.CredentialsFile); Assert.Contains("CONS", contents); Assert.DoesNotContain("first-password", contents); Assert.DoesNotContain("second-password", contents); Assert.DoesNotContain("passwd", contents); Assert.Equal(2, s.Profiles.Count);
    }

    [Fact] public async Task ImportHonorsPrecedenceAndPreservesSource()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var temp = new TestDirectory(); var s = AppSettings.CreateDefault(Path.Combine(temp.Root, "managed")); var source = Path.Combine(temp.Root, "source.json");
        var original = """{"default":{"user":"DEFAULT","client":"100","lang":"TR","passwd":"keep-source"},"overrides":{"DEV":{"user":"SID"},"Display":{"user":"NAME"}},"overridesByTarget":{"host:04":{"user":"TARGET","adtBaseUrls":["https://host:44304"]}}}""";
        await File.WriteAllTextAsync(source, original); var systems = new[] { new SapSystem("a", "Display", "DEV", "host", 3204, "04", "") };
        var count = await new ProfileService(new DpapiSecretStore()).ImportAsync(s, source, systems);
        Assert.Equal(1, count); Assert.Equal("TARGET", s.Profiles[0].User); Assert.True(s.Profiles[0].Extra.ContainsKey("adtBaseUrls")); Assert.Equal(original, await File.ReadAllTextAsync(source));
        Assert.DoesNotContain("keep-source", JsonSerializer.Serialize(s, JsonDefaults.Options));
    }
}

public sealed class ClientTests
{
    [Fact] public void CodexDoesNotMistakeMultilineStringsOrDottedNamesForOwnedTables()
    {
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "config.toml");
        var original = "notes = '''\n[mcp_servers.sap_adt_mcp]\ncommand = \"keep-inside-string\"\n'''\n[mcp_servers.\"sap_adt_mcp.other\"]\ncommand = \"keep-dotted-server\"\n[mcp_servers.sap_adt_mcp]\ncommand = \"replace\"\n";
        File.WriteAllText(file, original); var preview = ClientIntegration.Preview("Codex", file, AppSettings.CreateDefault(temp.Root), "C:\\config.json", ["adt"]);
        Assert.Contains("keep-inside-string", preview.After); Assert.Contains("keep-dotted-server", preview.After); Assert.DoesNotContain("\"replace\"", preview.After); Assert.False(Tomlyn.Toml.Parse(preview.After).HasErrors);
    }
    [Fact] public async Task ClaudeMergesBacksUpAndProtectsConcurrentEdits()
    {
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "claude.json"); var original = """{"theme":"dark","mcpServers":{"other":{"command":"keep"}}}"""; await File.WriteAllTextAsync(file, original);
        var s = AppSettings.CreateDefault(temp.Root); var preview = ClientIntegration.Preview("Claude Desktop", file, s, "C:\\path with spaces\\settings.json", ["adt"]);
        Assert.Empty(preview.Conflicts); await ClientIntegration.ApplyAsync(preview, false); var saved = JsonNode.Parse(await File.ReadAllTextAsync(file))!;
        Assert.Equal("keep", (string?)saved["mcpServers"]?["other"]?["command"]); Assert.Equal("dark", (string?)saved["theme"]); Assert.Single(Directory.GetFiles(temp.Root, "*.bak"));
        var next = ClientIntegration.Preview("Claude Desktop", file, s, "C:\\config.json", ["adt"]); Assert.Single(next.Conflicts);
        await Assert.ThrowsAsync<InvalidOperationException>(() => ClientIntegration.ApplyAsync(next, false));
        await File.AppendAllTextAsync(file, " "); await Assert.ThrowsAsync<IOException>(() => ClientIntegration.ApplyAsync(next, true));
    }

    [Fact] public async Task CodexPreservesOtherTablesAndComments()
    {
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "config.toml");
        var original = "# keep comment\nmodel = \"example\"\n[mcp_servers.other]\ncommand = \"keep\"\n[mcp_servers.sap_adt_mcp]\ncommand = \"old\"\n[mcp_servers.sap_adt_mcp.env]\nOLD = \"value\"\n[projects.\"C:\\\\repo\"]\ntrust_level = \"trusted\"\n";
        await File.WriteAllTextAsync(file, original); var s = AppSettings.CreateDefault(temp.Root); var preview = ClientIntegration.Preview("Codex", file, s, "C:\\new path\\settings.json", ["adt", "rfc"]);
        Assert.Single(preview.Conflicts); await ClientIntegration.ApplyAsync(preview, true); var saved = await File.ReadAllTextAsync(file);
        Assert.Contains("# keep comment", saved); Assert.Contains("command = \"keep\"", saved); Assert.Contains("trust_level = \"trusted\"", saved); Assert.DoesNotContain("OLD", saved); Assert.Contains("--component", saved); Assert.False(Tomlyn.Toml.Parse(saved).HasErrors);
    }

    [Fact] public void InvalidExistingClientConfigurationIsNeverReplaced()
    {
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "config.toml"); File.WriteAllText(file, "[broken");
        Assert.Throws<InvalidDataException>(() => ClientIntegration.Preview("Codex", file, AppSettings.CreateDefault(temp.Root), "C:\\config.json", ["adt"])); Assert.Equal("[broken", File.ReadAllText(file));
    }

    [Fact] public async Task UnregisterOnlyRemovesEntriesOwnedByThisHost()
    {
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "claude.json"); await File.WriteAllTextAsync(file, """{"mcpServers":{"sap_adt_mcp":{"command":"someone-else"},"sap_rfc_call":{"command":"our-host"}}}""");
        await ClientIntegration.RemoveOwnedAsync(new("Claude Desktop", file, ["sap_adt_mcp", "sap_rfc_call"]), "our-host"); var saved = await File.ReadAllTextAsync(file);
        Assert.Contains("someone-else", saved); Assert.DoesNotContain("sap_rfc_call", saved);
    }
}

public sealed class PackageAndLoggingTests
{
    [Fact] public async Task AdtOnlyIgnoresAnUnavailableRfcLogDirectory()
    {
        using var temp = new TestDirectory(); var s = AppSettings.CreateDefault(temp.Root); s.Rfc.Selected = false; s.Adt.Selected = true;
        var occupied = Path.Combine(temp.Root, "not-a-directory"); await File.WriteAllTextAsync(occupied, "occupied"); s.Rfc.LogDirectory = occupied;
        var checks = await new PrerequisiteService(new PackageService(temp.Root)).CheckAsync(s);
        Assert.DoesNotContain(checks, c => c.Id == "storage" && c.State == CheckState.Blocked);
        Assert.DoesNotContain(checks, c => c.Id is "sdk" or "python" or "pyrfc");
    }
    [Theory] [InlineData("../outside.txt")] [InlineData("/absolute.txt")] [InlineData("file:stream")] [InlineData("folder/../../escape")]
    public void ArchiveTraversalIsRejected(string name)
    {
        using var temp = new TestDirectory(); var file = Path.Combine(temp.Root, "bad.zip");
        using (var archive = ZipFile.Open(file, ZipArchiveMode.Create)) { using var writer = new StreamWriter(archive.CreateEntry(name).Open()); writer.Write("bad"); }
        Assert.Throws<InvalidDataException>(() => PackageService.Extract(file, Path.Combine(temp.Root, "target")));
    }

    [Fact] public async Task HashMismatchCannotReplaceInstalledPackage()
    {
        using var temp = new TestDirectory(); var payload = Path.Combine(temp.Root, "payload"); Directory.CreateDirectory(Path.Combine(payload, "packages"));
        var file = Path.Combine(payload, "packages", "test.zip"); File.WriteAllText(file, "not a package"); var destination = Path.Combine(temp.Root, "installed"); Directory.CreateDirectory(destination); File.WriteAllText(Path.Combine(destination, "old.txt"), "keep");
        await Assert.ThrowsAsync<InvalidDataException>(() => new PackageService(payload).InstallAssetAsync(new() { Id = "test", FileName = "test.zip", Sha256 = new('0', 64) }, destination, temp.Root)); Assert.Equal("keep", File.ReadAllText(Path.Combine(destination, "old.txt")));
    }

    [Fact] public async Task PackageInstallStripsPrefixAndRecordsProvenance()
    {
        using var temp = new TestDirectory(); var payload = Path.Combine(temp.Root, "payload"); Directory.CreateDirectory(Path.Combine(payload, "packages")); var file = Path.Combine(payload, "packages", "test.zip");
        using (var archive = ZipFile.Open(file, ZipArchiveMode.Create)) { using var writer = new StreamWriter(archive.CreateEntry("prefix/bin/file.txt").Open()); writer.Write("works"); }
        var asset = new PackageAsset { Id = "test", Version = "1", FileName = "test.zip", Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))), StripPrefix = "prefix" };
        var destination = Path.Combine(temp.Root, "installed"); await new PackageService(payload).InstallAssetAsync(asset, destination, temp.Root);
        Assert.Equal("works", File.ReadAllText(Path.Combine(destination, "bin", "file.txt"))); Assert.True(File.Exists(Path.Combine(destination, ".sapmcp-package.json")));
    }

    [Fact] public async Task LogsRespectThresholdRotationAndRedaction()
    {
        using var temp = new TestDirectory(); var logger = new RollingLog(temp.Root, LogLevel.Warn, 120, 2);
        await logger.WriteAsync(LogLevel.Info, "hidden"); Assert.False(File.Exists(Path.Combine(temp.Root, "manager.jsonl")));
        for (var i = 0; i < 7; i++) await logger.WriteAsync(LogLevel.Error, "visible", new { password = "DO-NOT-LOG", authorization = "Bearer abc", detail = "passwd=secret-token" });
        var files = Directory.GetFiles(temp.Root, "manager.jsonl*"); Assert.InRange(files.Length, 1, 3);
        var all = string.Join("", files.Select(File.ReadAllText)); Assert.DoesNotContain("DO-NOT-LOG", all); Assert.DoesNotContain("secret-token", all); Assert.DoesNotContain("Bearer abc", all); Assert.Contains("redacted", all);
    }

    [Fact] public async Task OffLoggingWritesNoFiles()
    {
        using var temp = new TestDirectory(); await new RollingLog(temp.Root, LogLevel.Off).WriteAsync(LogLevel.Error, "hidden"); Assert.Empty(Directory.GetFiles(temp.Root));
    }

    [Theory] [InlineData("create_function_module", true, true)] [InlineData("create_function_module", false, false)] [InlineData("update_function_module_source", false, true)] [InlineData("activate_function_module", false, true)] [InlineData("search_objects", false, false)]
    public void AdtRfcGuardRunsBeforeForwarding(string tool, bool remote, bool blocked)
    {
        var args = JsonSerializer.SerializeToElement(new { remote_enabled = remote }); Assert.Equal(blocked, LaunchConfiguration.RequiresRfc(tool, args));
    }

    [Fact] public async Task EmptyComponentSelectionIsBlockedBeforeAnyPackageAccess()
    {
        using var temp = new TestDirectory(); var s = AppSettings.CreateDefault(temp.Root); s.Rfc.Selected = false; s.Adt.Selected = false; var package = new PackageService(temp.Root);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new DeploymentService(package, new(package)).InstallSelectedAsync(s)); Assert.False(Directory.Exists(s.PackageDirectory));
    }
}
