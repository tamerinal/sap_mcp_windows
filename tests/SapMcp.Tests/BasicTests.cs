using System.Text.Json;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class BasicTests
{
    [Theory][InlineData("fictional.example.invalid",null)][InlineData("127.0.0.1",null)][InlineData("[::1]",null)][InlineData("fictional.example.invalid:44304",44304)]
    public void DiscoveryRequestOmitsAbsentOptionalsAndAllCredentials(string host,int? port)
    {
        var request=BasicDiscoveryService.SerializeRequest(new(){Host=host,User="PRIVATE_USER",Password="PRIVATE_PASSWORD",Client="123"});using var json=JsonDocument.Parse(request);
        Assert.False(json.RootElement.TryGetProperty("manualUrl",out _));
        if(port==null)Assert.False(json.RootElement.TryGetProperty("explicitPort",out _));else Assert.Equal(port,json.RootElement.GetProperty("explicitPort").GetInt32());
        Assert.DoesNotContain("PRIVATE",request);Assert.False(json.RootElement.TryGetProperty("user",out _));Assert.False(json.RootElement.TryGetProperty("client",out _));
    }
    [Theory][InlineData("DISCOVERY_PORT_INVALID")][InlineData("DISCOVERY_INPUT_INVALID")]
    public void DiscoveryConfigurationFailureIsNotDisguisedAsNetworkOrCertificateError(string code)
    {
        var ex=Assert.Throws<BasicDiscoveryException>(()=>BasicDiscoveryService.ReadResponse(new(2,"{\"schemaVersion\":1,\"code\":\""+code+"\",\"candidates\":[]}","secret stderr")));Assert.Equal(code,ex.Code);Assert.DoesNotContain("secret",ex.Message);
    }
    [Fact] public void UnknownOrBrokenDiscoveryOutputCannotLeakProcessContent()
    {
        foreach(var output in new[]{"not-json secret","{\"code\":\"SENSITIVE_ENDPOINT\"}","{\"schemaVersion\":1,\"candidates\":null}"})
        {var ex=Assert.Throws<BasicDiscoveryException>(()=>BasicDiscoveryService.ReadResponse(new(2,output,"secret stderr")));Assert.DoesNotContain("SENSITIVE",ex.Message);Assert.DoesNotContain("secret",ex.Message);}
    }
    private static SettingsStore Store(TestDirectory t)=>new(Path.Combine(t.Root,"config","settings.json"));
    [Fact] public void UnverifiedSdkCandidateCannotReplaceExistingSharedSdk()
    {
        using var t=new TestDirectory();var current=AppSettings.CreateDefault(t.Root);current.SdkDirectory=Path.Combine(t.Root,"validated-sdk");var candidate=BasicPolicy.Copy(current);candidate.SdkDirectory=Path.Combine(t.Root,"invalid-sdk");
        BasicPreparationService.RetainValidatedSdk(candidate,current,false);Assert.Equal(current.SdkDirectory,candidate.SdkDirectory);
        candidate.SdkDirectory=Path.Combine(t.Root,"new-validated-sdk");BasicPreparationService.RetainValidatedSdk(candidate,current,true);Assert.NotEqual(current.SdkDirectory,candidate.SdkDirectory);
    }
    [Fact] public async Task FreshConfigurationUsesBasicLegacyUsesAdvancedAndModePersists()
    {
        using var t=new TestDirectory();var store=Store(t);Assert.Equal("basic",(await store.LoadAsync()).UiMode);
        await store.SaveAsync(AppSettings.CreateDefault(t.Root));Assert.Equal("advanced",(await store.LoadAsync()).UiMode);
        var s=await store.LoadAsync();s.UiMode="basic";await store.SaveAsync(s);Assert.Equal("basic",(await store.LoadAsync()).UiMode);
    }
    [Theory]
    [InlineData("sap.example.invalid:8443","sap.example.invalid",8443)]
    [InlineData("[::1]:44301","::1",44301)]
    [InlineData("sap.example.invalid","sap.example.invalid",null)]
    public void HostIsIndependentOfHttpsPort(string input,string host,int? port)=>Assert.Equal((host,port),BasicPolicy.ParseHost(input));
    [Theory][InlineData("user:secret@host")][InlineData("host/path")][InlineData("host?password=secret")][InlineData("host#fragment")]
    public void HostCannotCarryUrlsCredentialsOrPaths(string host)=>Assert.Throws<InvalidDataException>(()=>BasicPolicy.ParseHost(host));
    [Fact] public void ManualPortAndInstanceAreNotGuessedFromOtherHosts()
    {
        Assert.Equal("https://[::1]:8443",BasicPolicy.ManualUrl("8443","[::1]"));
        Assert.Throws<InvalidDataException>(()=>BasicPolicy.ManualUrl("http://host","host"));
        Assert.Equal(4,BasicPolicy.InstanceCandidate("https://host:44304","host"));Assert.Null(BasicPolicy.InstanceCandidate("https://dispatcher:44304","host"));Assert.Null(BasicPolicy.InstanceCandidate("https://host:8443","host"));
    }
    [Theory]
    [InlineData(true,true,"passed","OK",true)]
    [InlineData(true,true,"failed","RFC_PING_FAILED",true)]
    [InlineData(false,true,"passed","OK",true)]
    [InlineData(false,true,"failed","RFC_PING_FAILED",false)]
    [InlineData(true,false,"blocked","RFC_DEPENDENCY_MISSING",true)]
    public async Task AdtAndRfcSaveOnlyIndependentSuccesses(bool adtSuccess,bool rfcAvailable,string rfcState,string rfcCode,bool saved)
    {
        using var t=new TestDirectory();var store=Store(t);var source=await new ConnectionManager(store).SelectSourceAsync(AppSettings.CreateDefault(t.Root),"managed");
        var old=await File.ReadAllBytesAsync(source.CredentialsFile);var draft=new BasicDraft{Host="test.example.invalid",Sysnr="04",User="DEMO",Password="  şifre  "};
        var discovery=new BasicDiscovery(1,[new("https://test.example.invalid:44304",0,"AUTHENTICATION_REQUIRED",401,false)],false,false,1);var calls=new List<string>();
        var result=await new BasicConnectionService(new DpapiSecretStore()).SaveAsync(source,store,draft,discovery,rfcAvailable,tester:(_,_,component,_)=>{calls.Add(component);return Task.FromResult(component=="adt"?(adtSuccess?BasicCheck.Passed:BasicCheck.Failed("ADT_DISCOVERY_INVALID")):new BasicCheck(rfcState,rfcCode));});
        Assert.Equal(saved,result.Settings!=null);Assert.Equal(adtSuccess,result.Adt.Success);Assert.Equal(rfcAvailable&&rfcState=="passed",result.Rfc.Success);
        Assert.Equal(rfcAvailable?new[]{"adt","rfc"}:new[]{"adt"},calls);
        Assert.Empty(source.Profiles);Assert.Empty(source.ActiveProfiles);Assert.Empty(Directory.GetDirectories(Path.Combine(t.Root,"cache","basic-tests")));
        if(saved){var s=await store.LoadAsync();var p=Assert.Single(s.Profiles);Assert.Equal("  şifre  ",await new DpapiSecretStore().ReadAsync(ProfileService.ResolveSecret(s.CredentialsFile,p.SecretRef)));Assert.Equal((adtSuccess?1:0)+(rfcAvailable&&rfcState=="passed"?1:0),BasicPolicy.Eligible(s,p).Length);p.User="CHANGED";Assert.Empty(BasicPolicy.Eligible(s,p));}
        else{Assert.Equal(old,await File.ReadAllBytesAsync(source.CredentialsFile));Assert.Empty((await store.LoadAsync()).Profiles);Assert.False(Directory.Exists(source.SecretsDirectory));}
    }
    [Fact] public async Task AuthenticationFailureStopsOtherPortsAndRfc()
    {
        using var t=new TestDirectory();var store=Store(t);var source=await new ConnectionManager(store).SelectSourceAsync(AppSettings.CreateDefault(t.Root),"managed");var calls=0;
        var r=await new BasicConnectionService(new DpapiSecretStore()).SaveAsync(source,store,new(){Host="example.invalid",Sysnr="00",User="DEMO",Password="test"},new(1,[new("https://example.invalid",0,"AUTHENTICATION_REQUIRED",401,false),new("https://example.invalid:44300",1,"AUTHENTICATION_REQUIRED",401,false)],false,false,0),true,tester:(_,_,_,_)=>{calls++;return Task.FromResult(BasicCheck.Failed("AUTHENTICATION_FAILED"));});
        Assert.Equal(1,calls);Assert.Null(r.Settings);Assert.Equal("AUTHENTICATION_RETRY_STOPPED",r.Rfc.Code);
    }
    [Fact] public async Task UnsafeDiscoveryTargetCannotReceiveCredentials()
    {
        using var t=new TestDirectory();var store=Store(t);var source=await new ConnectionManager(store).SelectSourceAsync(AppSettings.CreateDefault(t.Root),"managed");var called=false;
        await Assert.ThrowsAsync<InvalidDataException>(()=>new BasicConnectionService(new DpapiSecretStore()).SaveAsync(source,store,new(){Host="expected.invalid",Sysnr="00",User="DEMO",Password="test"},new(1,[new("https://unexpected.invalid",0,"AUTHENTICATION_REQUIRED",401,false)],false,false,0),true,tester:(_,_,_,_)=>{called=true;return Task.FromResult(BasicCheck.Passed);}));Assert.False(called);Assert.Empty((await store.LoadAsync()).Profiles);
    }
    [Fact] public async Task GuiConversionPreservesXmlProfilesAndRejectsUnsupportedEntries()
    {
        using var t=new TestDirectory();var store=Store(t);var s=AppSettings.CreateDefault(t.Root);s.ConnectionSourceMode="sapGui";s.LandscapeFile=Path.Combine(t.Root,"gui.xml");
        var xml="<Landscape><Services><Service uuid='one' type='SAPGUI' name='Fictional' server='sap.example.invalid:3204'/></Services></Landscape>";await File.WriteAllTextAsync(s.LandscapeFile,xml);await store.SaveAsync(s);
        var next=await new ConnectionManager(store).ConvertForBasicAsync(s);Assert.Single(await ConnectionManager.Source(next).LoadAsync(next));Assert.Equal(xml,await File.ReadAllTextAsync(s.LandscapeFile));
        await File.WriteAllTextAsync(s.LandscapeFile,xml.Replace("3204","3600"));await Assert.ThrowsAsync<InvalidDataException>(()=>new ConnectionManager(store).ConvertForBasicAsync(s));
    }
    private sealed class BrokenManager(SettingsStore store):ConnectionManager(store)
    {private int n;protected override Task WriteAsync(string p,string c,CancellationToken ct)=>++n==3?throw new IOException("injected"):base.WriteAsync(p,c,ct);}
    [Fact] public async Task GuiConversionCannotSilentlyReplaceAnExistingRouteForTheSameTarget()
    {
        using var t=new TestDirectory();var store=Store(t);var manager=new ConnectionManager(store);var s=await manager.SelectSourceAsync(AppSettings.CreateDefault(t.Root),"managed");
        s=await manager.SaveSystemAsync(s,new(){Name="Existing",Host="sap.example.invalid",Sysnr="00",Router="/H/router-a.invalid/S/3299"});
        var catalog=await File.ReadAllBytesAsync(ConnectionManager.CatalogPath(s));var gui=Path.Combine(t.Root,"gui.xml");
        await File.WriteAllTextAsync(gui,"<Landscape><Routers><Router uuid='r' router='/H/router-b.invalid/S/3299'/></Routers><Services><Service type='SAPGUI' uuid='other' name='GUI' server='sap.example.invalid:3200' routerid='r'/></Services></Landscape>");
        s=await manager.SelectSourceAsync(s,"sapGui",gui);var before=await File.ReadAllBytesAsync(store.FilePath);
        await Assert.ThrowsAsync<InvalidDataException>(()=>manager.ConvertForBasicAsync(s));Assert.Equal(before,await File.ReadAllBytesAsync(store.FilePath));Assert.Equal(catalog,await File.ReadAllBytesAsync(ConnectionManager.CatalogPath(s)));
    }
    [Fact] public async Task BasicCommitRollsBackAllSharedFilesOnFailure()
    {
        using var t=new TestDirectory();var store=Store(t);var s=await new ConnectionManager(store).SelectSourceAsync(AppSettings.CreateDefault(t.Root),"managed");var before=await File.ReadAllBytesAsync(store.FilePath);
        var p=new ConnectionProfile{Name="new",TargetKey="new.invalid:00",User="DEMO",SecretRef="reference.dpapi",BasicValidation=new(BasicCheck.Passed,BasicCheck.Passed,DateTimeOffset.UtcNow,"fingerprint")};
        await Assert.ThrowsAsync<IOException>(()=>new BrokenManager(store).SaveBasicAsync(s,new(){Name="new",Host="new.invalid",Sysnr="00"},p));Assert.Equal(before,await File.ReadAllBytesAsync(store.FilePath));Assert.Empty((await new ManagedJsonConnectionSource().ReadCatalogAsync(s)).Systems);Assert.Empty((await store.LoadAsync()).Profiles);
    }
    [Fact] public void HtmlTextOrEmptySuccessfulMcpEnvelopeCannotBecomeRfcSuccess()
    {
        using var empty=JsonDocument.Parse("{\"content\":[{\"text\":\"success\"}]}");Assert.False(BasicPolicy.RfcResult(empty.RootElement).Success);
        using var unverified=JsonDocument.Parse("{\"success\":false,\"stages\":[{\"status\":\"unverified\",\"code\":\"AUTHENTICATION_NOT_PROVEN\"}]}");Assert.False(BasicPolicy.AdtResult(unverified.RootElement).Success);
    }
}
