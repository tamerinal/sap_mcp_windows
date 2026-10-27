using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using SapMcp.Core;

if (args.Length == 2 && args[0] == "--probe") { await Probe.RunAsync(args[1]); return; }
if (args.Length < 3) throw new ArgumentException("publish-root payload-root verification-root [sdk-directory]");
var publish = Path.GetFullPath(args[0]); var sourcePayload = Path.GetFullPath(args[1]); var verification = Path.GetFullPath(args[2]);
Directory.CreateDirectory(verification);
var report = new List<object>();
async Task Step(string name, Func<Task> test)
{
    var started = DateTime.UtcNow;
    try { await test(); report.Add(new { name, status = "passed", durationMs = (DateTime.UtcNow - started).TotalMilliseconds }); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { report.Add(new { name, status = "failed", error = Redaction.Text(ex.Message) }); throw; }
    finally { await AtomicFile.WriteAsync(Path.Combine(verification, "report.json"), JsonSerializer.Serialize(report, JsonDefaults.Options)); }
}
void Ensure(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

var offlinePayload = Path.Combine(verification, "offline-payload"); Directory.CreateDirectory(Path.Combine(offlinePayload, "packages"));
var sourceCatalog = new PackageService(sourcePayload).LoadCatalog();
await Step("Offline package completeness and SHA-256", async () =>
{
    foreach (var asset in sourceCatalog.Assets)
    {
        var source = new[] { Path.Combine(sourcePayload, "packages", asset.FileName), Path.Combine(sourcePayload, "cache", asset.FileName) }.First(File.Exists);
        await PackageService.VerifyAsync(source, asset.Sha256);
        File.Copy(source, Path.Combine(offlinePayload, "packages", asset.FileName), true);
        asset.Url = ""; // Any accidental online dependency now fails, without changing network settings.
    }
    await AtomicFile.WriteAsync(Path.Combine(offlinePayload, "catalog.json"), JsonSerializer.Serialize(sourceCatalog, JsonDefaults.Options));
});
var data = Path.Combine(verification, "Kullanıcı verisi Türkçe");
var settings = AppSettings.CreateDefault(data); settings.Rfc.Selected = false; settings.Adt.Selected = true;
settings.HostExecutable = Path.Combine(publish, "SapMcpHost.exe");
settings.LandscapeFile = Path.Combine(data, "SAPUILandscape.xml"); Directory.CreateDirectory(data);
await File.WriteAllTextAsync(settings.LandscapeFile, "<Landscape><Services><Service type='SAPGUI' uuid='test' name='Offline test' systemid='TST' server='127.0.0.1:3200'/></Services></Landscape>");
var store = new SettingsStore(Path.Combine(data, "config", "settings.json"));
await Step("Managed JSON generates landscape without SAP GUI and preserves external XML", async () =>
{
    var externalBefore = await File.ReadAllBytesAsync(settings.LandscapeFile);
    var manager = new ConnectionManager(store);
    settings = await manager.SelectSourceAsync(settings, "managed");
    settings = await manager.SaveSystemAsync(settings, new() { Name = "Offline managed test", Host = "127.0.0.1", Sysnr = "00", Sid = "TST" });
    Ensure(settings.LandscapeFile == ConnectionManager.GeneratedPath(settings), "Managed projection not selected");
    Ensure(LandscapeService.Load(settings.LandscapeFile).Single().TargetKey == "127.0.0.1:00", "Managed target mismatch");
    var externalAfter = await File.ReadAllBytesAsync(settings.ExternalLandscapeFile!);
    Ensure(externalBefore.SequenceEqual(externalAfter), "External XML changed");
});
var packages = new PackageService(offlinePayload); var prerequisites = new PrerequisiteService(packages);
var deploy = new DeploymentService(packages, prerequisites);
await Step("Missing Node blocks MCP installation", async () =>
{
    settings.NodeExecutable = Path.Combine(data, "not-installed", "node.exe");
    try { await deploy.InstallSelectedAsync(settings); throw new Exception("Missing runtime was accepted"); }
    catch (InvalidOperationException ex) when (ex.Message.StartsWith(UiText.Get("Text010"), StringComparison.Ordinal)) { }
    settings.NodeExecutable = Path.Combine(data, "runtimes", "node", "node.exe");
});
await Step("Offline Node installation", () => prerequisites.InstallAsync(settings, ["node"], new Progress<string>(Console.WriteLine)));
await Step("ADT-only requires no Python or SDK", async () =>
{
    var checks = await prerequisites.CheckAsync(settings);
    Ensure(!checks.Any(c => c.Id is "python" or "pyrfc" or "sdk"), "ADT-only requested RFC requirements");
    await deploy.InstallSelectedAsync(settings); await store.SaveAsync(settings);
    Ensure(!Directory.Exists(Path.Combine(settings.PackageDirectory, "rfc")), "RFC was installed in ADT-only mode");
});
await Step("Published ADT host initialize, tools/list and landscape", async () =>
{
    await using var session = await McpSession.StartAsync(settings, store.FilePath, "adt");
    var tools = await session.RequestAsync("tools/list"); Ensure(tools.GetProperty("tools").GetArrayLength() > 10, "ADT tools missing");
    var systems = await session.RequestAsync("tools/call", new { name = "list_systems", arguments = new { } });
    Ensure(!systems.TryGetProperty("isError", out var failed) || failed.ValueKind != JsonValueKind.True, "Landscape call failed");
    Ensure(systems.GetRawText().Contains("127.0.0.1:00"), "ADT could not resolve generated target");
});
await Step("ADT RFC request blocked before forwarding", async () =>
{
    await using var session = await McpSession.StartAsync(settings, store.FilePath, "adt");
    var result = await session.RequestAsync("tools/call", new { name = "create_function_module", arguments = new { remote_enabled = true } });
    Ensure(result.GetProperty("isError").GetBoolean(), "Unsafe request was not blocked"); Ensure(result.GetRawText().Contains("RFC_UNAVAILABLE"), "Missing guard error");
});
await Step("Offline Python and PyRFC installation", async () =>
{
    settings.Rfc.Selected = true; settings.Adt.Selected = false;
    await prerequisites.InstallAsync(settings, ["python", "pyrfc"], new Progress<string>(Console.WriteLine));
    var version = await ProcessRunner.RunAsync(settings.PythonExecutable, ["--version"]); Ensure(version.Output.Contains("3.11."), "Python version mismatch");
    await deploy.InstallSelectedAsync(settings); await store.SaveAsync(settings);
});
await Step("SDK absent blocks RFC without network access", async () =>
{
    var check = await PrerequisiteService.ValidateSdkAsync(settings); Ensure(check.State == CheckState.Waiting, "SDK wait state missing");
    var run = await ProcessRunner.RunAsync(settings.HostExecutable, ["--component", "rfc", "--config", store.FilePath]); Ensure(run.ExitCode == 2, "RFC started without SDK"); Ensure(run.Output.Trim().Length == 0, "Host polluted protocol stdout");
});
if (args.Length > 3 && Directory.Exists(args[3]))
{
    await Step("Diagnostics reports waiting SDK as incomplete", async () =>
    {
        var run = await ProcessRunner.RunAsync(settings.HostExecutable, ["--diagnose", "--json", "--config", store.FilePath]);
        Ensure(run.ExitCode == 2, "Waiting SDK reported as complete");
        using var diagnosis = JsonDocument.Parse(run.Output);
        Ensure(diagnosis.RootElement.GetProperty("checks").EnumerateArray().Any(c => c.GetProperty("id").GetString() == "sdk" && c.GetProperty("state").GetString() == "Waiting"), "SDK wait diagnosis missing");
    });
    await Step("Provided SDK loads with managed Python/PyRFC", async () =>
    {
        settings.SdkDirectory = Path.GetFullPath(args[3]);
        var sdk = await PrerequisiteService.ValidateSdkAsync(settings); Ensure(sdk.State == CheckState.Ready, sdk.Detail); await store.SaveAsync(settings);
    });
    await Step("RFC-only host handshake and tools/list", async () =>
    {
        await using var session = await McpSession.StartAsync(settings, store.FilePath, "rfc");
        var tools = await session.RequestAsync("tools/list"); Ensure(tools.GetProperty("tools").EnumerateArray().Any(t => t.GetProperty("name").GetString() == "sap_rfc_list_systems"), "RFC tool missing");
        var result = await session.RequestAsync("tools/call", new { name = "sap_rfc_list_systems", arguments = new { } }); Ensure(!result.TryGetProperty("isError", out var failed) || !failed.GetBoolean(), "RFC landscape call failed");
        Ensure(result.GetRawText().Contains("127.0.0.1:00"), "RFC could not resolve generated target");
    });
}
await Step("Two selected MCP components start independently", async () =>
{
    settings.Adt.Selected = true; await store.SaveAsync(settings);
    await using var session = await McpSession.StartAsync(settings, store.FilePath, "adt");
    var result = await session.RequestAsync("tools/list"); Ensure(result.GetProperty("tools").GetArrayLength() > 10, "ADT tools missing");
});
await Step("Off logging creates no operation/session files", async () =>
{
    settings.Adt.LogLevel = LogLevel.Off; settings.Adt.LogDirectory = Path.Combine(data, "logs-off"); await store.SaveAsync(settings);
    await using (var session = await McpSession.StartAsync(settings, store.FilePath, "adt")) { await session.RequestAsync("tools/list"); }
    Ensure(!Directory.Exists(settings.Adt.LogDirectory) || !Directory.EnumerateFiles(settings.Adt.LogDirectory, "*.jsonl*").Any(), "Off produced a log");
});
await Step("Fake upstream receives no blocked mutation", async () =>
{
    var fakeRoot = Path.Combine(verification, "fake-upstream"); var fakePackage = Path.Combine(fakeRoot, "adt", "dist"); Directory.CreateDirectory(fakePackage);
    var marker = Path.Combine(fakeRoot, "mutation-marker.txt");
    var script = "const readline = require('node:readline'); const fs = require('node:fs'); const rl=readline.createInterface({input:process.stdin}); rl.on('line',line=>{ const m=JSON.parse(line); if(m.method==='initialize') console.log(JSON.stringify({jsonrpc:'2.0',id:m.id,result:{protocolVersion:'2024-11-05',capabilities:{},serverInfo:{name:'fixture',version:'1'}}})); else if(m.method==='tools/call'){fs.writeFileSync(" + JsonSerializer.Serialize(marker) + ",'BAD');console.log(JSON.stringify({jsonrpc:'2.0',id:m.id,result:{content:[]}}));}});";
    await File.WriteAllTextAsync(Path.Combine(fakePackage, "server.js"), script); settings.PackageDirectory = fakeRoot; settings.Rfc.Installed = false; await store.SaveAsync(settings);
    await using var session = await McpSession.StartAsync(settings, store.FilePath, "adt");
    var result = await session.RequestAsync("tools/call", new { name = "activate_function_module", arguments = new { target_key = "127.0.0.1:00" } });
    Ensure(result.GetProperty("isError").GetBoolean() && !File.Exists(marker), "Blocked operation reached upstream");
});
Console.WriteLine($"Verified {report.Count} release scenarios. No SAP login or mutation was performed.");
