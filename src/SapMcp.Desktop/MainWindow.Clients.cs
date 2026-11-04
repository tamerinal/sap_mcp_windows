using Avalonia.Controls;
using Avalonia.Media;
using SapMcp.Core;

namespace SapMcp.Desktop;

public partial class MainWindow
{
    private Action? _refreshBasicClients;
    private StackPanel BuildClientIntegration(bool basic)
    {
        var form = new StackPanel { Spacing = 12 };
        var prefix = basic ? "Basic" : "Advanced";
        var codex = new CheckBox { Name=prefix+"Codex", Content="Codex", IsChecked=_settings.CodexEnabled };
        var desktop = new CheckBox { Name=prefix+"ClaudeDesktop", Content="Claude Desktop", IsChecked=_settings.ClaudeEnabled };
        var code = new CheckBox { Name=prefix+"ClaudeCode", Content="Claude Code", IsChecked=_settings.ClaudeCodeEnabled };
        var openCode=new CheckBox { Name=prefix+"OpenCode",Content="OpenCode",IsChecked=_settings.OpenCode?.Enabled==true };
        form.Children.Add(Row(codex,desktop,code,openCode));
        var openCodeForm=new StackPanel { Spacing=8,IsVisible=openCode.IsChecked==true };
        var openCodeScope=new ComboBox { Name=prefix+"OpenCodeScope",ItemsSource=new[]{UiText.Get("OpenCode.Global"),UiText.Get("OpenCode.Project")},SelectedIndex=_settings.OpenCode?.Scope=="project"?1:0 };
        var openCodeVersion=new ComboBox { Name=prefix+"OpenCodeVersion",ItemsSource=new[]{"OpenCode V1","OpenCode V2"},SelectedIndex=_settings.OpenCode?.Version==OpenCodeVersion.V1?0:1 };
        openCodeForm.Children.Add(Row(openCodeScope,openCodeVersion));
        var candidates=new ComboBox { Name=prefix+"OpenCodeFiles",IsVisible=false };
        var initialFiles=ClientIntegration.OpenCodeFiles(ClientIntegration.OpenCodeDirectory);
        var initialPath=_settings.OpenCode?.ConfigPath;
        if(string.IsNullOrEmpty(initialPath))initialPath=initialFiles.Length==1?initialFiles[0]:initialFiles.Length==0&&openCodeScope.SelectedIndex==0?Path.Combine(ClientIntegration.OpenCodeDirectory,"opencode.jsonc"):"";
        var openCodePath=Field(openCodeForm,UiText.Get("OpenCode.ConfigPath"),initialPath);openCodePath.Name=prefix+"OpenCodePath";
        openCodeForm.Children.Add(candidates);
        void ChooseDirectory(string directory)
        {
            var files=ClientIntegration.OpenCodeFiles(directory);candidates.ItemsSource=files;candidates.IsVisible=files.Length>1;
            openCodePath.Text=files.Length==1?files[0]:files.Length==0?Path.Combine(directory,"opencode.jsonc"):"";
            candidates.SelectedIndex=files.Length==1?0:-1;
        }
        if(initialFiles.Length>1 && string.IsNullOrEmpty(initialPath)){candidates.ItemsSource=initialFiles;candidates.IsVisible=true;}
        openCodeForm.Children.Add(Row(Action(UiText.Get("OpenCode.ChooseFolder"),async()=>{var folder=await PickFolderAsync(UiText.Get("OpenCode.ConfigPath"));if(folder!=null)ChooseDirectory(folder);}),Action(UiText.Get("OpenCode.ChooseFile"),async()=>{var file=await PickFileAsync(UiText.Get("OpenCode.ConfigPath"),"*");if(file!=null)openCodePath.Text=file;})));
        openCodeForm.Children.Add(Text(UiText.Get("OpenCode.ScopeHelp"),"muted"));
        form.Children.Add(openCodeForm);
        TextBox? codexPath=null,desktopPath=null,codePath=null;
        if (!basic)
        {
            codexPath=Field(form,"Codex config.toml",ClientIntegration.CodexPath);
            desktopPath=Field(form,"Claude Desktop",ClientIntegration.ClaudePath);
            codePath=Field(form,"Claude Code",ClientIntegration.ClaudeCodePath);
        }
        var summary=Text(""); summary.Name=prefix+"ClientPreview"; form.Children.Add(summary);
        var details=new TextBox { IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=140,MaxHeight=280,FontFamily=new("Consolas") };
        form.Children.Add(new Expander { Header=UiText.Get("Clients.Details"),Content=details });
        var replace=new CheckBox { Name=prefix+"ReplaceEntries",Content=UiText.Get("Basic.ReplaceClientEntries"),IsVisible=false };
        form.Children.Add(replace);
        var result=Text("");result.Name=prefix+"ClientSaveResult";
        List<ClientPreview> previews=[];
        Button? save=null;
        string[] Components() => ClientComponents(basic);
        var custom=BuildCustomIntegration(basic,prefix,out var refreshCustom);
        void UpdateSave() { if(save!=null)save.IsEnabled=previews.Count>0 && (previews.All(p=>p.Conflicts.Count==0)||replace.IsChecked==true); }
        void Preview()
        {
            refreshCustom();
            previews=[];replace.IsChecked=false;replace.IsVisible=false;UpdateSave();details.Text="";result.Text="";
            try
            {
                var components=Components();
                if (components.Length==0) { summary.Text=UiText.Get(basic?"Basic.Retest":"Text207"); return; }
                var next=new List<ClientPreview>();
                if(codex.IsChecked==true)next.Add(ClientIntegration.Preview("Codex",codexPath==null?ClientIntegration.CodexPath:Read(codexPath),_settings,_store.FilePath,components));
                if(desktop.IsChecked==true)next.Add(ClientIntegration.Preview("Claude Desktop",desktopPath==null?ClientIntegration.ClaudePath:Read(desktopPath),_settings,_store.FilePath,components));
                if(code.IsChecked==true)next.Add(ClientIntegration.Preview("Claude Code",codePath==null?ClientIntegration.ClaudeCodePath:Read(codePath),_settings,_store.FilePath,components));
                if(openCode.IsChecked==true)
                {
                    if(!Path.IsPathFullyQualified(Read(openCodePath)))throw new InvalidDataException(UiText.Get("OpenCode.ChoosePath"));
                    next.Add(ClientIntegration.PreviewOpenCode(Read(openCodePath),_settings,_store.FilePath,components,openCodeVersion.SelectedIndex==0?OpenCodeVersion.V1:OpenCodeVersion.V2));
                }
                previews=next;
                summary.Text=previews.Count==0?UiText.Get("Text209"):string.Join("\n\n",previews.Select(ClientIntegration.Describe));
                if(basic&&_basicReadiness is{Ready:false} blocked)summary.Text+="\n"+UiText.Get("Rfc.ReadinessResult",blocked.Summary);
                details.Text=string.Join("\n\n",previews.Select(p=>p.Client+" → "+p.Path+"\n"+ClientIntegration.Details(p)));
                replace.IsVisible=previews.Any(p=>p.Conflicts.Count>0);UpdateSave();
            }
            catch(Exception ex) when(ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException or ArgumentException)
            { previews=[];summary.Text=UiText.Get("OperationFailed",Redaction.Text(ex.Message));UpdateSave(); }
        }
        save=Action(UiText.Get("Basic.ApplyClientSettings"),async()=>
        {
            if(basic){_basicReadiness=await new RfcReadinessService(_packages).CheckAsync(_settings,!BasicInstallation.AdtOnly(_settings),Token);_basicRfcReady=_basicReadiness.Ready;}
            var valid=Components().Select(ClientIntegration.ServerName).ToHashSet();
            if(previews.Count==0 || previews.Any(p=>p.Components.Count>0?p.Components.Any(c=>!valid.Contains(ClientIntegration.ServerName(c))):p.Names.Any(n=>!valid.Contains(n)))){Preview();throw new InvalidOperationException(UiText.Get("Basic.Retest"));}
            if(!basic && valid.Contains("sap_rfc_call") && (await PrerequisiteService.ValidateSdkAsync(_settings,Token)).State!=CheckState.Ready) throw new InvalidOperationException(UiText.Get("Text207"));
            var applied=await ClientIntegration.ApplyAllAsync(previews,replace.IsChecked==true,Token);
            foreach(var item in applied.Where(r=>r.Success))
            {
                var p=previews.Single(p=>p.Client==item.Client);
                var names=_settings.Registrations.Where(r=>r.Client==p.Client&&r.ConfigPath==p.Path).SelectMany(r=>r.ServerNames).Concat(p.Names).Distinct().ToList();
                _settings.Registrations.RemoveAll(r=>r.Client==p.Client&&r.ConfigPath==p.Path);_settings.Registrations.Add(new(p.Client,p.Path,names));
            }
            _settings.CodexEnabled=codex.IsChecked==true;_settings.ClaudeEnabled=desktop.IsChecked==true;_settings.ClaudeCodeEnabled=code.IsChecked==true;
            _settings.OpenCode=new(){Enabled=openCode.IsChecked==true,Scope=openCodeScope.SelectedIndex==0?"global":"project",ConfigPath=Path.IsPathFullyQualified(Read(openCodePath))?Read(openCodePath):"",Version=openCodeVersion.SelectedIndex==0?OpenCodeVersion.V1:OpenCodeVersion.V2};
            // Preserve successful target registrations even when another target failed or cancellation was requested.
            await _store.SaveAsync(_settings,CancellationToken.None);
            var report=string.Join("\n\n",applied.Select(r=>r.Client+" → "+r.Path+"\n"+(r.Success?UiText.Get("Clients.Saved")+" "+string.Join(", ",previews.Single(p=>p.Client==r.Client).Names)+(r.BackupPath==null?"":"\n"+UiText.Get("Clients.Backup",r.BackupPath))+"\n"+UiText.Get("Clients.Restart",r.Client):r.Error+(r.BackupPath==null?"":"\n"+UiText.Get("Clients.Backup",r.BackupPath)))));
            Preview();result.Text=report;SetStatus(UiText.Get(applied.All(r=>r.Success)?"Text217":"Clients.Partial"));
            RefreshBasicSummary();
        },true);save.Name=prefix+"SaveClients";save.IsEnabled=false;
        form.Children.Add(Row(Action(UiText.Get("Text208"),()=>{Preview();return Task.CompletedTask;}),save));form.Children.Add(result);
        form.Children.Add(custom);
        replace.IsCheckedChanged+=(_,_)=>UpdateSave();
        foreach(var box in new[]{codex,desktop,code,openCode})box.IsCheckedChanged+=(_,_)=>Preview();
        foreach(var field in new[]{codexPath,desktopPath,codePath}.OfType<TextBox>())field.TextChanged+=(_,_)=>Preview();
        openCode.IsCheckedChanged+=(_,_)=>openCodeForm.IsVisible=openCode.IsChecked==true;
        openCodeScope.SelectionChanged+=(_,_)=>{if(openCodeScope.SelectedIndex==0)ChooseDirectory(ClientIntegration.OpenCodeDirectory);else{openCodePath.Text="";candidates.IsVisible=false;}Preview();};
        candidates.SelectionChanged+=(_,_)=>{if(candidates.SelectedItem is string path)openCodePath.Text=path;};
        openCodeVersion.SelectionChanged+=(_,_)=>Preview();
        void DetectVersion()
        {
            try{if(File.Exists(Read(openCodePath)) && ClientIntegration.DetectOpenCodeVersion(File.ReadAllText(Read(openCodePath))) is {} version)openCodeVersion.SelectedIndex=version==OpenCodeVersion.V1?0:1;}
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException){ /* Preview reports the error and blocks writes. */ }
        }
        openCodePath.TextChanged+=(_,_)=>{DetectVersion();Preview();};DetectVersion();
        if(basic)_refreshBasicClients=()=>{Preview();_refreshCustomSummary?.Invoke();};
        Preview();return form;
    }
}
