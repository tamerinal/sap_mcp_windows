using System.IO.Compression;
using System.Text.Json;
using SapMcp.Core;

// Explicit opt-in only: fixture contains existing DPAPI references, never passwords.
internal static class BasicLiveAcceptance
{
    internal static async Task RunAsync(string publish,string payload,string fixture,string output)
    {
        using var input=JsonDocument.Parse(await File.ReadAllTextAsync(fixture));
        var root=Path.Combine(Path.GetFullPath(output),"isolated-"+Guid.NewGuid().ToString("N"));var settings=AppSettings.CreateDefault(root);
        settings.NodeExecutable=input.RootElement.GetProperty("nodeExecutable").GetString()!;settings.HostExecutable=Path.Combine(Path.GetFullPath(publish),"SapMcpHost.exe");
        if(input.RootElement.TryGetProperty("pythonExecutable",out var python))settings.PythonExecutable=python.GetString()!;
        if(input.RootElement.TryGetProperty("sdkDirectory",out var sdk))settings.SdkDirectory=sdk.GetString()!;
        var rfcReady=Directory.Exists(settings.SdkDirectory)&&File.Exists(settings.PythonExecutable);
        var packages=new PackageService(Path.GetFullPath(payload));
        foreach(var component in new[]{"adt","rfc"}.Where(c=>c=="adt"||rfcReady)){
            var asset=packages.LoadCatalog().Find(component);ZipFile.ExtractToDirectory(Path.Combine(payload,"packages",asset.FileName),Path.Combine(settings.PackageDirectory,component));
            settings.Component(component).Installed=settings.Component(component).Selected=true;settings.Component(component).Version=asset.Version;
        }
        var store=new SettingsStore(Path.Combine(root,"config","settings.json"));var manager=new ConnectionManager(store);settings=await manager.SelectSourceAsync(settings,"managed");
        var results=new List<object>();var failed=false;
        foreach(var row in input.RootElement.GetProperty("cases").EnumerateArray()){
            var system=new ManagedConnection{Sid=row.GetProperty("sid").GetString()!,Name=row.GetProperty("name").GetString()!,Host=row.GetProperty("host").GetString()!,Sysnr=row.GetProperty("sysnr").GetString()!};settings=await manager.SaveSystemAsync(settings,system);
            var p=new ConnectionProfile{Sid=system.Sid,Name=system.Name,TargetKey=system.TargetKey,User=row.GetProperty("user").GetString()!,Client=row.GetProperty("client").GetString()!,Language=row.GetProperty("language").GetString()!,SecretRef=row.GetProperty("secretRef").GetString()!};
            settings.Profiles=[p];settings.ActiveProfiles=new(){{p.TargetKey,p.Id}};await ProfileService.MaterializeAsync(settings);await store.SaveAsync(settings);
            var draft=new BasicDraft{Sid=system.Sid,ProfileId=p.Id,SystemId=system.Id,Host=system.Host,Sysnr=system.Sysnr,User=p.User,Client=p.Client,Language=p.Language};
            var consents=new List<string>();
            try {
                var result=await new BasicConnectionService(new DpapiSecretStore()).ValidateAsync(settings,store,draft,ct=>new RfcReadinessService(packages).CheckAsync(settings,rfcReady,ct),(prompt,_)=>{
                    consents.Add(prompt.ToString());return Task.FromResult(prompt!=BasicPrompt.ExtendedScan); // No live full sweep in acceptance.
                },new Progress<string>(message=>{if(message.StartsWith("RFC",StringComparison.Ordinal))Console.WriteLine(system.Sid+": "+message);}));
                var discovery=result.Discovery;var expected=row.GetProperty("expectedCandidateUrl").GetString()!;
                if(!discovery.Candidates.Any(c=>c.Url==expected))discovery=await new BasicDiscoveryService().DiscoverAsync(settings,draft);
                var found=BasicPolicy.Usable(discovery).Any(c=>c.Url==expected);bool? negativeExcluded=null;
                if(row.TryGetProperty("negativeUrl",out var negative)){
                    var negativeDraft=BasicPolicy.Copy(draft);negativeDraft.ManualUrl=negative.GetString()!;
                    var negativeDiscovery=await new BasicDiscoveryService().DiscoverAsync(settings,negativeDraft);negativeExcluded=!BasicPolicy.Usable(negativeDiscovery).Any()&&negativeDiscovery.Candidates.Any(c=>c.HttpStatus==503);
                }
                var passed=(!rfcReady||result.Rfc.Success)&&result.Adt.Success&&result.Settings?.Profiles.Single().AllowInsecureTls==true&&found&&negativeExcluded!=false;
                results.Add(new{name=system.Sid,passed,found,negativeExcluded,selectedUrl=result.Url,adt=result.Adt,rfc=result.Rfc,readinessCode=result.Readiness?.Code,consents,tlsVerification=false,manualUrlUsed=false,preferredUrlUsed=false,liveFullSweep=false,source="packaged Manager/Host/ADT"});
                Console.WriteLine($"{(passed?"PASS":"FAIL")} Basic live {system.Sid}: ADT={result.Adt.Code}; RFC={result.Rfc.Code}; expected candidate={found}");
                failed|=!passed;if(result.Settings!=null)settings=result.Settings;
            }catch(Exception ex){failed=true;results.Add(new{name=system.Sid,passed=false,errorType=ex.GetType().Name});Console.WriteLine($"FAIL Basic live {system.Sid}: {ex.GetType().Name}");}
            await AtomicFile.WriteAsync(Path.Combine(output,"basic-live-results.json"),JsonSerializer.Serialize(results,JsonDefaults.Options),privateData:true);
        }
        if(failed)throw new InvalidDataException("Some live acceptance cases failed; consult the private result report.");
    }
}
