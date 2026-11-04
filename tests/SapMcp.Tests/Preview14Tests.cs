using System.Text.Json;
using System.Text.Json.Nodes;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class OpenCodeTests
{
    [Theory][InlineData(OpenCodeVersion.V1)][InlineData(OpenCodeVersion.V2)]
    public async Task JsoncPreservesInstructionsCommentsAndOtherServersAndIsIdempotent(OpenCodeVersion version)
    {
        using var t=new TestDirectory();var file=Path.Combine(t.Root,"opencode.jsonc");var settings=AppSettings.CreateDefault(t.Root);
        var other="\"other\": {\"type\":\"local\",\"command\":[\"fixture\"]}";
        var mcp=version==OpenCodeVersion.V1?"{ "+other+", /* preserved inside */ }":"{\"servers\":{ "+other+", /* preserved inside */ }}";
        var before="{\r\n // Türkçe preserved instructions\r\n \"instructions\": [\"shared-agent-rules.md\"],\r\n \"model\":\"fixture/model\",\r\n \"mcp\":"+mcp+",\r\n}";
        await File.WriteAllTextAsync(file,before);
        var preview=ClientIntegration.PreviewOpenCode(file,settings,"D:\\SAP config\\settings.json",["adt","rfc"],version);
        Assert.Contains("// Türkçe preserved instructions",preview.After);Assert.Contains("/* preserved inside */",preview.After);Assert.Contains(other,preview.After);
        Assert.Contains("\"instructions\": [\"shared-agent-rules.md\"]",preview.After);Assert.Contains("\"model\":\"fixture/model\"",preview.After);
        Assert.Equal(before,await File.ReadAllTextAsync(file));
        var backup=await ClientIntegration.ApplyAsync(preview,false);Assert.Equal(before,await File.ReadAllTextAsync(backup!));
        var next=ClientIntegration.PreviewOpenCode(file,settings,"D:\\SAP config\\settings.json",["adt","rfc"],version);
        Assert.False(next.Changed);Assert.All(next.Entries,e=>Assert.Equal(ClientEntryState.Current,e.State));Assert.Null(await ClientIntegration.ApplyAsync(next,false));
        await ClientIntegration.RemoveOwnedAsync(new("OpenCode",file,preview.Names),settings.HostExecutable);
        var removed=await File.ReadAllTextAsync(file);Assert.DoesNotContain("sap_adt_mcp",removed);Assert.Contains(other,removed);Assert.Contains("preserved inside",removed);
        Assert.Equal(version,ClientIntegration.DetectOpenCodeVersion(removed));
    }
    [Theory][InlineData(OpenCodeVersion.V1)][InlineData(OpenCodeVersion.V2)]
    public void NewConfigurationRoundTripsBothDialects(OpenCodeVersion version)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);s.HostExecutable="C:\\Program Files\\SAP fixture\\SapMcpHost.exe";
        var example=ClientIntegration.OpenCodeConfiguration(s.HostExecutable,"D:\\fixture config\\settings.json",["adt","rfc"],version);
        var p=ClientIntegration.PreviewOpenCode(Path.Combine(t.Root,"new.jsonc"),s,"D:\\fixture config\\settings.json",["adt","rfc"],version);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(example),JsonNode.Parse(p.After)));Assert.Equal(version,ClientIntegration.DetectOpenCodeVersion(p.After));
        var root=JsonNode.Parse(p.After)!;var servers=version==OpenCodeVersion.V1?root["mcp"]!:root["mcp"]!["servers"]!;
        Assert.Equal(s.HostExecutable,(string?)servers["sap_adt_mcp"]!["command"]![0]);
        Assert.Equal(5,servers["sap_rfc_call"]!["command"]!.AsArray().Count);
        Assert.Equal(version==OpenCodeVersion.V1,servers["sap_adt_mcp"]!.AsObject().ContainsKey("enabled"));
    }
    [Theory][InlineData(OpenCodeVersion.V1)][InlineData(OpenCodeVersion.V2)]
    public async Task AliasReuseAndUninstallRespectOwnership(OpenCodeVersion version)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var file=Path.Combine(t.Root,"opencode.jsonc");var config=Path.Combine(t.Root,"settings.json");
        var json=ClientIntegration.OpenCodeConfiguration(s.HostExecutable,config,["adt"],version).Replace("sap_adt_mcp","existing_alias",StringComparison.Ordinal);
        await File.WriteAllTextAsync(file,json);
        var p=ClientIntegration.PreviewOpenCode(file,s,config,["adt"],version);Assert.Equal("existing_alias",Assert.Single(p.Names));Assert.False(p.Changed);
        var edited=json.Replace("SapMcpHost.exe","ForeignHost.exe",StringComparison.Ordinal);await File.WriteAllTextAsync(file,edited);
        await ClientIntegration.RemoveOwnedAsync(new("OpenCode",file,p.Names),s.HostExecutable);Assert.Equal(edited,await File.ReadAllTextAsync(file));
    }
    [Theory][InlineData(OpenCodeVersion.V1)][InlineData(OpenCodeVersion.V2)]
    public async Task ConflictsAndConcurrentEditsCannotOverwriteConfiguration(OpenCodeVersion version)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var file=Path.Combine(t.Root,"opencode.json");
        var other=ClientIntegration.OpenCodeConfiguration("other.exe","other.json",["adt"],version);await File.WriteAllTextAsync(file,other);
        var p=ClientIntegration.PreviewOpenCode(file,s,"settings.json",["adt"],version);Assert.Single(p.Conflicts);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>ClientIntegration.ApplyAsync(p,false));
        await File.AppendAllTextAsync(file,"\n// changed elsewhere");
        await Assert.ThrowsAsync<IOException>(()=>ClientIntegration.ApplyAsync(p,true));
    }
    [Theory]
    [InlineData("{\"mcp\":42}")]
    [InlineData("{\"mcp\":{\"servers\":{},\"legacy\":{\"type\":\"local\"}}}")]
    [InlineData("{\"mcp\":{},\"mcp\":{}}")]
    [InlineData("{\"mcp\":{\"servers\":false}}")]
    public void InvalidAndMixedFilesAreRejected(string json)=>Assert.ThrowsAny<Exception>(()=>ClientIntegration.DetectOpenCodeVersion(json));
    [Fact] public async Task DuplicateAliasesAndVersionMismatchAreRejected()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var file=Path.Combine(t.Root,"config.json");
        var root=JsonNode.Parse(ClientIntegration.OpenCodeConfiguration(s.HostExecutable,"settings.json",["adt"],OpenCodeVersion.V2))!;
        root["mcp"]!["servers"]!["alias"]=root["mcp"]!["servers"]!["sap_adt_mcp"]!.DeepClone();await File.WriteAllTextAsync(file,root.ToJsonString());
        Assert.Throws<InvalidDataException>(()=>ClientIntegration.PreviewOpenCode(file,s,"settings.json",["adt"],OpenCodeVersion.V2));
        Assert.Throws<InvalidDataException>(()=>ClientIntegration.PreviewOpenCode(file,s,"settings.json",["adt"],OpenCodeVersion.V1));
    }
    [Fact] public async Task OptionalPreferencesKeepLegacySchemaCompatible()
    {
        using var t=new TestDirectory();var store=new SettingsStore(Path.Combine(t.Root,"settings.json"));var s=AppSettings.CreateDefault(t.Root);
        await store.SaveAsync(s);Assert.Null((await store.LoadAsync()).OpenCode);
        s.OpenCode=new(){Enabled=true,Scope="project",Version=OpenCodeVersion.V1,ConfigPath=Path.Combine(t.Root,"opencode.jsonc")};
        await store.SaveAsync(s);var loaded=await store.LoadAsync();Assert.Equal(1,loaded.SchemaVersion);Assert.Equal(OpenCodeVersion.V1,loaded.OpenCode!.Version);
    }
    [Fact] public async Task UpdatingSelectedServerPreservesItsCommentsAndCanEnableIt()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var file=Path.Combine(t.Root,"opencode.jsonc");
        var text=ClientIntegration.OpenCodeConfiguration(s.HostExecutable,"settings.json",["adt"],OpenCodeVersion.V2).Replace("\"disabled\": false","/* keep flag explanation */ \"disabled\": true",StringComparison.Ordinal);
        await File.WriteAllTextAsync(file,text);
        var p=ClientIntegration.PreviewOpenCode(file,s,"settings.json",["adt"],OpenCodeVersion.V2);
        Assert.Contains("/* keep flag explanation */",p.After);Assert.Single(p.Conflicts);
        await ClientIntegration.ApplyAsync(p,true);Assert.False(ClientIntegration.PreviewOpenCode(file,s,"settings.json",["adt"],OpenCodeVersion.V2).Changed);
    }
    [Fact] public async Task ReplacingCommandKeepsCommentsInsideItsArray()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var file=Path.Combine(t.Root,"opencode.jsonc");
        var text=ClientIntegration.OpenCodeConfiguration("foreign.exe","settings.json",["adt"],OpenCodeVersion.V2).Replace("\"command\": [","\"command\": [ // keep command explanation\n",StringComparison.Ordinal);
        await File.WriteAllTextAsync(file,text);var p=ClientIntegration.PreviewOpenCode(file,s,"settings.json",["adt"],OpenCodeVersion.V2);
        Assert.Contains("// keep command explanation",p.After);Assert.Equal(OpenCodeVersion.V2,ClientIntegration.DetectOpenCodeVersion(p.After));
    }
}

