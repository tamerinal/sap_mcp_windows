using System.Text.Json;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class RfcReadinessTests
{
    private static List<Requirement> Ready()=>RfcReadinessService.RequiredIds.Select(id=>new Requirement(id,id,"",CheckState.Ready,"")).ToList();
    [Theory][InlineData("sdk")][InlineData("python")][InlineData("pyrfc")][InlineData("vc-runtime")][InlineData("vc2013")][InlineData("rfc-native")][InlineData("rfc-package")]
    public void MissingCheckCannotBecomeReady(string id)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var checks=Ready();checks.RemoveAll(c=>c.Id==id);
        var result=RfcReadinessService.Evaluate(s,true,checks);Assert.False(result.Ready);Assert.Contains(result.Blockers,c=>c.Id==id);
    }
    [Theory][InlineData("RFC_NATIVE_IMPORT_FAILED")][InlineData("RFC_NATIVE_IMPORT_TIMEOUT")]
    public void NativeFailureRemainsDistinctFromMissingSdk(string code)
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var checks=Ready();checks.RemoveAll(c=>c.Id=="rfc-native");checks.Add(new("rfc-native","native","",CheckState.Blocked,code));
        Assert.Equal(code,RfcReadinessService.Evaluate(s,true,checks).Code);
        Assert.Equal("RFC_DISABLED_BY_MODE",RfcReadinessService.Evaluate(s,false,checks).Code);
    }
    [Fact] public async Task NativeImportUsesConfiguredSdkWithoutPreloadingOrConnecting()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);s.SdkDirectory=Path.Combine(t.Root,"SDK Türkçe boşluk");string? work=null;
        var result=await RfcNativeValidation.CheckAsync(s,(exe,env,dir,_)=>{
            Assert.Equal(s.PythonExecutable,exe);Assert.Equal(s.SdkDirectory,env["SAPNWRFC_HOME"]);Assert.StartsWith(Path.Combine(s.SdkDirectory,"lib")+";",env["PATH"]);Assert.Empty(Directory.EnumerateFileSystemEntries(dir));work=dir;
            return Task.FromResult(new ProcessResult(0,"{\"schemaVersion\":1,\"ok\":true}",""));
        });
        Assert.Equal(CheckState.Ready,result.State);Assert.False(Directory.Exists(work));Assert.DoesNotContain("ctypes",RfcNativeValidation.Script);Assert.DoesNotContain("Connection(",RfcNativeValidation.Script);
    }
    [Fact] public async Task ImportErrorNeverLeaksWorkerOutputAndCancellationPropagates()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);
        var result=await RfcNativeValidation.CheckAsync(s,(_,_,_,_)=>Task.FromResult(new ProcessResult(1,"secret invalid stdout","private stderr")));
        Assert.Equal("RFC_NATIVE_IMPORT_FAILED",result.Detail);Assert.True(result.Diagnostic!.IsSafe);
        var timeout=await RfcNativeValidation.CheckAsync(s,(_,_,_,_)=>throw new OperationCanceledException());Assert.Equal("RFC_NATIVE_IMPORT_TIMEOUT",timeout.Detail);
        using var cancel=new CancellationTokenSource();cancel.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>RfcNativeValidation.CheckAsync(s,(_,_,_,ct)=>{ct.ThrowIfCancellationRequested();throw new Exception();},cancel.Token));
    }
    [Fact] public void ClientEligibilityNeedsSuccessfulPingAndCurrentReadiness()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var p=new ConnectionProfile{TargetKey="fixture.invalid:04"};s.ActiveProfiles[p.TargetKey]=p.Id;
        p.BasicValidation=new(BasicCheck.Passed,BasicCheck.Passed,DateTimeOffset.UtcNow,BasicPolicy.Fingerprint(s,p),2);
        Assert.Equal(new[]{"adt"},RfcReadinessService.Eligible(s,p,null,true));var ready=RfcReadinessService.Evaluate(s,true,Ready());Assert.Contains("rfc",RfcReadinessService.Eligible(s,p,ready,true));
        Directory.CreateDirectory(Path.Combine(s.PackageDirectory,"rfc","python"));File.WriteAllText(Path.Combine(s.PackageDirectory,"rfc","python","worker.py"),"fixture");
        Assert.DoesNotContain("rfc",RfcReadinessService.Eligible(s,p,ready,true));ready=RfcReadinessService.Evaluate(s,true,Ready());Assert.Contains("rfc",RfcReadinessService.Eligible(s,p,ready,true));
        p.BasicValidation=p.BasicValidation with{Rfc=BasicCheck.Failed("AUTHENTICATION_FAILED")};Assert.DoesNotContain("rfc",RfcReadinessService.Eligible(s,p,ready,true));
    }
    [Fact] public void MissingOptionalRfcPackageDoesNotBlockAdtPreparation()
    {
        Assert.True(BasicInstallation.SharedReady([new("node","Node","24",CheckState.Ready,""),new("rfc-package","RFC","",CheckState.Missing,"")]));
    }
    [Fact] public void ChangingAdtPinsInvalidatesStoredValidation()
    {
        using var t=new TestDirectory();var s=AppSettings.CreateDefault(t.Root);var p=new ConnectionProfile{TargetKey="fixture.invalid:04"};s.ActiveProfiles[p.TargetKey]=p.Id;
        p.BasicValidation=new(BasicCheck.Passed,BasicCheck.Passed,DateTimeOffset.UtcNow,BasicPolicy.Fingerprint(s,p),2);
        p.Extra["adtTlsFingerprintPins"]=JsonSerializer.SerializeToElement(new[]{new{origin="https://fixture.invalid",sha256=new string('a',64)}});
        Assert.Empty(BasicPolicy.Eligible(s,p));
    }
    [Fact] public async Task VerifyRechecksAfterConsentAndRecoversWithoutRestart()
    {
        using var t=new TestDirectory();var store=new SettingsStore(Path.Combine(t.Root,"config","settings.json"));var s=await new ConnectionManager(store).SelectSourceAsync(AppSettings.CreateDefault(t.Root),"managed");
        var draft=new BasicDraft{Sid="TST",Host="fixture.invalid",Sysnr="04",User="TEST",Password="fixture",ManualUrl="https://fixture.invalid:44300"};var events=new List<string>();var available=false;
        var service=new BasicConnectionService(new DpapiSecretStore()){Hooks=new(){Check=(_,_,c,_)=>{events.Add(c);return Task.FromResult(BasicCheck.Passed);},Servers=(_,_,_,_)=>Task.FromResult(new BasicRfcServers("AUTHORIZATION_DENIED",[])),Discover=(_,_,_,_,_,_)=>Task.FromResult(new BasicDiscovery(2,[new(draft.ManualUrl,0,"AUTHENTICATION_REQUIRED",401,false)],false,true,1))}};
        Task<RfcReadinessResult> Check(CancellationToken ct){events.Add("readiness");var checks=Ready();if(!available)checks.RemoveAll(c=>c.Id=="sdk");return Task.FromResult(RfcReadinessService.Evaluate(s,true,checks));}
        Task<bool> Consent(BasicPrompt p,CancellationToken ct){events.Add(p.ToString());return Task.FromResult(true);}
        var first=await service.ValidateAsync(s,store,draft,Check,Consent);Assert.Equal("RFC_DEPENDENCY_MISSING",first.Rfc.Code);Assert.DoesNotContain("rfc",events);
        available=true;events.Clear();var second=await service.ValidateAsync(s,store,draft,Check,Consent);Assert.True(second.Rfc.Success);Assert.True(events.IndexOf("ConnectionWarning")<events.IndexOf("readiness"));Assert.True(events.IndexOf("readiness")<events.IndexOf("rfc"));
        events.Clear();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>service.ValidateAsync(s,store,draft,Check,(_,_)=>Task.FromResult(false)));Assert.DoesNotContain("readiness",events);
    }
}
