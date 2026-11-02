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
    private bool _basicRestored;
    private bool _basicMaintenance;
    private TextBlock? _basicSummary;
    private IReadOnlyList<Requirement> _basicChecks=[];
    private bool _basicRfcReady;
    private RfcReadinessResult? _basicReadiness;
    private BasicDraft _basicDraft=new();
    private CancellationTokenSource? _discoveryCancel;
    private long _discoveryGeneration;
    private BasicDiscovery? _discovery;
    private TextBox? _basicSid,_basicHost,_basicInstance,_basicUser,_basicPassword,_basicClient,_basicLanguage,_basicUrl;
    private TextBlock? _basicDiscoveryStatus,_basicResult;
    private Button? _basicNextButton;
    private StackPanel? _basicManualPanel;
    private string _basicConsentKey="";
    private bool _basicValidationRunning;
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
                _settings.UiMode=mode;await PersistAsync();DisplayMode();if(mode=="basic"){if(!_basicRestored)await RestoreBasicAsync();else await InspectBasicAsync();}else InvalidateBasicReadiness();
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
        if(_discovery!=null&&_basicDiscoveryStatus!=null)_basicDiscoveryStatus.Text=BasicPolicy.DiscoveryText(_discovery);
        RefreshBasicSummary();_refreshBasicClients?.Invoke();UpdateBasicNext();
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
        this.FindControl<TextBlock>("PageSubtitle")!.Text=UiText.Get(_basicStep==3?"Basic.Summary":"Basic.Step"+(_basicStep+1));
    }
    private void SetBasicStep(int step)
    {
        _basicStep=step;for(var i=0;i<_basicSteps.Count;i++)_basicSteps[i].IsVisible=i==step;RefreshBasicTitle();
        if(step==2)_refreshBasicClients?.Invoke();
        if(step==3)RefreshBasicSummary();
    }
    private StackPanel BuildBasic()
    {
        var root=new StackPanel{Spacing=18};
        root.Children.Add(Row(
            Action(UiText.Get("Basic.Summary"),()=>NavigateBasicAsync(3)),
            Action(UiText.Get("Basic.NavPackages"),()=>NavigateBasicAsync(0)),
            Action(UiText.Get("Basic.NavConnection"),()=>NavigateBasicAsync(1)),
            Action(UiText.Get("Basic.NavClients"),()=>NavigateBasicAsync(2))));
        var sdkForm=new StackPanel{Spacing=12,Name="BasicSdkStep"};
        sdkForm.Children.Add(Text(UiText.Get("Basic.SdkIntro")));
        var sdk=Field(sdkForm,UiText.Get("Basic.SdkLabel"),_settings.SdkDirectory);sdk.Name="BasicSdkDirectory";
        var adtOnly=new CheckBox{Content=UiText.Get("Basic.AdtOnly"),Name="BasicAdtOnly",IsChecked=BasicInstallation.AdtOnly(_settings)};sdkForm.Children.Add(adtOnly);
        sdkForm.Children.Add(Row(Action(UiText.Get("Basic.Browse"),async()=>{var folder=await PickFolderAsync(UiText.Get("Basic.SdkLabel"));if(folder!=null)sdk.Text=folder;}),Action(UiText.Get(BasicInstallation.IsMaintenance(_settings)?"Basic.UpdatePackages":"Basic.Prepare"),async()=>
        {
            var firstPreparation=!_basicPrepared;
            var preparation=new BasicPreparationService(_packages);
            if(_settings.BasicSetup==null&&!BasicInstallation.IsMaintenance(_settings))
            { _settings.BasicSetup=new(null,adtOnly.IsChecked==true);await PersistAsync(); }
            var previous=_settings;
            _settings=await preparation.PrepareAsync(_settings,Read(sdk),adtOnly.IsChecked==true,Progress,Token,_basicMaintenance);
            _basicRfcReady=preparation.RfcReady&&_settings.Rfc.Installed;
            try { await VerifyBasicPackagesAsync(); }
            catch { _settings.BasicSetup=previous.BasicSetup;throw; }
            _basicPrepared=true;_basicMaintenance=true;_basicChecks=preparation.LastChecks;await PersistAsync();
            if(_basicRoot!=null)foreach(var button in _basicRoot.GetLogicalDescendants().OfType<Button>().Where(b=>b.Content is string text&&text==UiText.Get("Basic.Prepare")))button.Content=UiText.Get("Basic.UpdatePackages");
            if(!await EnsureBasicSourceAsync())return;
            await LoadConnectionsAsync(repairManaged:true);if(firstPreparation)FillBasicProfile(_settings.BasicProfileId??(_settings.Profiles.Count==1?_settings.Profiles[0].Id:null));SetBasicStep(1);
            SetStatus(UiText.Get(_basicRfcReady?"Basic.Prepared":"Basic.PreparedAdt")+(_basicRfcReady?"":" "+string.Join(", ",preparation.LastChecks.Where(c=>c.State!=CheckState.Ready&&c.Id is "sdk" or "python" or "pyrfc" or "vc-runtime" or "vc2013").Select(c=>c.Name))));
        },true)));
        sdkForm.Children.Add(Notice(UiText.Get("Basic.MaintenanceNotice")));
        sdkForm.Children.Add(Action(UiText.Get("Basic.AdvancedRepair"),OpenBasicRepairAsync));
        root.Children.Add(sdkForm);_basicSteps.Add(sdkForm);

        var form=new StackPanel{Spacing=10,Name="BasicConnectionStep"};
        var profiles=new ComboBox{Name="BasicProfilePicker"};_basicProfilePicker=profiles;
        profiles.SelectionChanged+=async(_,_)=>
        {
            if(_basicFilling||profiles.SelectedIndex<0)return;
            var id=profiles.SelectedIndex==0?null:_basicProfileIds[profiles.SelectedIndex-1];
            if(BasicFields()!=_basicLoadedFields&&!await ConfirmAsync(UiText.Get("Basic.SelectProfile"),UiText.Get("Connections.UnsavedBody"))){RefreshBasicProfiles();return;}
            FillBasicProfile(id);
        };
        form.Children.Add(Text(UiText.Get("Basic.SelectProfile")));form.Children.Add(profiles);
        _basicSid=Field(form,"SID","");_basicSid.Name="BasicSid";
        _basicHost=Field(form,UiText.Get("Basic.Host"),"");_basicHost.Name="BasicHost";
        _basicUser=Field(form,UiText.Get("Basic.User"),"");_basicUser.Name="BasicUser";
        _basicPassword=Field(form,UiText.Get("Basic.Password"),"",true);_basicPassword.Name="BasicPassword";
        _basicClient=Field(form,UiText.Get("Basic.Client"),"100");_basicClient.Name="BasicClient";
        _basicLanguage=Field(form,UiText.Get("Basic.Language"),UiText.Language=="tr"?"TR":"EN");_basicLanguage.Name="BasicLanguage";
        _basicInstance=Field(form,UiText.Get("Basic.Instance"),"");_basicInstance.Name="BasicInstance";
        var manual=new StackPanel{Spacing=8};_basicUrl=Field(manual,UiText.Get("Basic.ManualUrl"),"");_basicUrl.Name="BasicAdtUrl";
        _basicManualPanel=manual;manual.IsVisible=false;form.Children.Add(manual);
        form.Children.Add(Notice(UiText.Get("Basic.ConnectionWarning")));
        _basicDiscoveryStatus=Text(UiText.Get("Basic.DiscoveryIdle"));_basicDiscoveryStatus.Name="BasicDiscoveryStatus";form.Children.Add(_basicDiscoveryStatus);
        _basicResult=Text("");_basicResult.Name="BasicResult";form.Children.Add(_basicResult);
        var save=Action(UiText.Get("Basic.SaveOnly"),SaveBasicConnectionAsync);save.Name="BasicSaveConnection";
        var verify=Action(UiText.Get("Basic.Verify"),ValidateBasicConnectionAsync,true);verify.Name="BasicVerifyConnection";
        _basicNextButton=Action(UiText.Get("Basic.Next"),()=>NavigateBasicAsync(2));_basicNextButton.Name="BasicNext";_basicNextButton.IsEnabled=false;
        form.Children.Add(Row(Action(UiText.Get("Basic.Back"),()=>NavigateBasicAsync(0)),save,verify,_basicNextButton));
        _basicSid.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty&&!_basicFilling)InvalidateBasicResult();};
        _basicHost.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty)BasicEndpointChanged();};
        _basicUrl.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty)BasicEndpointChanged();};
        _basicInstance.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty&&!_basicFilling){_basicInstanceWasDerived=false;InvalidateBasicResult();_basicConsentKey="";}};
        foreach(var box in new[]{_basicUser,_basicPassword,_basicClient,_basicLanguage})box.PropertyChanged+=(_,e)=>{if(e.Property==TextBox.TextProperty&&!_basicFilling)InvalidateBasicResult();};
        RefreshBasicProfiles();_basicLoadedFields=BasicFields();
        root.Children.Add(form);_basicSteps.Add(form);
        var clients=new StackPanel{Spacing=12,Name="BasicClientsStep"};
        clients.Children.Add(new TextBlock{Name="BasicClientResult",TextWrapping=Avalonia.Media.TextWrapping.Wrap});
        clients.Children.Add(Notice(UiText.Get("Basic.ClientsNotice")));
        clients.Children.Add(BuildClientIntegration(true));
        clients.Children.Add(Action(UiText.Get("Basic.Back"),()=>NavigateBasicAsync(1)));
        root.Children.Add(clients);_basicSteps.Add(clients);
        var summary=new StackPanel{Spacing=12,Name="BasicSummaryStep"};
        _basicSummary=Text("");_basicSummary.Name="BasicSummaryText";summary.Children.Add(_basicSummary);
        summary.Children.Add(Row(Action(UiText.Get("Basic.RefreshStatus"),async()=>await InspectBasicAsync()),Action(UiText.Get("Basic.AdvancedRepair"),OpenBasicRepairAsync)));
        summary.Children.Add(BuildCustomIntegration(true,"BasicSummary",out _refreshCustomSummary));
        root.Children.Add(summary);_basicSteps.Add(summary);return root;
    }

    private async Task NavigateBasicAsync(int step)
    {
        if(_basicValidationRunning)return;
        if(step==2&&(_basicLastResult==null&&_settings.Profiles.SingleOrDefault(p=>p.Id==_basicDraft.ProfileId)?.BasicValidation==null||BasicFields()!=_basicLoadedFields||!BasicCanContinue()))
        {if(_basicResult!=null)_basicResult.Text=UiText.Get("Basic.Retest");return;}
        if(_basicStep==1&&step!=1&&BasicFields()!=_basicLoadedFields)
        {
            if(!await ConfirmAsync(UiText.Get("Basic.Title"),UiText.Get("Connections.UnsavedBody")))return;
            FillBasicProfile(_basicDraft.ProfileId,false);
        }
        CancelBasicDiscovery();SetBasicStep(step);
    }
    private async Task OpenBasicRepairAsync()
    {
        if(_basicStep==1&&BasicFields()!=_basicLoadedFields)
        {if(!await ConfirmAsync(UiText.Get("Basic.Title"),UiText.Get("Connections.UnsavedBody")))return;FillBasicProfile(_basicDraft.ProfileId,false);}
        CancelBasicDiscovery();_settings.UiMode="advanced";await PersistAsync();DisplayMode();ShowPage(1);
    }
    private async Task RestoreBasicAsync()
    {
        _basicRestored=true;
        if(_basicRoot==null)return;
        var history=BasicInstallation.HasHistory(_settings);_basicMaintenance=BasicInstallation.IsMaintenance(_settings);
        FillBasicProfile(_settings.BasicProfileId??(_settings.Profiles.Count==1?_settings.Profiles[0].Id:null),false);
        var profile=_settings.Profiles.SingleOrDefault(p=>p.Id==_basicDraft.ProfileId);
        if(profile!=null&&_settings.BasicProfileId==null)_settings.BasicProfileId=profile.Id;
        if(profile?.BasicValidation is {} validation)
        {
            _basicResult!.Text=UiText.Get("Basic.LastValidation",validation.TestedAt.ToLocalTime().ToString("g"))+"\n"+UiText.Get("Basic.AdtResult")+" "+Outcome(validation.Adt)+"\n"+UiText.Get("Basic.RfcResult")+" "+Outcome(validation.Rfc);
            _basicRoot.GetLogicalDescendants().OfType<TextBlock>().Single(t=>t.Name=="BasicClientResult").Text=_basicResult.Text;
        }
        SetBasicStep(history?3:0);
        if(history)await InspectBasicAsync();else RefreshBasicSummary();
    }
    private void InvalidateBasicReadiness(){_basicReadiness=null;_basicRfcReady=false;_refreshBasicClients?.Invoke();}
    private async Task InspectBasicAsync()
    {
        var candidate=BasicPolicy.Copy(_settings);candidate.Adt.Selected=true;candidate.Rfc.Selected=!BasicInstallation.AdtOnly(_settings);
        var enabled=!BasicInstallation.AdtOnly(_settings);
        _basicReadiness=await new RfcReadinessService(_packages).CheckAsync(candidate,enabled,Token);
        _basicChecks=enabled?_basicReadiness.Checks:await new PrerequisiteService(_packages).CheckAsync(candidate,Token);
        foreach(var component in new[]{"adt","rfc"})
        {
            _settings.Component(component).Installed=File.Exists(Path.Combine(_settings.PackageDirectory,component,"dist","server.js"));
            if(_settings.Component(component).Installed)_settings.Component(component).Version=BasicInstallation.InstalledVersion(_settings,component);
        }
        var adtRoot=Path.Combine(_settings.PackageDirectory,"adt","dist");
        _basicPrepared=BasicInstallation.SharedReady(_basicChecks)&&File.Exists(Path.Combine(adtRoot,"server.js"))&&File.Exists(Path.Combine(adtRoot,"basic-connection.js"));
        _basicRfcReady=_basicReadiness.Ready;
        RefreshBasicSummary();_refreshBasicClients?.Invoke();UpdateBasicNext();
    }
    private void RefreshBasicSummary()
    {
        _refreshCustomSummary?.Invoke();
        if(_basicSummary==null)return;
        var lines=new List<string>{UiText.Get("Basic.LocalStatus")};
        if(_settings.BasicSetup is {PreparedAt:null})lines.Add(UiText.Get("Basic.PreparationIncomplete"));
        lines.AddRange(_basicChecks.Select(c=>c.Name+" Â· "+UiText.Get(c.State==CheckState.Ready?"Basic.Ready":"Basic.NeedsAttention")));
        try
        {
            var catalog=_packages.LoadCatalog();
            foreach(var component in new[]{"adt","rfc"})
                lines.Add(component.ToUpperInvariant()+" Â· "+_settings.Component(component).Version+" Â· "+UiText.Get("Packages."+BasicInstallation.Package(_settings,component,catalog.Find(component).Version)));
        }
        catch(Exception ex)when(ex is IOException or InvalidDataException or System.Text.Json.JsonException){lines.Add(UiText.Get("Basic.NeedsAttention"));}
        var profile=_settings.Profiles.SingleOrDefault(p=>p.Id==_settings.BasicProfileId);
        lines.Add(profile==null?UiText.Get("Basic.NoProfile"):profile.ToString());
        if(profile?.BasicValidation is {} v)lines.Add(UiText.Get("Basic.LastValidation",v.TestedAt.ToLocalTime().ToString("g"))+" Â· ADT: "+Outcome(v.Adt)+" Â· RFC: "+Outcome(v.Rfc)+(BasicPolicy.Eligible(_settings,profile).Length==0?"\n"+UiText.Get("Basic.Retest"):""));
        foreach(var (client,path) in new[]{("Codex",ClientIntegration.CodexPath),("Claude Desktop",ClientIntegration.ClaudePath),("Claude Code",ClientIntegration.ClaudeCodePath)})
        {
            var registration=_settings.Registrations.FirstOrDefault(r=>r.Client==client&&r.ConfigPath==path);
            if(registration==null){lines.Add(client+" Â· "+UiText.Get("Basic.NotRegistered"));continue;}
            try
            {
                var preview=ClientIntegration.Preview(client,path,_settings,_store.FilePath,registration.ServerNames.Select(n=>n=="sap_rfc_call"?"rfc":"adt"));
                lines.Add(client+" Â· "+UiText.Get(preview.Entries.All(e=>e.State==ClientEntryState.Current)?"Clients.Current":"Basic.NeedsAttention"));
            }
            catch(Exception ex)when(ex is IOException or System.Text.Json.JsonException or InvalidDataException or UnauthorizedAccessException){lines.Add(client+" Â· "+UiText.Get("Basic.NeedsAttention"));}
        }
        _basicSummary.Text=string.Join("\n\n",lines);
    }

    private async Task<bool> EnsureBasicSourceAsync()
    {
        if(ConnectionManager.Mode(_settings)=="managed")return true;
        if(!await ConfirmAsync(UiText.Get("Basic.ConvertTitle"),UiText.Get("Basic.ConvertBody")+"\n\n"+string.Join("\n",_systems.Select(s=>s.ToString()))))return false;
        _settings=await new ConnectionManager(_store).ConvertForBasicAsync(_settings,Token);return true;
    }
    private string BasicFields()=>string.Join("\u001f",new[]{_basicSid,_basicHost,_basicInstance,_basicUser,_basicPassword,_basicClient,_basicLanguage,_basicUrl}.Select(x=>x?.Text??""));
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
    private void FillBasicProfile(string? id, bool discover=true)
    {
        CancelBasicDiscovery();_basicConsentKey="";_basicFilling=true;
        _basicInstanceWasDerived=false;
        var p=_settings.Profiles.SingleOrDefault(p=>p.Id==id);var system=p==null?null:_systems.SingleOrDefault(s=>s.TargetKey.Equals(p.TargetKey,StringComparison.OrdinalIgnoreCase));
        _basicDraft=new(){ProfileId=p?.Id??Guid.NewGuid().ToString("N"),SystemId=system?.Id??Guid.NewGuid().ToString("N"),Certificates=p?.AdtCertificates.ToList()??[],PreferredUrl=p?.AdtBaseUrl??""};
        _basicSid!.Text=system?.Sid??"";_basicHost!.Text=system?.Host??"";_basicInstance!.Text=system?.Sysnr??"";_basicUser!.Text=p?.User??"";_basicPassword!.Text="";_basicClient!.Text=p?.Client??"100";_basicLanguage!.Text=p?.Language??(UiText.Language=="tr"?"TR":"EN");_basicUrl!.Text=p?.AdtBaseUrl??"";_basicManualPanel!.IsVisible=!string.IsNullOrWhiteSpace(p?.AdtBaseUrl);
        _basicLastResult=null;_basicResult!.Text="";_basicFilling=false;_basicLoadedFields=BasicFields();RefreshBasicProfiles();UpdateBasicNext();
    }
    private BasicDraft CaptureBasicDraft()
    {
        _basicDraft.Sid=Read(_basicSid!);_basicDraft.Host=Read(_basicHost!);_basicDraft.Sysnr=Read(_basicInstance!);_basicDraft.User=Read(_basicUser!);_basicDraft.Password=_basicPassword!.Text??"";_basicDraft.Client=Read(_basicClient!);_basicDraft.Language=Read(_basicLanguage!);_basicDraft.ManualUrl=Read(_basicUrl!);
        var original=_settings.Profiles.SingleOrDefault(p=>p.Id==_basicDraft.ProfileId);
        _basicDraft.PreferredUrl=original!=null&&original.TargetKey.Equals(BasicPolicy.ParseHost(_basicDraft.Host).Host+":"+_basicDraft.Sysnr,StringComparison.OrdinalIgnoreCase)&&original.BasicValidation?.Adt.Success==true?original.AdtBaseUrl:"";
        return BasicPolicy.Copy(_basicDraft);
    }
    private void CancelBasicDiscovery(){_discoveryGeneration++;_discoveryCancel?.Cancel();_discoveryCancel?.Dispose();_discoveryCancel=null;_discovery=null;}
    private void InvalidateBasicResult()
    {
        if(_basicFilling)return;
        CancelBasicDiscovery();_basicLastResult=null;
        if(_basicResult!=null)_basicResult.Text=UiText.Get("Basic.Changed");
        if(_basicNextButton!=null)_basicNextButton.IsEnabled=false;
        _refreshBasicClients?.Invoke();
    }
    private void BasicEndpointChanged()
    {
        if(_basicFilling)return;
        _basicConsentKey="";InvalidateBasicResult();
        if(_basicInstanceWasDerived){_basicInstance!.Text="";_basicInstanceWasDerived=false;}
    }
    private void SetInferredInstance(int value){_basicFilling=true;try{_basicInstance!.Text=value.ToString("00");_basicInstanceWasDerived=true;}finally{_basicFilling=false;}}
    private bool BasicCanContinue()
    {
        var p=_settings.Profiles.SingleOrDefault(p=>p.Id==_basicDraft.ProfileId);
        return !_basicValidationRunning&&_basicPrepared&&p!=null&&BasicPolicy.Eligible(_settings,p).Length>0;
    }
    private void UpdateBasicNext(){if(_basicNextButton!=null)_basicNextButton.IsEnabled=BasicFields()==_basicLoadedFields&&BasicCanContinue();}
    private async Task<bool> ConfirmBasicSaveTargetAsync(BasicDraft draft)
    {
        var active=_settings.Profiles.SingleOrDefault(p=>p.Id==draft.ProfileId);
        if(active!=null){
            var target=BasicPolicy.ParseHost(draft.Host).Host+":"+draft.Sysnr;
            if(!active.TargetKey.Equals(target,StringComparison.OrdinalIgnoreCase)){
                var affected=_settings.Profiles.Where(p=>p.TargetKey.Equals(active.TargetKey,StringComparison.OrdinalIgnoreCase)).ToArray();
                if(!await ConfirmAsync(UiText.Get("Connections.MoveTitle"),UiText.Get("Connections.MoveBody",active.TargetKey,target,affected.Length,string.Join("\n",affected.Select(p=>p.ToString())))))return false;
            }
            if(_settings.ActiveProfiles.TryGetValue(active.TargetKey,out var current)&&current!=active.Id&&!await ConfirmAsync(UiText.Get("Basic.SaveOnly"),UiText.Get("Basic.ActivateNotice")))return false;
        }
        return true;
    }
    private async Task SaveBasicConnectionAsync()
    {
        if(!await EnsureBasicSourceAsync())return;
        var draft=CaptureBasicDraft();if(!await ConfirmBasicSaveTargetAsync(draft))return;
        CancelBasicDiscovery();_basicLastResult=null;
        _settings=await new BasicConnectionService(_secrets).SaveDraftAsync(_settings,_store,draft,Token);
        await RefreshSavedBasicAsync();
        _basicResult!.Text=UiText.Get("Basic.SavedWithoutTest");SetStatus(_basicResult.Text);UpdateBasicNext();
    }
    private async Task RefreshSavedBasicAsync()
    {
        _basicFilling=true;try{_basicPassword!.Text="";_basicDraft.Password="";}finally{_basicFilling=false;}
        await LoadConnectionsAsync(repairManaged:false);_basicLoadedFields=BasicFields();RefreshBasicProfiles();_refreshBasicClients?.Invoke();
    }
    private async Task ValidateBasicConnectionAsync()
    {
        if(!_basicPrepared)throw new InvalidOperationException(UiText.Get("Basic.PrepareFirst"));
        if(!await EnsureBasicSourceAsync())return;
        var draft=CaptureBasicDraft();if(!await ConfirmBasicSaveTargetAsync(draft))return;
        CancelBasicDiscovery();_discoveryCancel=CancellationTokenSource.CreateLinkedTokenSource(Token);var ct=_discoveryCancel.Token;var generation=_discoveryGeneration;
        _basicValidationRunning=true;_basicLastResult=null;UpdateBasicNext();
        var consentKey=draft.ProfileId+"|"+draft.Host.Trim().ToLowerInvariant()+"|"+draft.Sysnr;
        async Task<bool> Confirm(BasicPrompt prompt,CancellationToken token){
            token.ThrowIfCancellationRequested();
            if(prompt==BasicPrompt.ConnectionWarning&&_basicConsentKey==consentKey)return true;
            var key=prompt switch{BasicPrompt.ConnectionWarning=>"Basic.ConnectionWarning",BasicPrompt.RfcDiscovery=>"Basic.RfcDiscoveryConsent",BasicPrompt.AutomaticScan=>"Basic.AutomaticConsent",_=>"Basic.ExtendedConsent"};
            var accepted=await ConfirmBasicNetworkAsync(UiText.Get(key),prompt==BasicPrompt.ConnectionWarning,token);token.ThrowIfCancellationRequested();
            if(accepted&&prompt==BasicPrompt.ConnectionWarning)_basicConsentKey=consentKey;return accepted;
        }
        try {
            var progress=new Progress<string>(message=>{if(generation==_discoveryGeneration&&!ct.IsCancellationRequested)_basicDiscoveryStatus!.Text=message;});
            async Task<RfcReadinessResult> Ready(CancellationToken token){
                var ready=await new RfcReadinessService(_packages).CheckAsync(_settings,!BasicInstallation.AdtOnly(_settings),token);
                _basicReadiness=ready;_basicRfcReady=ready.Ready;_basicChecks=ready.Checks;return ready;
            }
            var result=await new BasicConnectionService(_secrets).ValidateAsync(_settings,_store,draft,Ready,Confirm,progress,ct);
            if(generation!=_discoveryGeneration||ct.IsCancellationRequested)return;
            _discovery=result.Discovery;_basicLastResult=(result.Adt,result.Rfc);
            _basicResult!.Text=UiText.Get("Basic.AdtResult")+" "+Outcome(result.Adt)+(result.Url.Length>0?" Â· "+result.Url:"")+"\n"+UiText.Get("Basic.RfcResult")+" "+Outcome(result.Rfc);
            if(result.Discovery.BudgetExpired)_basicResult.Text+="\n"+UiText.Get("Basic.ScanIncomplete");
            if(result.Readiness is{Ready:false} blocked)_basicResult.Text+="\n"+UiText.Get("Rfc.ReadinessResult",blocked.Summary);
            _basicManualPanel!.IsVisible=true;
            if(result.Settings!=null){
                _settings=result.Settings;
                if(result.Url.Length>0){_basicFilling=true;try{_basicUrl!.Text=result.Url;}finally{_basicFilling=false;}}
                await RefreshSavedBasicAsync();
            }else SetStatus(UiText.Get("Basic.NeitherAvailable"));
            _basicRoot!.GetLogicalDescendants().OfType<TextBlock>().Single(t=>t.Name=="BasicClientResult").Text=_basicResult.Text;
        }finally{_basicValidationRunning=false;UpdateBasicNext();}
    }
    private async Task<bool> ConfirmBasicNetworkAsync(string description,bool warning,CancellationToken ct)
    {
        var dialog=new Window{Title=UiText.Get("Basic.Verify"),Width=520,MaxHeight=650,SizeToContent=SizeToContent.Height,WindowStartupLocation=WindowStartupLocation.CenterOwner,CanResize=false};
        var yes=new Button{Content=UiText.Get(warning?"Basic.Accept":"Text242")};var no=new Button{Content=UiText.Get("Text243")};
        yes.Click+=(_,_)=>dialog.Close(true);no.Click+=(_,_)=>dialog.Close(false);
        dialog.Content=new ScrollViewer{Content=new StackPanel{Margin=new(24),Spacing=20,Children={Text(description),Row(no,yes)}}};
        using var cancel=ct.Register(()=>Avalonia.Threading.Dispatcher.UIThread.Post(()=>dialog.Close(false)));
        ct.ThrowIfCancellationRequested();return await dialog.ShowDialog<bool>(this);
    }
    private static string Outcome(BasicCheck check)
    {
        var text=UiText.Get(check.Success?(check.Code=="OK_HTTP_PLAINTEXT"?"Basic.PassedHttp":check.Code=="OK_TLS_UNVERIFIED"?"Basic.PassedUnverifiedTls":"Basic.Passed"):"Basic.Failed")+(check.Success?"":" Â· "+check.Code);
        if(check.Sid!=null)text+=" Â· SID: "+check.Sid;if(check.Host!=null)text+=" Â· "+check.Host;if(check.Release!=null)text+=" Â· "+check.Release;
        return text;
    }
}