public sealed class AdvancedEvaluationTests
{
    [Theory][InlineData(true,true)][InlineData(true,false)][InlineData(false,true)][InlineData(false,false)]
    public async Task EvaluationDoesNotCommitProfilesMappingsSecretsOrExternalSource(bool adt,bool rfc)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);s.ConnectionSourceMode="sapGui";s.LandscapeFile=Path.Combine(t.Root,"external.xml");
        await File.WriteAllTextAsync(s.LandscapeFile,"<Landscape><Services><Service type='SAPGUI' uuid='fixture' name='Fixture' systemid='TST' server='fixture.invalid:3202'/></Services></Landscape>");
        var secrets=new DpapiSecretStore();var secret=await secrets.ProtectAsync("existing secret",s.SecretsDirectory);
        var p=new ConnectionProfile{Name="Original",Sid="TST",TargetKey="fixture.invalid:02",User="fixture",SecretRef=secret,AdtBaseUrl="https://fixture.invalid:44302",AdtCertificates=[new(Path.Combine(t.Root,"certificate.pem"),new string('a',64))]};s.Profiles.Add(p);s.ActiveProfiles[p.TargetKey]=p.Id;
        await ProfileService.MaterializeAsync(s);var store=new SettingsStore(Path.Combine(t.Root,"config","settings.json"));await store.SaveAsync(s);
        var before=JsonSerializer.Serialize(s,JsonDefaults.Options);var files=Directory.GetFiles(t.Root,"*",SearchOption.AllDirectories).ToDictionary(f=>f,File.ReadAllBytes);
        var draft=new BasicDraft{ProfileId=p.Id,Sid="TST",Host="fixture.invalid",Sysnr="02",User="edited",Password="new secret"};
        var service=new BasicConnectionService(secrets){Hooks=new(){Check=(_,_,c,_)=>Task.FromResult((c=="adt"?adt:rfc)?BasicCheck.Passed:BasicCheck.Failed("FIXTURE_FAILED")),Servers=(_,_,_,_)=>Task.FromResult(new BasicRfcServers("OK",[new("fixture.invalid","02")])),Discover=(_,_,_,_,_,_)=>Task.FromResult(new BasicDiscovery(2,[new("http://fixture.invalid:8080",0,"AUTHENTICATION_REQUIRED",401,false)],false,true,1))}};
        var result=await service.EvaluateAsync(s,draft,_=>Task.FromResult(new RfcReadinessResult("OK","",[],[])),(_,_)=>Task.FromResult(true));
        Assert.Null(result.Settings);Assert.Equal(adt,result.Adt.Success);Assert.Equal(rfc,result.Rfc.Success);Assert.Equal(before,JsonSerializer.Serialize(s,JsonDefaults.Options));
        foreach(var f in files)Assert.Equal(f.Value,await File.ReadAllBytesAsync(f.Key));
        Assert.True(files.Keys.Order().SequenceEqual(Directory.GetFiles(t.Root,"*",SearchOption.AllDirectories).Order()),string.Join(";",Directory.GetFiles(t.Root,"*",SearchOption.AllDirectories).Except(files.Keys).Select(f=>Path.GetRelativePath(t.Root,f))));
        Assert.Equal("existing secret",await secrets.ReadAsync(secret));
        Assert.Equal(adt?"http://fixture.invalid:8080":"",result.Url);
    }
    [Fact] public async Task RejectedConsentDoesNotReadSecretsOrRunChecks()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var calls=0;
        var svc=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Check=(_,_,_,_)=>{calls++;throw new Exception();}}};
        await Assert.ThrowsAsync<OperationCanceledException>(()=>svc.EvaluateAsync(s,new(){Sid="TST",Host="fixture.invalid",Sysnr="02",User="fixture"},_=>throw new Exception(),(_,_)=>Task.FromResult(false)));
        Assert.Equal(0,calls);Assert.Empty(Directory.GetFiles(t.Root,"*",SearchOption.AllDirectories));
    }
    [Fact] public async Task CancellationDuringDiscoveryCleansTemporarySecrets()
    {
        using var t=new TestDirectory();using var cancelled=new CancellationTokenSource();var s=AppSettings.CreateDefault(t.Root);
        var svc=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Discover=(_,_,_,_,_,_)=>{cancelled.Cancel();return Task.FromResult(new BasicDiscovery(2,[],false,false,1));}}};
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>svc.EvaluateAsync(s,new(){Sid="TST",Host="fixture.invalid",Sysnr="02",User="fixture",Password="fixture secret"},_=>Task.FromResult(new RfcReadinessResult("RFC_DISABLED_BY_MODE","",[],[])),(_,_)=>Task.FromResult(true),ct:cancelled.Token));
        Assert.Empty(s.Profiles);Assert.Empty(s.ActiveProfiles);Assert.Empty(Directory.GetFiles(t.Root,"*",SearchOption.AllDirectories));
    }
}
