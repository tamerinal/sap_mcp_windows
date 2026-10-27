using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using SapMcp.Core;

namespace SapMcp.Desktop;

public partial class MainWindow
{
    private bool _modeGuard;
    private List<Control>? _advancedControls;
    private Action? _advancedRefresh;
    private StackPanel? _basicRoot;
    private readonly List<Control> _basicSteps=[];
    private int _basicStep;
    private bool _basicPrepared;
    private bool _basicRfcReady;
    private BasicDraft _basicDraft=new();
    private CancellationTokenSource? _discoveryCancel;
    private long _discoveryGeneration;
    private BasicDiscovery? _discovery;
    private DateTimeOffset _discoveredAt;
    private Task? _discoveryTask;
    private TextBox? _basicHost,_basicInstance,_basicUser,_basicPassword,_basicClient,_basicLanguage,_basicUrl;
    private TextBlock? _basicDiscoveryStatus,_basicResult;
    private Button? _basicCertificateButton;
    private bool _basicFilling;
    private bool _basicInstanceWasDerived;
    private ComboBox? _basicProfilePicker;
    private string[] _basicProfileIds=[];
    private string _basicLoadedFields="";
    private (BasicCheck Adt,BasicCheck Rfc)? _basicLastResult;

    private void InitializeModePicker()
    {
        var picker=this.FindControl<ComboBox>("ModePicker")!;
        picker.ItemsSource=new[]{UiText.Get("Basic.Mode"),UiText.Get("Basic.Advanced")};
        picker.SelectionChanged+=async(_,_)=>
        {
            if(_modeGuard||!_loaded||_operation!=null)return;
            await RunAsync(async()=>
            {
                var mode=picker.SelectedIndex==0?"basic":"advanced";
                if(mode==_settings.UiMode)return;
                CancelBasicDiscovery();
                if(mode=="basic") {_advancedControls=Body.Children.ToList();_advancedRefresh=_refreshConnectionLanguage;}
                _settings.UiMode=mode;await PersistAsync();DisplayMode();
            });
        };
    }
    private void RefreshModeLanguage()
    {
        var picker=this.FindControl<ComboBox>("ModePicker");if(picker==null)return;
        _modeGuard=true;picker.ItemsSource=new[]{UiText.Get("Basic.Mode"),UiText.Get("Basic.Advanced")};picker.SelectedIndex=_settings.UiMode=="basic"?0:1;_modeGuard=false;
        if(_settings.UiMode=="basic")RefreshBasicTitle();
        if(_basicRoot!=null)
        {
            foreach(var expander in _basicRoot.GetLogicalDescendants().OfType<Expander>())if(expander.Header is string header)expander.Header=UiText.Redisplay(header);
            RefreshBasicProfiles();
        }
        if(_basicLastResult is {} r&&_basicResult!=null)
        {
            _basicResult.Text=UiText.Get("Basic.AdtResult")+" "+Outcome(r.Adt)+"\n"+UiText.Get("Basic.RfcResult")+" "+Outcome(r.Rfc);
            var clientResult=_basicRoot?.GetLogicalDescendants().OfType<TextBlock>().SingleOrDefault(t=>t.Name=="BasicClientResult");if(clientResult!=null)clientResult.Text=_basicResult.Text;
        }
    }
    private void DisplayInitialMode(){Closing+=(_,_)=>CancelBasicDiscovery();DisplayMode();}
    private void DisplayMode()
    {
        var basic=_settings.UiMode=="basic";this.FindControl<StackPanel>("Navigation")!.IsVisible=!basic;
        Body.Children.Clear();
        if(basic){_refreshConnectionLanguage=null;_basicRoot??=BuildBasic();Body.Children.Add(_basicRoot);SetBasicStep(_basicStep);}
        else if(_advancedControls!=null){foreach(var c in _advancedControls)Body.Children.Add(c);_refreshConnectionLanguage=_advancedRefresh;this.FindControl<TextBlock>("PageTitle")!.Text=UiText.Redisplay(_titles[_page]);this.FindControl<TextBlock>("PageSubtitle")!.Text=UiText.Redisplay(_subtitles[_page]);}
        else ShowPage(_page);
        ApplyLanguage(UiText.Language);
    }
    private void RefreshBasicTitle()
    {
        this.FindControl<TextBlock>("PageTitle")!.Text=UiText.Get("Basic.Title");
        this.FindControl<TextBlock>("PageSubtitle")!.Text=UiText.Get("Basic.Step"+(_basicStep+1));
    }
    private void SetBasicStep(int step)
    {
        _basicStep=step;for(var i=0;i<_basicSteps.Count;i++)_basicSteps[i].IsVisible=i==step;RefreshBasicTitle();
    }
    private StackPanel BuildBasic()
    {
        var root=new StackPanel{Spacing=18};
        var sdkForm=new StackPanel{Spacing=12,Name="BasicSdkStep"};
        sdkForm.Children.Add(Text(UiText.Get("Basic.SdkIntro")));
        var sdk=Field(sdkForm,UiText.Get("Basic.SdkLabel"),_settings.SdkDirectory);sdk.Name="BasicSdkDirectory";
        var adtOnly=new CheckBox{Content=UiText.Get("Basic.AdtOnly"),Name="BasicAdtOnly"};sdkForm.Children.Add(adtOnly);
        sdkForm.Children.Add(Row(Action(UiText.Get("Basic.Browse"),async()=>{var folder=await PickFolderAsync(UiText.Get("Basic.SdkLabel"));if(folder!=null)sdk.Text=folder;}),Action(UiText.Get("Basic.Prepare"),async()=>
        {
            var firstPreparation=!_basicPrepared;
            var preparation=new BasicPreparationService(_packages);
            _settings=await preparation.PrepareAsync(_settings,Read(sdk),adtOnly.IsChecked==true,Progress,Token);
            await PersistAsync();_basicRfcReady=preparation.RfcReady&&_settings.Rfc.Installed;
            await VerifyBasicPackagesAsync();_basicPrepared=true;
            if(!await EnsureBasicSourceAsync())return;
            await LoadConnectionsAsync(repairManaged:true);if(firstPreparation)FillBasicProfile(_settings.BasicProfileId??(_settings.Profiles.Count==1?_settings.Profiles[0].Id:null));SetBasicStep(1);
            SetStatus(UiText.Get(_basicRfcReady?"Basic.Prepared":"Basic.PreparedAdt")+(_basicRfcReady?"":" "+string.Join(", ",preparation.LastChecks.Where(c=>c.State!=CheckState.Ready&&c.Id is "sdk" or "python" or "pyrfc" or "vc-runtime" or "vc2013").Select(c=>c.Name))));
        },true)));
        sdkForm.Children.Add(Notice(UiText.Get("Basic.PrepareNotice")));
        root.Children.Add(sdkForm);_basicSteps.Add(sdkForm);

        var form=new StackPanel{Spacing=10,Name="BasicConnectionStep"};
        var profiles=new ComboBox{Name="BasicProfilePicker"};_basicProfilePicker=profiles;
        profiles.SelectionChanged+=async(_,_)=>
        {
            if(_basicFilling||profiles.SelectedIndex<0)return;
            var id=profiles.SelectedIndex==0?null:_basicProfileIds[profiles.SelectedIndex-1];
            if(BasicFields()!=_basicLoadedFields&&!await ConfirmAsync(UiText.Get("Basic.SelectProfile"),UiText.Get("Connections.Unsaved"))){RefreshBasicProfiles();return;}
            FillBasicProfile(id);
        };
        form.Children.Add(Text(UiText.Get("Basic.SelectProfile")));form.Children.Add(profiles);
        _basicHost=Field(form,UiText.Get("Basic.Host"),"");_basicHost.Name="BasicHost";
        _basicUser=Field(form,UiText.Get("Basic.User"),"");_basicUser.Name="BasicUser";
        _basicPassword=Field(form,UiText.Get("Basic.Password"),"",true);_basicPassword.Name="BasicPassword";
        _basicClient=Field(form,UiText.Get("Basic.Client"),"100");_basicClient.Name="BasicClient";
        _basicLanguage=Field(form,UiText.Get("Basic.Language"),UiText.Language=="tr"?"TR":"EN");_basicLanguage.Name="BasicLanguage";
        _basicInstance=Field(form,UiText.Get("Basic.Instance"),"");_basicInstance.Name="BasicInstance";
        var manual=new StackPanel{Spacing=8};_basicUrl=Field(manual,UiText.Get("Basic.ManualUrl"),"");_basicUrl.Name="BasicAdtUrl";
        form.Children.Add(new Expander{Header=UiText.Get("Basic.Manual"),Content=manual});
        _basicDiscoveryStatus=Text(UiText.Get("Basic.DiscoveryIdle"));_basicDiscoveryStatus.Name="BasicDiscoveryStatus";form.Children.Add(_basicDiscoveryStatus);
        _basicResult=Text("");_basicResult.Name="BasicResult";form.Children.Add(_basicResult);
        _basicCertificateButton=Action(UiText.Get("Adt.Certificates"),ImportBasicTrustAsync);_basicCertificateButton.IsVisible=false;
        form.Children.Add(Row(Action(UiText.Get("Basic.Back"),()=>{CancelBasicDiscovery();SetBasicStep(0);return Task.CompletedTask;}),Action(UiText.Get("Basic.Help"),ShowAdtUrlHelpAsync),_basicCertificateButton,Action(UiText.Get("Basic.Save"),SaveBasicConnectionAsync,true)));
        _basicHost.TextChanged+=(_,_)=>BasicEndpointChanged();_basicUrl.TextChanged+=(_,_)=>BasicEndpointChanged();
        _basicInstance.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty&&!_basicFilling)_basicInstanceWasDerived=false;};
        foreach(var box in new[]{_basicUser,_basicPassword,_basicClient,_basicLanguage,_basicInstance})box.TextChanged+=(_,_)=>{if(!_basicFilling&&_basicResult!=null)_basicResult.Text=UiText.Get("Basic.Changed");};
        RefreshBasicProfiles();_basicLoadedFields=BasicFields();
        root.Children.Add(form);_basicSteps.Add(form);
        var clients=new StackPanel{Spacing=12,Name="BasicClientsStep"};
        clients.Children.Add(new TextBlock{Name="BasicClientResult",TextWrapping=Avalonia.Media.TextWrapping.Wrap});
        clients.Children.Add(Notice(UiText.Get("Basic.ClientsNotice")));
        var codex=new CheckBox{Content="Codex",IsChecked=_settings.CodexEnabled};var claude=new CheckBox{Content="Claude Desktop",IsChecked=_settings.ClaudeEnabled};clients.Children.Add(Row(codex,claude));
        var preview=new TextBox{IsReadOnly=true,AcceptsReturn=true,TextWrapping=Avalonia.Media.TextWrapping.Wrap,MinHeight=160};clients.Children.Add(preview);
        List<ClientPreview> previews=[];var replace=new CheckBox{Content=UiText.Get("Text210")};clients.Children.Add(replace);
        clients.Children.Add(Row(Action(UiText.Get("Basic.Back"),()=>{SetBasicStep(1);return Task.CompletedTask;}),Action(UiText.Get("Text211"),()=>
        {
            var p=_settings.Profiles.Single(p=>p.Id==_settings.BasicProfileId);var components=BasicPolicy.Eligible(_settings,p);if(components.Length==0)throw new InvalidOperationException(UiText.Get("Basic.Retest"));
            previews=[];if(codex.IsChecked==true)previews.Add(ClientIntegration.Preview("Codex",ClientIntegration.CodexPath,_settings,_store.FilePath,components));if(claude.IsChecked==true)previews.Add(ClientIntegration.Preview("Claude Desktop",ClientIntegration.ClaudePath,_settings,_store.FilePath,components));
            preview.Text=string.Join("\n\n",previews.Select(p=>p.Client+"\n"+(p.Conflicts.Count>0?UiText.Get("Text212")+" "+string.Join(", ",p.Conflicts)+"\n":"")+p.After));return Task.CompletedTask;
        }),Action(UiText.Get("Text214"),async()=>
        {
            if(previews.Count==0)throw new InvalidOperationException(UiText.Get("Text215"));
            var profile=_settings.Profiles.Single(p=>p.Id==_settings.BasicProfileId);var valid=BasicPolicy.Eligible(_settings,profile).Select(ClientIntegration.ServerName).ToHashSet();
            if(previews.Any(p=>p.Names.Any(n=>!valid.Contains(n))))throw new InvalidOperationException(UiText.Get("Basic.Retest"));
            foreach(var p in previews){await ClientIntegration.ApplyAsync(p,replace.IsChecked==true,Token);var names=_settings.Registrations.Where(r=>r.Client==p.Client&&r.ConfigPath==p.Path).SelectMany(r=>r.ServerNames).Concat(p.Names).Distinct().ToList();_settings.Registrations.RemoveAll(r=>r.Client==p.Client&&r.ConfigPath==p.Path);_settings.Registrations.Add(new(p.Client,p.Path,names));}
            _settings.CodexEnabled=codex.IsChecked==true;_settings.ClaudeEnabled=claude.IsChecked==true;await PersistAsync();previews=[];SetStatus(UiText.Get("Text217"));
        },true)));
        root.Children.Add(clients);_basicSteps.Add(clients);return root;
    }

    private async Task<bool> EnsureBasicSourceAsync()
    {
        if(ConnectionManager.Mode(_settings)=="managed")return true;
        if(!await ConfirmAsync(UiText.Get("Basic.ConvertTitle"),UiText.Get("Basic.ConvertBody")+"\n\n"+string.Join("\n",_systems.Select(s=>s.ToString()))))return false;
        _settings=await new ConnectionManager(_store).ConvertForBasicAsync(_settings,Token);return true;
    }
    private string BasicFields()=>string.Join("\u001f",new[]{_basicHost,_basicInstance,_basicUser,_basicPassword,_basicClient,_basicLanguage,_basicUrl}.Select(x=>x?.Text??""));
    private void RefreshBasicProfiles()
    {
        if(_basicProfilePicker==null)return;var filling=_basicFilling;_basicFilling=true;
        _basicProfileIds=_settings.Profiles.Select(p=>p.Id).ToArray();_basicProfilePicker.ItemsSource=new[]{UiText.Get("Basic.NewProfile")}.Concat(_settings.Profiles.Select(p=>p.ToString())).ToArray();_basicProfilePicker.SelectedIndex=Array.IndexOf(_basicProfileIds,_basicDraft.ProfileId)+1;_basicFilling=filling;
    }
    private async Task VerifyBasicPackagesAsync()
    {
        var root=Path.Combine(_settings.DataDirectory,"cache","basic-bootstrap",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var s=BasicPolicy.Copy(_settings);s.Profiles=[];s.ActiveProfiles=[];s.Rfc.Selected=_basicRfcReady;s.Adt.Selected=true;
            s.LandscapeFile=Path.Combine(root,"landscape.xml");s.CredentialsFile=Path.Combine(root,"mapping.json");
            await AtomicFile.WriteAsync(s.LandscapeFile,ManagedJsonConnectionSource.ExportXml(new()),Token,true);await ProfileService.MaterializeAsync(s,Token);
            var file=Path.Combine(root,"settings.json");await new SettingsStore(file).SaveAsync(s,Token);
            foreach(var component in new[]{"adt","rfc"}.Where(c=>c=="adt"||_basicRfcReady))
            {await using var session=await McpSession.StartAsync(s,file,component,Token);var tools=await session.RequestAsync("tools/list",ct:Token);if(tools.GetProperty("tools").GetArrayLength()==0)throw new InvalidDataException(UiText.Get("Basic.PrepareFirst"));}
        }
        finally{Directory.Delete(root,true);}
    }
    private void FillBasicProfile(string? id)
    {
        CancelBasicDiscovery();_basicFilling=true;
        _basicInstanceWasDerived=false;
        var p=_settings.Profiles.SingleOrDefault(p=>p.Id==id);var system=p==null?null:_systems.SingleOrDefault(s=>s.TargetKey.Equals(p.TargetKey,StringComparison.OrdinalIgnoreCase));
        _basicDraft=new(){ProfileId=p?.Id??Guid.NewGuid().ToString("N"),SystemId=system?.Id??Guid.NewGuid().ToString("N"),Certificates=p?.AdtCertificates.ToList()??[]};
        _basicHost!.Text=system?.Host??"";_basicInstance!.Text=system?.Sysnr??"";_basicUser!.Text=p?.User??"";_basicPassword!.Text="";_basicClient!.Text=p?.Client??"100";_basicLanguage!.Text=p?.Language??(UiText.Language=="tr"?"TR":"EN");_basicUrl!.Text=p?.AdtBaseUrl??"";
        _basicLastResult=null;_basicResult!.Text="";_basicFilling=false;_basicLoadedFields=BasicFields();RefreshBasicProfiles();if(_basicPrepared)QueueBasicDiscovery();
    }
    private BasicDraft CaptureBasicDraft()
    {
        _basicDraft.Host=Read(_basicHost!);_basicDraft.Sysnr=Read(_basicInstance!);_basicDraft.User=Read(_basicUser!);_basicDraft.Password=_basicPassword!.Text??"";_basicDraft.Client=Read(_basicClient!);_basicDraft.Language=Read(_basicLanguage!);_basicDraft.ManualUrl=Read(_basicUrl!);return BasicPolicy.Copy(_basicDraft);
    }
    private void CancelBasicDiscovery(){_discoveryGeneration++;_discoveryCancel?.Cancel();_discoveryCancel?.Dispose();_discoveryCancel=null;_discovery=null;}
    private void QueueBasicDiscovery()
    {
        if(_basicFilling||!_basicPrepared)return;CancelBasicDiscovery();var generation=_discoveryGeneration;_discoveryCancel=new();var token=_discoveryCancel.Token;
        _discoveryTask=DiscoverBasicInBackgroundAsync(generation,token);
    }
    private void BasicEndpointChanged()
    {
        if(_basicFilling)return;
        if(_basicInstanceWasDerived){_basicInstance!.Text="";_basicInstanceWasDerived=false;}
        QueueBasicDiscovery();
    }
    private void SetInferredInstance(int value){_basicInstance!.Text=value.ToString("00");_basicInstanceWasDerived=true;}
    private async Task DiscoverBasicInBackgroundAsync(long generation,CancellationToken token)
    {
        try
        {
            _basicDiscoveryStatus!.Text=UiText.Get("Basic.Discovering");await Task.Delay(BasicPolicy.Discovery.DebounceMs,token);
            var draft=CaptureBasicDraft();draft.Password=""; // Password never enters the discovery subprocess.
            var result=await new BasicDiscoveryService().DiscoverAsync(_settings,draft,token);
            if(generation!=_discoveryGeneration||token.IsCancellationRequested)return;
            _discovery=result;_discoveredAt=DateTimeOffset.UtcNow;
            _basicCertificateButton!.IsVisible=_basicDraft.Certificates.Count>0||result.Candidates.Any(c=>c.Code.StartsWith("TLS_",StringComparison.Ordinal));
            var useful=result.Candidates.FirstOrDefault(c=>c.Verified)??result.Candidates.FirstOrDefault(c=>c.HttpStatus==401);
            var error=result.Candidates.Select(c=>c.Code).FirstOrDefault(c=>c.StartsWith("TLS_",StringComparison.Ordinal)||c is "AUTHORIZATION_DENIED" or "NETWORK_TIMEOUT" or "DNS_FAILED");
            _basicDiscoveryStatus!.Text=UiText.Get(useful?.Verified==true?"Basic.Discovered":useful!=null?"Basic.AuthCandidate":error!=null?"Basic.DiscoveryIssue":"Basic.NotFound")+(error==null?"":" · "+BasicPolicy.SafeCode(error));
        }
        catch(OperationCanceledException){}
        catch{if(generation==_discoveryGeneration)_basicDiscoveryStatus!.Text=UiText.Get("Basic.DiscoveryIssue");}
    }
    private async Task SaveBasicConnectionAsync()
    {
        if(!_basicPrepared)throw new InvalidOperationException(UiText.Get("Basic.PrepareFirst"));
        if(_discoveryTask!=null)await _discoveryTask;
        if(_discovery==null||DateTimeOffset.UtcNow-_discoveredAt>TimeSpan.FromSeconds(BasicPolicy.Discovery.MaxAgeSeconds))
        {QueueBasicDiscovery();if(_discoveryTask!=null)await _discoveryTask;}
        var draft=CaptureBasicDraft();
        if(draft.Sysnr.Length==0)
        {
            var host=BasicPolicy.ParseHost(draft.Host).Host;var values=(_discovery?.Candidates??[]).Where(c=>c.Verified||c.HttpStatus==401).Select(c=>BasicPolicy.InstanceCandidate(c.Url,host)).Where(x=>x!=null).Distinct().ToArray();
            if(values.Length==1){SetInferredInstance(values[0]!.Value);draft.Sysnr=Read(_basicInstance!);}
            else{_basicResult!.Text=UiText.Get("Basic.InstanceRequired");_basicInstance!.Focus();return;}
        }
        var active=_settings.Profiles.SingleOrDefault(p=>p.Id==_basicDraft.ProfileId);
        if(active!=null)
        {
            var target=BasicPolicy.ParseHost(draft.Host).Host+":"+draft.Sysnr;
            if(!active.TargetKey.Equals(target,StringComparison.OrdinalIgnoreCase))
            {
                var affected=_settings.Profiles.Where(p=>p.TargetKey.Equals(active.TargetKey,StringComparison.OrdinalIgnoreCase)).ToArray();
                if(!await ConfirmAsync(UiText.Get("Connections.MoveTitle"),UiText.Get("Connections.MoveBody",active.TargetKey,target,affected.Length,string.Join("\n",affected.Select(p=>p.ToString())))))return;
            }
        }
        if(active!=null&&_settings.ActiveProfiles.TryGetValue(active.TargetKey,out var current)&&current!=active.Id&&!await ConfirmAsync(UiText.Get("Basic.Save"),UiText.Get("Basic.ActivateNotice")))return;
        var result=await new BasicConnectionService(_secrets).SaveAsync(_settings,_store,draft,_discovery??new(1,[],false,false,0),_basicRfcReady,Token);
        _basicLastResult=(result.Adt,result.Rfc);
        _basicResult!.Text=UiText.Get("Basic.AdtResult")+" "+Outcome(result.Adt)+"\n"+UiText.Get("Basic.RfcResult")+" "+Outcome(result.Rfc);
        if(result.Settings==null){SetStatus(UiText.Get("Basic.BothFailed"));return;}
        _settings=result.Settings;_basicPassword!.Text="";_basicDraft.Password="";await LoadConnectionsAsync(repairManaged:false);
        _basicLoadedFields=BasicFields();RefreshBasicProfiles();
        SetStatus(UiText.Get(result.Adt.Success&&result.Rfc.Success?"Basic.SavedBoth":"Basic.SavedPartial"));
        _basicRoot!.GetLogicalDescendants().OfType<TextBlock>().Single(t=>t.Name=="BasicClientResult").Text=_basicResult.Text;
        SetBasicStep(2);
    }
    private static string Outcome(BasicCheck check)=>UiText.Get(check.Success?"Basic.Passed":"Basic.Failed")+(check.Success?"":" · "+check.Code);
    private async Task ImportBasicTrustAsync()
    {
        var file=await PickFileAsync(UiText.Get("Adt.ImportCertificate"),"*");if(file==null)return;
        var certs=await AdtProfiles.ReadCertificatesAsync(file,Token);
        if(!await ConfirmAsync(UiText.Get("Adt.ConfirmTrust"),UiText.Get("Adt.ConfirmTrustBody")+"\n\n"+string.Join("\n\n",certs.Select(c=>c.Display))))return;
        var stored=await AdtProfiles.StoreCertificatesAsync(_settings,new(){Id=_basicDraft.ProfileId},certs,Token);
        _basicDraft.Certificates=_basicDraft.Certificates.Concat(stored).DistinctBy(c=>c.Sha256).ToList();QueueBasicDiscovery();
    }
}
