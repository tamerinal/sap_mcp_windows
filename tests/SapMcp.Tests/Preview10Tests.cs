using System.Text.Json;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class Preview10Tests
{
    private static BasicDraft Draft()=>new(){Sid="TST",Host="fixture.invalid",Sysnr="02",User="PRIVATE_USER",Password="  PRIVATE_PASSWORD  "};
    private static BasicDiscovery Found(string host="fixture.invalid")=>new(2,[new($"https://{host}:44300",0,"AUTHENTICATION_REQUIRED",401,false)],false,true,1);
    private static BasicDiscovery Empty()=>new(2,[],false,false,1);
    private static async Task<(SettingsStore Store,AppSettings Settings)> Source(TestDirectory t){var store=new SettingsStore(Path.Combine(t.Root,"config","settings.json"));return(store,await new ConnectionManager(store).SelectSourceAsync(AppSettings.CreateDefault(t.Root),"managed"));}

    [Fact] public async Task SaveOnlyNeverUsesNetworkHooksAndInvalidatesValidation()
    {
        using var t=new TestDirectory();var(store,source)=await Source(t);var calls=0;
        var service=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Check=(_,_,_,_)=>{calls++;throw new Exception();},Discover=(_,_,_,_,_,_)=>{calls++;throw new Exception();}}};
        var draft=Draft();var saved=await service.SaveDraftAsync(source,store,draft);var p=Assert.Single(saved.Profiles);
        Assert.Equal(0,calls);Assert.Null(p.BasicValidation);Assert.True(p.AllowInsecureTls);Assert.Equal("TST",p.Sid);Assert.Empty(BasicPolicy.Eligible(saved,p));
        Assert.Equal(draft.Password,await new DpapiSecretStore().ReadAsync(ProfileService.ResolveSecret(saved.CredentialsFile,p.SecretRef)));
        draft.Password="";saved=await service.SaveDraftAsync(saved,store,draft);Assert.Equal(p.SecretRef,Assert.Single(saved.Profiles).SecretRef);
    }
    [Theory][InlineData(false,"RFC_UNAVAILABLE")][InlineData(true,"AUTHORIZATION_DENIED")][InlineData(true,"RFC_ALLOWLIST_DENIED")][InlineData(true,"RFC_DISCOVERY_FAILED")]
    public async Task MissingRfcOrPermissionUsesScanConsentWithoutRfcQuestion(bool rfc,string permission)
    {
        using var t=new TestDirectory();var(store,source)=await Source(t);var events=new List<string>();
        var service=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Check=(_,_,component,_)=>{events.Add(component);return Task.FromResult(BasicCheck.Passed);},Servers=(_,_,_,_)=>Task.FromResult(new BasicRfcServers(permission,[])),Discover=(_,_,mode,_,_,_)=>{events.Add(mode);return Task.FromResult(Found());}}};
        var result=await service.ValidateAsync(source,store,Draft(),rfc,(p,_)=>{events.Add(p.ToString());return Task.FromResult(true);});
        Assert.DoesNotContain("RfcDiscovery",events);Assert.True(events.IndexOf("AutomaticScan")<events.IndexOf("smart"));Assert.True(events.IndexOf("smart")<events.IndexOf("adt"));
        Assert.True(result.Adt.Success);Assert.Equal(rfc,result.Rfc.Success);Assert.NotNull(result.Settings);
    }
    [Theory][InlineData(true,true)][InlineData(true,false)][InlineData(false,false)]
    public async Task RfcHostsAreUsedOnlyAfterConsentAndFailureFallsBack(bool acceptRfc,bool foundRfc)
    {
        using var t=new TestDirectory();var(store,source)=await Source(t);var events=new List<string>();
        var servers=new List<BasicRfcServer>{new("other-instance.invalid","04")};
        var service=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Check=(_,_,c,_)=>{events.Add(c);return Task.FromResult(BasicCheck.Passed);},Servers=(_,_,_,_)=>Task.FromResult(new BasicRfcServers("OK",servers)),Discover=(_,_,mode,hosts,_,_)=>{events.Add(mode);Assert.Equal(servers,hosts);return Task.FromResult(mode=="rfc"?(foundRfc?Found("other-instance.invalid"):Empty()):Found());}}};
        var result=await service.ValidateAsync(source,store,Draft(),true,(p,_)=>{events.Add(p.ToString());return Task.FromResult(p!=BasicPrompt.RfcDiscovery||acceptRfc);});
        Assert.True(result.Adt.Success);Assert.Contains("RfcDiscovery",events);
        if(acceptRfc&&foundRfc){Assert.DoesNotContain("smart",events);Assert.Equal("https://other-instance.invalid:44300",result.Url);}
        else{Assert.Contains("AutomaticScan",events);Assert.Contains("smart",events);}
        Assert.True(events.IndexOf("rfc")<events.IndexOf("RfcDiscovery"));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task ExtendedScanRequiresSeparateConsent(bool allow)
    {
        using var t=new TestDirectory();var(store,source)=await Source(t);var modes=new List<string>();
        var service=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Check=(_,_,_,_)=>Task.FromResult(BasicCheck.Passed),Discover=(_,_,mode,_,_,_)=>{modes.Add(mode);return Task.FromResult(mode=="full"?Found():Empty());}}};
        var result=await service.ValidateAsync(source,store,Draft(),false,(p,_)=>Task.FromResult(p!=BasicPrompt.ExtendedScan||allow));
        Assert.Equal(allow,result.Adt.Success);Assert.Equal(allow,modes.Contains("full"));Assert.Equal(allow,result.Settings!=null);
    }
    [Theory][InlineData(BasicPrompt.ConnectionWarning)][InlineData(BasicPrompt.AutomaticScan)]
    public async Task RefusingConsentCannotStartScan(BasicPrompt refusal)
    {
        using var t=new TestDirectory();var(store,source)=await Source(t);var calls=0;
        var service=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Discover=(_,_,_,_,_,_)=>{calls++;throw new Exception("must not run");}}};
        async Task Run(){var r=await service.ValidateAsync(source,store,Draft(),false,(p,_)=>Task.FromResult(p!=refusal));Assert.Null(r.Settings);}
        if(refusal==BasicPrompt.ConnectionWarning)await Assert.ThrowsAnyAsync<OperationCanceledException>(Run);else await Run();
        Assert.Equal(0,calls);Assert.Empty((await store.LoadAsync()).Profiles);
    }
    [Fact] public async Task CredentialFailureTriesNextCandidateAndStopsAfterFirstSuccess()
    {
        using var t=new TestDirectory();var(store,source)=await Source(t);var calls=0;
        var candidates=new BasicDiscovery(2,[new("https://fixture.invalid:44300",0,"AUTHENTICATION_REQUIRED",401,false),new("https://fixture.invalid:44301",1,"AUTHENTICATION_REQUIRED",401,false),new("https://fixture.invalid:44302",2,"AUTHENTICATION_REQUIRED",401,false)],false,true,0);
        var result=await new BasicConnectionService(new DpapiSecretStore()).SaveAsync(source,store,Draft(),candidates,false,tester:(_,_,_,_)=>Task.FromResult(++calls==1?BasicCheck.Failed("AUTHENTICATION_FAILED"):BasicCheck.Passed));
        Assert.Equal(2,calls);Assert.True(result.Adt.Success);Assert.Equal("https://fixture.invalid:44301",Assert.Single(result.Settings!.Profiles).AdtBaseUrl);
    }
    [Fact] public async Task CancellationDiscardsDiscoveryAndCleansTemporarySecrets()
    {
        using var t=new TestDirectory();var(store,source)=await Source(t);using var stop=new CancellationTokenSource();
        var service=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Discover=(_,_,_,_,_,_)=>{stop.Cancel();return Task.FromResult(Found());}}};
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>service.ValidateAsync(source,store,Draft(),false,(_,_)=>Task.FromResult(true),ct:stop.Token));
        Assert.Empty((await store.LoadAsync()).Profiles);Assert.Empty(Directory.GetDirectories(Path.Combine(t.Root,"cache","basic-tests")));
    }
    [Fact] public async Task ManualProxyIsCheckedWithoutPortScanAndRfcSuccessIsIndependent()
    {
        using var t=new TestDirectory();var(store,source)=await Source(t);var draft=Draft();draft.ManualUrl="http://dispatcher.invalid:8080/proxy/sap/bc/adt";
        var service=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Check=(_,_,c,_)=>Task.FromResult(c=="rfc"?BasicCheck.Passed:BasicCheck.Failed("AUTHENTICATION_FAILED")),Servers=(_,_,_,_)=>Task.FromResult(new BasicRfcServers("AUTHORIZATION_DENIED",[])),Discover=(_,_,mode,_,_,_)=>{Assert.Equal("manual",mode);return Task.FromResult(Found() with{Candidates=[new("http://dispatcher.invalid:8080/proxy",0,"AUTHENTICATION_REQUIRED",401,false)]});}}};
        var prompts=new List<BasicPrompt>();var r=await service.ValidateAsync(source,store,draft,true,(p,_)=>{prompts.Add(p);return Task.FromResult(true);});
        Assert.True(r.Rfc.Success);Assert.False(r.Adt.Success);Assert.Equal(new[]{BasicPrompt.ConnectionWarning},prompts);Assert.Equal(new[]{"rfc"},BasicPolicy.Eligible(r.Settings!,Assert.Single(r.Settings!.Profiles)));
    }
    [Fact] public void BasicAcceptsWarningsOnlyWithAllRequiredStagesPassed()
    {
        using var valid=JsonDocument.Parse("""{"success":false,"outcome":"verified_with_warnings","stages":[{"stage":"authentication","status":"passed"},{"stage":"discovery","status":"passed"},{"stage":"readOnly","status":"passed"}]}""");Assert.True(BasicPolicy.AdtResult(valid.RootElement).Success);
        foreach(var body in new[]{"{\"success\":true}",valid.RootElement.GetRawText().Replace("\"readOnly\"","\"somethingElse\""),valid.RootElement.GetRawText().Replace("\"passed\"","\"unverified\"")}){using var json=JsonDocument.Parse(body);Assert.False(BasicPolicy.AdtResult(json.RootElement).Success);}
    }
    [Fact] public void ParsesAllRfcServersAndSeparatesLocalPolicyFromSapAuthority()
    {
        using var json=JsonDocument.Parse("""{"structuredContent":{"result":{"LIST":[{"NAME":"app_TST_02","HOSTADDR_V4_STR":"192.0.2.2"},{"NAME":"other_TST_04","HOST":"other.invalid"}]}}}""");
        Assert.Equal(new[]{new BasicRfcServer("192.0.2.2","02"),new BasicRfcServer("other.invalid","04")},BasicConnectionService.ParseServers(json.RootElement).Servers);
        foreach(var(code,message) in new[]{("AUTHORIZATION_DENIED","RFC_NO_AUTHORITY S_RFC"),("RFC_ALLOWLIST_DENIED","Function allowlist disi")}){using var denied=JsonDocument.Parse(JsonSerializer.Serialize(new{isError=true,content=new[]{new{text=message}}}));Assert.Equal(code,BasicConnectionService.ParseServers(denied.RootElement).Code);}
    }
    [Fact] public void CrossHostPreferredAddressCannotEnterAutomaticDiscovery()
    {
        using var json=JsonDocument.Parse(BasicDiscoveryService.SerializeRequest(new(){Host="fixture.invalid",Sysnr="00",PreferredUrl="https://dispatcher.invalid:44300"}));Assert.False(json.RootElement.TryGetProperty("preferredUrl",out _));
    }
    [Fact] public void VersionTwoDiscoverySupportsMoreThan128ResultsAndRejectsLegacyProtocol()
    {
        var candidates=Enumerable.Range(1,129).Select(p=>new BasicCandidate("https://fixture.invalid:"+p,p,"AUTHENTICATION_REQUIRED",401,false)).ToList();
        var result=new BasicDiscovery(2,candidates,false,true,1);Assert.Equal(129,BasicDiscoveryService.ReadResponse(new(0,JsonSerializer.Serialize(result,JsonDefaults.Options),"")).Candidates.Count);
        Assert.Throws<BasicDiscoveryException>(()=>BasicDiscoveryService.ReadResponse(new(0,JsonSerializer.Serialize(result with{SchemaVersion=1},JsonDefaults.Options),"")));
    }
    [Fact] public void ErrorPriorityDoesNotDependOnPortOrderAndLegacyValidationIsIneligible()
    {
        var candidates=new List<BasicCandidate>{new("https://fixture.invalid",0,"NETWORK_TIMEOUT",null,false),new("https://fixture.invalid:44300",1,"TLS_UNTRUSTED",null,false)};
        Assert.Equal("TLS_UNTRUSTED",BasicPolicy.BestFailure(new(2,candidates,false,false,0)));candidates.Reverse();Assert.Equal("TLS_UNTRUSTED",BasicPolicy.BestFailure(new(2,candidates,false,false,0)));
        var s=AppSettings.CreateDefault(Path.GetTempPath());var p=new ConnectionProfile{TargetKey="fixture.invalid:02"};s.ActiveProfiles[p.TargetKey]=p.Id;p.BasicValidation=new(BasicCheck.Passed,BasicCheck.Passed,DateTimeOffset.UtcNow,BasicPolicy.Fingerprint(s,p));Assert.Empty(BasicPolicy.Eligible(s,p));
    }
}
