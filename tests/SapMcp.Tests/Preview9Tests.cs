using SapMcp.Core;
using Xunit;
namespace SapMcp.Tests;
public class Preview9Tests {
 [Theory][InlineData("",null,false)][InlineData("", "AAA",false)][InlineData("aaa","AAA",true)][InlineData("BBB","AAA",false)][InlineData("XY",null,false)]
 public async Task SidIsRequiredAndMustMatchBackend(string supplied,string? detected,bool saved){
  using var t=new TestDirectory();var store=new SettingsStore(Path.Combine(t.Root,"config","settings.json"));
  var source=await new ConnectionManager(store).SelectSourceAsync(AppSettings.CreateDefault(t.Root),"managed");
  var draft=new BasicDraft{Host="fixture.invalid",Sysnr="02",Sid=supplied,User="DEMO",Password="fixture"};
  var discovery=new BasicDiscovery(1,[new("http://fixture.invalid:8080",0,"AUTHENTICATION_REQUIRED",401,false)],false,true,0);
  async Task Run(){var result=await new BasicConnectionService(new DpapiSecretStore()).SaveAsync(source,store,draft,discovery,true,tester:(_,_,component,_)=>Task.FromResult(new BasicCheck("passed","OK",component=="rfc"?detected:null)));Assert.NotNull(result.Settings);Assert.Equal("AAA",LandscapeService.Load(result.Settings!.LandscapeFile).Single().Sid);}
  if(saved)await Run();else{await Assert.ThrowsAsync<InvalidDataException>(Run);Assert.Empty((await store.LoadAsync()).Profiles);}
 }
 [Fact] public void LegacyBlankSidStillLoads(){new ManagedConnection{Name="Legacy",Host="fixture.invalid",Sysnr="00",Sid=""}.NormalizeAndValidate();}
}
