using System.Text.Json;
using System.Text.Json.Nodes;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class Preview8Tests
{
    [Theory]
    [InlineData("Codex")]
    [InlineData("Claude Desktop")]
    [InlineData("Claude Code")]
    public async Task RepeatedRegistrationIsSemanticNoOpWithNoNewBackup(string client)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var file=Path.Combine(t.Root,client=="Codex"?"client.toml":"client.json");
        var first=ClientIntegration.Preview(client,file,s,"C:\\sample config\\settings.json",["adt","rfc"]);
        Assert.All(first.Entries,e=>Assert.Equal(ClientEntryState.New,e.State));await ClientIntegration.ApplyAsync(first,false);
        var text=await File.ReadAllTextAsync(file);
        if(client!="Codex")
        {
            var root=JsonNode.Parse(text)!.AsObject();
            var servers=root["mcpServers"]!.AsObject();
            foreach(var key in servers.Select(k=>k.Key).ToArray())
            {
                var value=servers[key]!.AsObject();var reversed=new JsonObject();foreach(var item in value.Reverse())reversed[item.Key]=item.Value?.DeepClone();
                if(client=="Claude Code")reversed.Remove("type");servers[key]=reversed;
            }
            text=root.ToJsonString();await File.WriteAllTextAsync(file,text);
        }
        var next=ClientIntegration.Preview(client,file,s,"C:\\sample config\\settings.json",["adt","rfc"]);
        Assert.Empty(next.Conflicts);Assert.All(next.Entries,e=>Assert.Equal(ClientEntryState.Current,e.State));Assert.False(next.Changed);
        Assert.Null(await ClientIntegration.ApplyAsync(next,false));Assert.Empty(Directory.GetFiles(t.Root,"*.bak"));Assert.Equal(text,await File.ReadAllTextAsync(file));
    }
    [Fact]
    public async Task ClaudeCodePreservesProjectsAndUnrelatedSettingsDuringAddAndRemove()
    {
        using var t=new TestDirectory();var file=Path.Combine(t.Root,"code.json");
        var root=JsonNode.Parse("""{"projects":{"fictional-project":{"mcpServers":{"sap_adt_mcp":{"command":"project-owned"}}}},"theme":"dark","mcpServers":{"unrelated":{"command":"keep"}}}""")!;
        await File.WriteAllTextAsync(file,root.ToJsonString());var s=AppSettings.CreateDefault(t.Root);
        var preview=ClientIntegration.Preview("Claude Code",file,s,"C:\\settings.json",["adt"]);await ClientIntegration.ApplyAsync(preview,false);
        var after=JsonNode.Parse(await File.ReadAllTextAsync(file))!;Assert.True(JsonNode.DeepEquals(root["projects"],after["projects"]));Assert.Equal("keep",(string?)after["mcpServers"]?["unrelated"]?["command"]);
        Assert.DoesNotContain("project-owned",ClientIntegration.Details(preview));
        await ClientIntegration.RemoveOwnedAsync(new("Claude Code",file,["sap_adt_mcp"]),s.HostExecutable);
        after=JsonNode.Parse(await File.ReadAllTextAsync(file))!;Assert.True(JsonNode.DeepEquals(root,after));
    }
    [Fact]
    public async Task BatchPreflightPreventsEarlierTargetWriteWhenLaterTargetChanged()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var a=Path.Combine(t.Root,"a.json");var b=Path.Combine(t.Root,"b.json");
        var previews=new[]{ClientIntegration.Preview("Claude Desktop",a,s,"C:\\settings.json",["adt"]),ClientIntegration.Preview("Claude Code",b,s,"C:\\settings.json",["adt"])};
        await File.WriteAllTextAsync(b,"{}");await Assert.ThrowsAsync<IOException>(()=>ClientIntegration.ApplyAllAsync(previews,false));Assert.False(File.Exists(a));
    }
    [Fact]
    public async Task BatchReportsPerTargetFailureAndContinues()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var blocked=Path.Combine(t.Root,"locked.json");await File.WriteAllTextAsync(blocked,"{}");
        var good=Path.Combine(t.Root,"good.json");var previews=new[]{ClientIntegration.Preview("Claude Code",blocked,s,"C:\\settings.json",["adt"]),ClientIntegration.Preview("Claude Desktop",good,s,"C:\\settings.json",["adt"])};
        using var held=new FileStream(blocked,FileMode.Open,FileAccess.Read,FileShare.Read);
        var results=await ClientIntegration.ApplyAllAsync(previews,false);Assert.False(results[0].Success);Assert.NotNull(results[0].BackupPath);Assert.True(results[1].Success);Assert.True(File.Exists(good));
    }
    [Fact]
    public async Task OldSettingsAndPreparedStateRemainCompatible()
    {
        var old=JsonSerializer.Deserialize<AppSettings>("{}",JsonDefaults.Options)!;Assert.Null(old.BasicSetup);Assert.False(old.ClaudeCodeEnabled);
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);s.BasicSetup=new(DateTimeOffset.UtcNow,true);s.ClaudeCodeEnabled=true;
        var store=new SettingsStore(Path.Combine(t.Root,"config","settings.json"));await store.SaveAsync(s);var loaded=await store.LoadAsync();Assert.Equal(s.BasicSetup,loaded.BasicSetup);Assert.True(BasicInstallation.AdtOnly(loaded));Assert.True(loaded.ClaudeCodeEnabled);
    }
    private static void Package(AppSettings s,string component,string version)
    {
        var root=Path.Combine(s.PackageDirectory,component);Directory.CreateDirectory(Path.Combine(root,"dist"));File.WriteAllText(Path.Combine(root,"dist","server.js"),"fixture");
        if(component=="adt")File.WriteAllText(Path.Combine(root,"dist","basic-connection.js"),"fixture");s.Component(component).Version=version;s.Component(component).Installed=true;
    }
    [Fact]
    public void PackageStateUsesDiskAndDoesNotDowngradeOrRepairSameVersion()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);Assert.Equal("Missing",BasicInstallation.Package(s,"adt","2.0.0"));
        Package(s,"adt","1.0.0");Assert.Equal("Update",BasicInstallation.Package(s,"adt","2.0.0"));Assert.Equal("Current",BasicInstallation.Package(s,"adt","1.0.0"));Assert.Equal("Current",BasicInstallation.Package(s,"adt","0.9.0"));
        File.Delete(Path.Combine(s.PackageDirectory,"adt","dist","server.js"));Assert.Equal("Repair",BasicInstallation.Package(s,"adt","1.0.0"));
        File.WriteAllText(Path.Combine(s.PackageDirectory,"adt",".sapmcp-package.json"),"broken");Assert.Equal("Repair",BasicInstallation.Package(s,"adt","2.0.0"));
    }
    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public async Task PreparationSkipsReadyPrerequisitesAndMaintenanceNeverRunsInstallers(bool maintenance,bool missingNode)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);if(maintenance)s.BasicSetup=new(DateTimeOffset.UtcNow,true);
        Package(s,"adt","1.0.0");s.Adt.Installed=false; // fixture package, not a completed setup marker
        var payload=Path.Combine(t.Root,"payload");Directory.CreateDirectory(payload);
        await File.WriteAllTextAsync(Path.Combine(payload,"catalog.json"),JsonSerializer.Serialize(new PackageCatalog{Assets=[new(){Id="adt",Version="1.0.0",FileName="adt.zip",Sha256=new string('a',64)}]},JsonDefaults.Options));
        var installs=new List<string>();
        var service=new BasicPreparationService(new(payload))
        {
            CheckOverride=(_,_)=>Task.FromResult(new List<Requirement>{new("node","Node","24",missingNode?CheckState.Missing:CheckState.Ready,"",true)}),
            InstallOverride=(_,ids,_,_)=>{installs.AddRange(ids);return Task.CompletedTask;}
        };
        if(missingNode)await Assert.ThrowsAsync<InvalidOperationException>(()=>service.PrepareAsync(s,"",true,null,default));
        else {var prepared=await service.PrepareAsync(s,"",true,null,default);Assert.NotNull(prepared.BasicSetup);Assert.True(prepared.BasicSetup!.AdtOnly);}
        Assert.Empty(installs);
    }
    [Fact]
    public async Task FirstPreparationInstallsOnlyMissingPrerequisite()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);s.BasicSetup=new(null,true);Assert.False(BasicInstallation.IsMaintenance(s));var payload=Path.Combine(t.Root,"payload");Directory.CreateDirectory(payload);
        await File.WriteAllTextAsync(Path.Combine(payload,"catalog.json"),JsonSerializer.Serialize(new PackageCatalog{Assets=[new(){Id="adt",Version="1.0.0",FileName="adt.zip",Sha256=new string('a',64)}]},JsonDefaults.Options));
        var installed=false;var calls=new List<string>();
        var service=new BasicPreparationService(new(payload))
        {
            CheckOverride=(_,_)=>Task.FromResult(new List<Requirement>{new("node","Node","24",installed?CheckState.Ready:CheckState.Missing,"",true),new("storage","Disk","",CheckState.Ready,"")}),
            InstallOverride=(_,ids,_,_)=>{calls.AddRange(ids);installed=true;return Task.CompletedTask;},
            InstallPackageOverride=(asset,_,_)=>{Package(s,"adt",asset.Version);return Task.CompletedTask;}
        };
        var result=await service.PrepareAsync(s,"",true,null,default);Assert.Equal(new[]{"node"},calls);Assert.NotNull(result.BasicSetup);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MaintenanceUpdatesAdtWhileSkippingUnavailableRfcAndNeverInstallsPrerequisites(bool brokenSameVersion)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);s.BasicSetup=new(DateTimeOffset.UtcNow,false);s.SdkDirectory=Path.Combine(t.Root,"sdk");
        Package(s,"adt",brokenSameVersion?"2.0.0":"1.0.0");Package(s,"rfc","1.0.0");
        if(brokenSameVersion)File.Delete(Path.Combine(s.PackageDirectory,"adt","dist","server.js"));
        var payload=Path.Combine(t.Root,"payload");Directory.CreateDirectory(payload);
        await File.WriteAllTextAsync(Path.Combine(payload,"catalog.json"),JsonSerializer.Serialize(new PackageCatalog{Assets=[new(){Id="adt",Version="2.0.0",FileName="adt.zip",Sha256=new string('a',64)},new(){Id="rfc",Version="2.0.0",FileName="rfc.zip",Sha256=new string('b',64)}]},JsonDefaults.Options));
        var calls=new List<string>();
        var service=new BasicPreparationService(new(payload))
        {
            CheckOverride=(_,_)=>Task.FromResult(new List<Requirement>{new("node","Node","24",CheckState.Ready,"",true),new("python","Python","",CheckState.Missing,"",true),new("sdk","SDK","",CheckState.Missing,"")}),
            InstallOverride=(_,_,_,_)=>throw new Exception("Maintenance must not install a prerequisite"),
            InstallPackageOverride=(asset,_,_)=>{calls.Add(asset.Id);Package(s,asset.Id,asset.Version);return Task.CompletedTask;}
        };
        if(brokenSameVersion){await Assert.ThrowsAsync<InvalidOperationException>(()=>service.PrepareAsync(s,s.SdkDirectory,false,null,default));Assert.Empty(calls);}
        else{var updated=await service.PrepareAsync(s,s.SdkDirectory,false,null,default);Assert.Equal(new[]{"adt"},calls);Assert.Equal("2.0.0",updated.Adt.Version);Assert.Equal("1.0.0",updated.Rfc.Version);Assert.False(service.RfcReady);}
    }

}
