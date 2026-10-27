using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using SapMcp.Core;

internal static class AdtVerification
{
    public static async Task RunAsync(AppSettings source, Func<string, Func<Task>, Task> step)
    {
        var root = Path.Combine(source.DataDirectory, "ADT bağlantı sınaması"); Directory.CreateDirectory(root);
        using var caKey = RSA.Create(2048);
        var caRequest = new CertificateRequest("CN=Fictional test CA", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        using var ca = caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(10));
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder(); san.AddDnsName("localhost"); san.AddIpAddress(IPAddress.Loopback); request.CertificateExtensions.Add(san.Build());
        using var cert = request.Create(ca, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2), RandomNumberGenerator.GetBytes(16));
        var caFile = Path.Combine(root, "root.cer"); await File.WriteAllBytesAsync(caFile, ca.Export(X509ContentType.Cert));
        await File.WriteAllTextAsync(Path.Combine(root, "leaf.pem"), cert.ExportCertificatePem()); await File.WriteAllTextAsync(Path.Combine(root, "leaf.key"), key.ExportPkcs8PrivateKeyPem());
        var fixture = Path.Combine(root, "fixture.cjs"); var ready = Path.Combine(root, "ready.json");
        await File.WriteAllTextAsync(fixture, """
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),https=require('node:https');
let auth=0;const root=__dirname;const servers=[];
function handler(req,res){
 if(!req.headers.authorization){res.writeHead(401,{'WWW-Authenticate':'Basic realm="fictional"'});res.end();return;}
 auth++;fs.writeFileSync(path.join(root,'count.json'),JSON.stringify({auth}));
 if(req.headers.authorization!=='Basic '+Buffer.from('TEST:  fixture şifre  ').toString('base64')){res.writeHead(401);res.end();return;}
 res.writeHead(200,{'content-type':'application/xml','x-csrf-token':'fixture-token'});
 res.end(req.url.includes('/discovery')?'<app:service xmlns:app="http://www.w3.org/2007/app"><app:workspace><app:collection href="/sap/bc/adt/repository/informationsystem/search"/></app:workspace></app:service>':'<compatibility:graph xmlns:compatibility="http://www.sap.com/adt/compatibility"><nodes/><edges/></compatibility:graph>');
}
(async()=>{const plain=http.createServer(handler),secure=https.createServer({key:fs.readFileSync(path.join(root,'leaf.key')),cert:fs.readFileSync(path.join(root,'leaf.pem'))},handler);servers.push(plain,secure);for(const s of servers)await new Promise(r=>s.listen(0,'127.0.0.1',r));fs.writeFileSync(path.join(root,'ready.json'),JSON.stringify({http:'http://127.0.0.1:'+plain.address().port,https:'https://127.0.0.1:'+secure.address().port}));})();
process.stdin.resume();process.stdin.on('end',()=>{for(const s of servers){s.closeAllConnections();s.close();}setTimeout(()=>process.exit(0),30);});
""");
        var start = new ProcessStartInfo(source.NodeExecutable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true, RedirectStandardOutput = true };
        start.ArgumentList.Add(fixture);
        using var server = Process.Start(start)!;
        try
        {
            for (var i = 0; i < 200 && !File.Exists(ready) && !server.HasExited; i++) await Task.Delay(25);
            Ensure(File.Exists(ready), "ADT local fixture did not start");
            using var endpoints = JsonDocument.Parse(await File.ReadAllTextAsync(ready));
            var s = AppSettings.CreateDefault(Path.Combine(root, "manager")); s.NodeExecutable = source.NodeExecutable; s.HostExecutable = source.HostExecutable; s.PowerShellExecutable = source.PowerShellExecutable; s.PackageDirectory = source.PackageDirectory;
            s.Rfc.Selected = false; s.Adt.Selected = true; s.Adt.Installed = true;
            var store = new SettingsStore(Path.Combine(s.DataDirectory, "config", "settings.json")); await store.SaveAsync(s);
            var manager = new ConnectionManager(store); s = await manager.SelectSourceAsync(s, "managed"); s = await manager.SaveSystemAsync(s, new() { Name = "Fictional local ADT", Host = "127.0.0.1", Sysnr = "00" });
            var profiles = new ProfileService(new DpapiSecretStore());
            var active = new ConnectionProfile { Name = "Active without extra trust", User = "TEST", TargetKey = "127.0.0.1:00", AdtBaseUrl = endpoints.RootElement.GetProperty("https").GetString()! };
            await profiles.SaveAsync(s, active, "  fixture şifre  ", true);
            var selected = new ConnectionProfile { Name = "Selected trusted", User = "TEST", TargetKey = active.TargetKey, AdtBaseUrl = active.AdtBaseUrl };
            selected.AdtCertificates = (await AdtProfiles.StoreCertificatesAsync(s, selected, await AdtProfiles.ReadCertificatesAsync(caFile))).ToList();
            await profiles.SaveAsync(s, selected, "  fixture şifre  ", false); await store.SaveAsync(s);
            async Task<JsonElement> Diagnose(string? id)
            {
                await using var session = await McpSession.StartAsync(s, store.FilePath, "adt", profileId: id);
                var result = await session.RequestAsync("tools/call", new { name = "diagnose_connection", arguments = new { target_key = active.TargetKey } });
                return result.GetProperty("structuredContent").Clone();
            }
            await step("Manager selected-profile TLS uses actual MCP without changing active mapping", async () =>
            {
                var before = await File.ReadAllTextAsync(s.CredentialsFile); var result = await Diagnose(selected.Id);
                Ensure(result.GetProperty("success").GetBoolean(), "Selected trusted TLS profile did not verify");
                Ensure(before == await File.ReadAllTextAsync(s.CredentialsFile) && s.ActiveProfiles[active.TargetKey] == active.Id, "Selected test changed shared mapping");
                Ensure(!Directory.EnumerateDirectories(Path.Combine(s.DataDirectory, "cache", "adt-tests")).Any(), "Temporary test mapping not cleaned");
            });
            await step("Actual MCP active profile cannot inherit selected profile trust", async () =>
            {
                var result = await Diagnose(null);
                Ensure(!result.GetProperty("success").GetBoolean() && result.GetProperty("stages").EnumerateArray().Any(r => r.GetProperty("stage").GetString() == "tls" && r.GetProperty("status").GetString() == "failed"), "CA trust leaked across profiles");
            });
            await step("Activated profile matches Manager diagnosis and real ping_system", async () =>
            {
                s.ActiveProfiles[active.TargetKey] = selected.Id; await ProfileService.MaterializeAsync(s); await store.SaveAsync(s);
                Ensure((await Diagnose(null)).GetProperty("success").GetBoolean(), "Active profile differed from selected Manager test");
                await using var session = await McpSession.StartAsync(s, store.FilePath, "adt");
                var result = await session.RequestAsync("tools/call", new { name = "ping_system", arguments = new { target_key = active.TargetKey } });
                Ensure(!result.TryGetProperty("isError", out var error) || !error.GetBoolean(), "Verified MCP ping failed");
            });
            await step("Actual MCP HTTP custom port reports plaintext warning and valid ADT content", async () =>
            {
                selected.AdtBaseUrl = endpoints.RootElement.GetProperty("http").GetString()! + "/sap/bc/adt";
                await profiles.SaveAsync(s, selected, null, true); await store.SaveAsync(s);
                var result = await Diagnose(null); Ensure(result.GetProperty("success").GetBoolean(), "HTTP ADT fixture failed");
                Ensure(result.GetProperty("warnings").EnumerateArray().Any(w => w.GetString() == "HTTP_CREDENTIALS_UNENCRYPTED"), "HTTP warning missing");
                var support = AdtProfiles.SupportReport(selected, result);
                foreach (var value in new[] { "127.0.0.1", "  fixture şifre  ", "fixture-token", "TEST", "Fictional test CA" }) Ensure(!support.Contains(value, StringComparison.Ordinal), "Support export leaked fixture data");
            });
            await step("Legacy blank ADT URL is blocked without credentials or automatic port guesses", async () =>
            {
                var before = await File.ReadAllTextAsync(Path.Combine(root, "count.json")); selected.AdtBaseUrl = "";
                await profiles.SaveAsync(s, selected, null, true); await store.SaveAsync(s);
                var result = await Diagnose(null);
                Ensure(!result.GetProperty("success").GetBoolean() && result.GetProperty("stages")[0].GetProperty("code").GetString() == "ADT_URL_REQUIRED", "Legacy blank URL was guessed");
                Ensure(before == await File.ReadAllTextAsync(Path.Combine(root, "count.json")), "Blank URL sent credentials");
            });
        }
        finally
        {
            server.StandardInput.Close();
            try { await server.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)); } catch (TimeoutException) { server.Kill(true); }
        }
    }
    private static void Ensure(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
