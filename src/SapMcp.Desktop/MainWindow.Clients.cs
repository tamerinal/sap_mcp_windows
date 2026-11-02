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
        form.Children.Add(Row(codex,desktop,code));
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
        string[] Components()
        {
            if (basic)
            {
                var p=_settings.Profiles.SingleOrDefault(p=>p.Id==_basicDraft.ProfileId);
                if (p==null || !_basicPrepared || BasicFields()!=_basicLoadedFields) return [];
                return BasicPolicy.Eligible(_settings,p).Where(c=>c=="adt" || _basicRfcReady).ToArray();
            }
            return new[]{"adt","rfc"}.Where(c=>_settings.Component(c).Installed && _settings.Component(c).Selected).ToArray();
        }
        void UpdateSave() { if(save!=null)save.IsEnabled=previews.Count>0 && (previews.All(p=>p.Conflicts.Count==0)||replace.IsChecked==true); }
        void Preview()
        {
            previews=[];replace.IsChecked=false;replace.IsVisible=false;UpdateSave();details.Text="";result.Text="";
            try
            {
                var components=Components();
                if (components.Length==0) { summary.Text=UiText.Get(basic?"Basic.Retest":"Text207"); return; }
                var next=new List<ClientPreview>();
                if(codex.IsChecked==true)next.Add(ClientIntegration.Preview("Codex",codexPath==null?ClientIntegration.CodexPath:Read(codexPath),_settings,_store.FilePath,components));
                if(desktop.IsChecked==true)next.Add(ClientIntegration.Preview("Claude Desktop",desktopPath==null?ClientIntegration.ClaudePath:Read(desktopPath),_settings,_store.FilePath,components));
                if(code.IsChecked==true)next.Add(ClientIntegration.Preview("Claude Code",codePath==null?ClientIntegration.ClaudeCodePath:Read(codePath),_settings,_store.FilePath,components));
                previews=next;
                summary.Text=previews.Count==0?UiText.Get("Text209"):string.Join("\n\n",previews.Select(ClientIntegration.Describe));
                details.Text=string.Join("\n\n",previews.Select(p=>p.Client+" → "+p.Path+"\n"+ClientIntegration.Details(p)));
                replace.IsVisible=previews.Any(p=>p.Conflicts.Count>0);UpdateSave();
            }
            catch(Exception ex) when(ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException or ArgumentException)
            { previews=[];summary.Text=UiText.Get("OperationFailed",Redaction.Text(ex.Message));UpdateSave(); }
        }
        save=Action(UiText.Get("Basic.ApplyClientSettings"),async()=>
        {
            var valid=Components().Select(ClientIntegration.ServerName).ToHashSet();
            if(previews.Count==0 || previews.Any(p=>p.Names.Any(n=>!valid.Contains(n)))){Preview();throw new InvalidOperationException(UiText.Get("Basic.Retest"));}
            if(!basic && valid.Contains("sap_rfc_call") && (await PrerequisiteService.ValidateSdkAsync(_settings,Token)).State!=CheckState.Ready) throw new InvalidOperationException(UiText.Get("Text207"));
            var applied=await ClientIntegration.ApplyAllAsync(previews,replace.IsChecked==true,Token);
            foreach(var item in applied.Where(r=>r.Success))
            {
                var p=previews.Single(p=>p.Client==item.Client);
                var names=_settings.Registrations.Where(r=>r.Client==p.Client&&r.ConfigPath==p.Path).SelectMany(r=>r.ServerNames).Concat(p.Names).Distinct().ToList();
                _settings.Registrations.RemoveAll(r=>r.Client==p.Client&&r.ConfigPath==p.Path);_settings.Registrations.Add(new(p.Client,p.Path,names));
            }
            _settings.CodexEnabled=codex.IsChecked==true;_settings.ClaudeEnabled=desktop.IsChecked==true;_settings.ClaudeCodeEnabled=code.IsChecked==true;
            // Preserve successful target registrations even when another target failed or cancellation was requested.
            await _store.SaveAsync(_settings,CancellationToken.None);
            var report=string.Join("\n\n",applied.Select(r=>r.Client+" → "+r.Path+"\n"+(r.Success?UiText.Get("Clients.Saved")+" "+string.Join(", ",previews.Single(p=>p.Client==r.Client).Names)+(r.BackupPath==null?"":"\n"+UiText.Get("Clients.Backup",r.BackupPath))+"\n"+UiText.Get("Clients.Restart",r.Client):r.Error+(r.BackupPath==null?"":"\n"+UiText.Get("Clients.Backup",r.BackupPath)))));
            Preview();result.Text=report;SetStatus(UiText.Get(applied.All(r=>r.Success)?"Text217":"Clients.Partial"));
            RefreshBasicSummary();
        },true);save.Name=prefix+"SaveClients";save.IsEnabled=false;
        form.Children.Add(Row(Action(UiText.Get("Text208"),()=>{Preview();return Task.CompletedTask;}),save));form.Children.Add(result);
        replace.IsCheckedChanged+=(_,_)=>UpdateSave();
        foreach(var box in new[]{codex,desktop,code})box.IsCheckedChanged+=(_,_)=>Preview();
        foreach(var field in new[]{codexPath,desktopPath,codePath}.OfType<TextBox>())field.TextChanged+=(_,_)=>Preview();
        if(basic)_refreshBasicClients=Preview;
        Preview();return form;
    }
}
