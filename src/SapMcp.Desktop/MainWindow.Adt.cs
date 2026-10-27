using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input.Platform;
using Avalonia.LogicalTree;
using SapMcp.Core;

namespace SapMcp.Desktop;

public partial class MainWindow
{
    private Button DialogAction(string caption, Func<Task> action, string? name = null)
    {
        var button = new Button { Content = caption, Name = name };
        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            try { await action(); }
            catch (Exception ex)
            {
                var message=UiText.Get("OperationFailed", Redaction.Text(ex.Message)); SetStatus(message);
                if (TopLevel.GetTopLevel(button) is Window owner && owner.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(t=>t.Name=="AdtDialogError") is { } error) error.Text=message;
            }
            finally { button.IsEnabled = true; }
        };
        return button;
    }

    private async Task ShowAdtUrlHelpAsync()
    {
        var text = UiText.Get("Adt.UrlHelpBody");
        var copy = DialogAction(UiText.Get("Adt.CopyBasisRequest"), async () => { if (Clipboard != null) await Clipboard.SetTextAsync(UiText.Get("Adt.BasisRequest")); });
        var box = new TextBox { Name = "AdtUrlHelpText", Text = text + "\n\n" + UiText.Get("Adt.BasisRequest"), IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 350 };
        var dialog = new Window { Name = "AdtUrlHelpDialog", Title = UiText.Get("Adt.UrlHelp"), Width = 760, Height = 650, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var close = new Button { Content = UiText.Get("Text243") }; close.Click += (_, _) => dialog.Close();
        dialog.Content = new ScrollViewer { Content = new StackPanel { Margin = new(22), Spacing = 12, Children = { box, Row(copy, close) } } };
        await dialog.ShowDialog(this);
    }

    private ConnectionProfile SavedAdtProfile(ConnectionProfile? profile)
    {
        if (profile == null || _connectionsDirty) throw new InvalidOperationException(UiText.Get("Adt.SaveFirst"));
        return _settings.Profiles.Single(p => p.Id == profile.Id);
    }

    private async Task<JsonElement> AdtOperationAsync(ConnectionProfile profile, string tool)
    {
        await PersistAsync();
        await using var session = await McpSession.StartAsync(_settings, _store.FilePath, "adt", Token, profile.Id);
        var tools = await session.RequestAsync("tools/list", ct: Token);
        if (!tools.GetProperty("tools").EnumerateArray().Any(t => t.GetProperty("name").GetString() == tool)) throw new InvalidOperationException(UiText.Get("Adt.PackageUpgrade"));
        var response = await session.RequestAsync("tools/call", new { name = tool, arguments = new { target_key = profile.TargetKey } }, Token);
        if (response.TryGetProperty("structuredContent", out var data)) return data.Clone();
        if (response.TryGetProperty("isError", out var error) && error.ValueKind == JsonValueKind.True) throw new InvalidOperationException(UiText.Get("Adt.UrlAdvice"));
        throw new InvalidDataException(UiText.Get("Adt.PackageUpgrade"));
    }

    private async Task TestAdtProfileAsync(ConnectionProfile? selected)
    {
        var profile = SavedAdtProfile(selected);
        var report = await AdtOperationAsync(profile, "diagnose_connection");
        var form = new StackPanel { Margin = new(22), Spacing = 12 };
        var successful = report.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True;
        form.Children.Add(Notice(UiText.Get(successful ? "Adt.Verified" : "Adt.NotVerified"), !successful));
        if (report.TryGetProperty("stages", out var stages))
            foreach (var stage in stages.EnumerateArray())
            {
                var name = stage.GetProperty("stage").GetString() ?? ""; var code = stage.GetProperty("code").GetString() ?? "";
                if (stage.TryGetProperty("httpStatus", out var status) && status.TryGetInt32(out var number) && number is >=100 and <=599) code += " · HTTP " + number;
                var state=stage.GetProperty("status").GetString() ?? "unverified";
                form.Children.Add(Text(UiText.Get("Adt.StageLine", AdtStage(name), UiText.Get("Adt.Status." + (state is "passed" or "failed" or "skipped" or "pending" ? state : "unverified")), code)));
                if (stage.GetProperty("status").GetString() is "failed" or "unverified") form.Children.Add(Text(AdtProfiles.Advice(code), "muted"));
            }
        foreach (var warning in report.GetProperty("warnings").EnumerateArray())
            form.Children.Add(Notice(UiText.Get(warning.GetString() == "HTTP_CREDENTIALS_UNENCRYPTED" ? "Adt.HttpWarning" : "Adt.TlsWarning"), true));
        var export = DialogAction(UiText.Get("Adt.ExportSupport"), async () =>
        {
            var file = await PickSaveAsync("adt-support-redacted.json");
            if (file != null) await AtomicFile.WriteAsync(file, AdtProfiles.SupportReport(profile, report), privateData: true);
        }, "ExportAdtSupport");
        var dialog = new Window { Name = "AdtDiagnosticsDialog", Title = UiText.Get("Adt.Diagnostics"), Width = 780, Height = 700, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var close = new Button { Content = UiText.Get("Text243") }; close.Click += (_, _) => dialog.Close();
        form.Children.Add(Row(export, close)); dialog.Content = new ScrollViewer { Content = form };
        form.Children.Add(new TextBlock { Name="AdtDialogError", Foreground=Brushes.IndianRed, TextWrapping=TextWrapping.Wrap });
        await AuditAsync("adt.diagnosed", new { success = successful });
        await dialog.ShowDialog(this);
    }

    private static string AdtStage(string stage) => UiText.Get(stage switch { "configuration" => "Adt.StageConfig", "dns" => "Adt.StageDns", "tcp" => "Adt.StageTcp", "tls" => "Adt.StageTls", "authentication" => "Adt.StageAuth", "discovery" => "Adt.StageDiscovery", _ => "Adt.StageRead" });

    private async Task<ConnectionProfile?> EditAdtTrustAsync(ConnectionProfile? selected)
    {
        var original = SavedAdtProfile(selected);
        var profile = JsonSerializer.Deserialize<ConnectionProfile>(JsonSerializer.Serialize(original, JsonDefaults.Options), JsonDefaults.Options)!;
        ConnectionProfile? saved = null;
        var form = new StackPanel { Margin = new(22), Spacing = 12 };
        form.Children.Add(Notice(UiText.Get("Adt.TrustScope")));
        form.Children.Add(new TextBlock { Name="AdtDialogError", Foreground=Brushes.IndianRed, TextWrapping=TextWrapping.Wrap });
        var list = new ListBox { Name = "AdtTrustedCertificates", MinHeight = 120 };
        void Refresh() => list.ItemsSource = profile.AdtCertificates.Select(c => c.Sha256).ToArray();
        Refresh(); form.Children.Add(list);
        var details = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 160, Name = "AdtCertificateDetails" }; form.Children.Add(details);
        var dialog = new Window { Name = "AdtTrustDialog", Title = UiText.Get("Adt.Certificates"), Width = 800, Height = 700, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        form.Children.Add(Row(
            DialogAction(UiText.Get("Adt.ImportCertificate"), async () =>
            {
                var file = await PickFileAsync(UiText.Get("Adt.ImportCertificate"), "*"); if (file == null) return;
                var certificates = await AdtProfiles.ReadCertificatesAsync(file); details.Text = string.Join("\n\n", certificates.Select(c => c.Display));
                if (!await ConfirmAsync(UiText.Get("Adt.ConfirmTrust"), UiText.Get("Adt.ConfirmTrustBody") + "\n\n" + details.Text, dialog)) return;
                var stored = await AdtProfiles.StoreCertificatesAsync(_settings, profile, certificates);
                profile.AdtCertificates = profile.AdtCertificates.Concat(stored).DistinctBy(c => c.Sha256).ToList(); Refresh();
            }, "ImportAdtCertificate"),
            DialogAction(UiText.Get("Adt.ViewCertificate"), async () =>
            {
                if (list.SelectedIndex >= 0) details.Text = string.Join("\n\n", (await AdtProfiles.ReadCertificatesAsync(profile.AdtCertificates[list.SelectedIndex].File)).Select(c => c.Display));
            }),
            DialogAction(UiText.Get("Adt.RemoveCertificate"), () => { if (list.SelectedIndex >= 0) { profile.AdtCertificates.RemoveAt(list.SelectedIndex); Refresh(); details.Text = ""; } return Task.CompletedTask; }),
            DialogAction(UiText.Get("Adt.InspectCertificate"), async () => { var data = await AdtOperationAsync(original, "inspect_tls_certificate"); details.Text = UiText.Get("Adt.InspectionWarning") + "\n\n" + JsonSerializer.Serialize(data, JsonDefaults.Options); })));
        var cancel = new Button { Content = UiText.Get("Text243") }; cancel.Click += (_, _) => dialog.Close();
        var save = DialogAction(UiText.Get("Text185"), async () =>
        {
            var previous = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(_settings, JsonDefaults.Options), JsonDefaults.Options)!;
            try { await new ProfileService(_secrets).SaveAsync(_settings, profile, null, false); await PersistAsync(); }
            catch { _settings = previous; await ProfileService.MaterializeAsync(_settings); throw; }
            saved = profile; dialog.Close(); SetStatus(UiText.Get("Adt.TrustSaved"));
        }, "SaveAdtTrust");
        form.Children.Add(Row(cancel, save)); dialog.Content = new ScrollViewer { Content = form };
        await dialog.ShowDialog(this); return saved;
    }

    private async Task DiscoverAdtAsync(ConnectionProfile? selected, TextBox url)
    {
        var profile = SavedAdtProfile(selected);
        if (!profile.AdtAutoDiscovery) throw new InvalidOperationException(UiText.Get("Adt.EnableDiscovery"));
        var report = await AdtOperationAsync(profile, "discover_adt_endpoints");
        var candidates = report.GetProperty("candidates").EnumerateArray().Select(c => c.Clone()).ToArray();
        var list = new ListBox { ItemsSource = candidates.Select(c => c.GetProperty("url").GetString() + " · " + c.GetProperty("code").GetString()).ToArray(), MinHeight = 180 };
        var dialog = new Window { Title = UiText.Get("Adt.Discover"), Width = 760, Height = 500, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var select = new Button { Content = UiText.Get("Adt.UseCandidate") };
        select.Click += (_, _) => { if (list.SelectedIndex >= 0) { url.Text = candidates[list.SelectedIndex].GetProperty("url").GetString(); _connectionsDirty = true; dialog.Close(); } };
        dialog.Content = new ScrollViewer { Content = new StackPanel { Margin = new(22), Spacing = 12, Children = { Notice(UiText.Get("Adt.CandidateWarning")), list, select } } };
        await dialog.ShowDialog(this);
    }
}
