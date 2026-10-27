using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SapMcp.Core;

namespace SapMcp.Desktop;

public partial class MainWindow
{
    private string? _connectionError;
    private bool _connectionsDirty;
    private bool _fillingProfiles;
    private string? _preferredSystemId;
    private Action? _refreshConnectionLanguage;

    private async Task LoadConnectionsAsync(bool repairManaged = false)
    {
        _connectionError = null;
        try
        {
            if (repairManaged && _settings.ConnectionSourceMode == "managed")
                _settings = await new ConnectionManager(_store).SelectSourceAsync(_settings, "managed", ct: Token);
            else if (repairManaged && ConnectionManager.Mode(_settings) == "sapGui" && _settings.Profiles.Count > 0 && ConnectionManager.ExternalPath(_settings).Length > 0)
                _settings = await new ConnectionManager(_store).SelectSourceAsync(_settings, "sapGui", ct: Token);
            _systems = await ConnectionManager.Source(_settings).LoadAsync(_settings, Token);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Xml.XmlException or System.Text.Json.JsonException or ArgumentException)
        {
            _systems = []; _connectionError = UiText.Get("Connections.SourceError", Redaction.Text(ex.Message));
            try { await AuditAsync("connections.source.failed", new { error = ex.GetType().Name }, LogLevel.Warn); } catch { }
        }
    }

    private async Task AuditConnectionChangeAsync(string name, object detail)
    {
        try { await AuditAsync(name, detail); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { SetStatus(UiText.Get("Connections.AuditWarning")); }
    }

    private async Task<bool> DiscardConnectionChangesAsync() => !_connectionsDirty ||
        await ConfirmAsync(UiText.Get("Connections.UnsavedTitle"), UiText.Get("Connections.UnsavedBody"));

    private Control ConnectionSourceCard()
    {
        var form = new StackPanel { Spacing = 10 };
        form.Children.Add(Text(UiText.Get("Connections.SourceTitle"), "h2"));
        form.Children.Add(Text(UiText.Get(SapGuiInstallation.IsDetected() ? "Connections.GuiDetected" : "Connections.GuiNotDetected"), "muted"));
        var picker = new ComboBox { Name = "ConnectionSourcePicker" };
        var initial = _settings.ConnectionSourceMode ?? (_settings.LandscapeFile.Length > 0 || SapGuiInstallation.IsDetected() ? "sapGui" : "managed");
        picker.ItemsSource = new[] { UiText.Get("Connections.GuiMode"), UiText.Get("Connections.ManagedMode") };
        picker.SelectedIndex = initial == "managed" ? 1 : 0;
        _refreshConnectionLanguage = () =>
        {
            var selection = picker.SelectedIndex;
            picker.ItemsSource = new[] { UiText.Get("Connections.GuiMode"), UiText.Get("Connections.ManagedMode") };
            picker.SelectedIndex = selection;
        };
        form.Children.Add(picker);
        var pathPanel = new StackPanel { Spacing = 10 };
        var file = Field(pathPanel, UiText.Get("Text162"), ConnectionManager.ExternalPath(_settings)); file.Name = "ExternalLandscapePath";
        pathPanel.Children.Add(Row(Action(UiText.Get("Text163"), async () =>
        {
            var chosen = await PickFileAsync(UiText.Get("Text164"), "*.xml"); if (chosen != null) file.Text = chosen;
        }), Action(UiText.Get("Connections.ChooseFolder"), async () =>
        {
            var folder = await PickFolderAsync(UiText.Get("Connections.ChooseFolder")); if (folder == null) return;
            var candidates = SapGuiLandscapeSource.FindInFolder(folder);
            if (candidates.Count == 0) throw new InvalidDataException(UiText.Get("Connections.NoXml"));
            var chosen = candidates.Count == 1 ? candidates[0] : await ChooseLandscapeAsync(candidates);
            if (chosen != null) file.Text = chosen;
        })));
        var managedInfo = Text(UiText.Get("Connections.CatalogLocation", ConnectionManager.CatalogPath(_settings)), "muted");
        void ShowDraftMode() { pathPanel.IsVisible = picker.SelectedIndex == 0; managedInfo.IsVisible = picker.SelectedIndex == 1; }
        picker.SelectionChanged += (_, _) => ShowDraftMode(); ShowDraftMode();
        form.Children.Add(pathPanel); form.Children.Add(managedInfo);
        var apply = Action(UiText.Get("Connections.ApplySource"), async () =>
        {
            if (!await DiscardConnectionChangesAsync()) return;
            _settings = await new ConnectionManager(_store).SelectSourceAsync(_settings, picker.SelectedIndex == 1 ? "managed" : "sapGui", Read(file), Token);
            await LoadConnectionsAsync(); ShowPage(3); SetStatus(UiText.Get("Connections.SourceSaved"));
            await AuditConnectionChangeAsync("connections.source.changed", new { mode = _settings.ConnectionSourceMode });
        }, true); apply.Name = "ApplyConnectionSource";
        form.Children.Add(Row(apply, Action(UiText.Get("Connections.Refresh"), async () =>
        {
            if (!await DiscardConnectionChangesAsync()) return;
            await LoadConnectionsAsync(repairManaged: true); ShowPage(3);
            SetStatus(_connectionError ?? UiText.Get("Connections.Refreshed"));
        })));
        form.Children.Add(Text(UiText.Get(ConnectionManager.Mode(_settings) == "sapGui" ? "Connections.ReadOnly" : "Connections.ManagedHint"), "muted"));
        return Card(form);
    }

    private async Task<string?> ChooseLandscapeAsync(IReadOnlyList<string> files)
    {
        var dialog = new Window { Title = UiText.Get("Connections.ChooseXml"), Width = 730, Height = 320, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var list = new ListBox { ItemsSource = files, SelectedIndex = 0 };
        var button = new Button { Content = UiText.Get("Text242") };
        button.Click += (_, _) => dialog.Close(list.SelectedItem as string);
        dialog.Content = new StackPanel { Margin = new(24), Spacing = 18, Children = { list, button } };
        return await dialog.ShowDialog<string?>(this);
    }

    private Control SystemToolbar(ComboBox systems)
    {
        var add = new Button { Name = "AddConnection", Content = UiText.Get("Connections.Add") };
        var edit = new Button { Name = "EditConnection", Content = UiText.Get("Connections.Edit") };
        var remove = new Button { Name = "DeleteConnection", Content = UiText.Get("Connections.Delete") };
        var editable = _settings.ConnectionSourceMode == "managed";
        add.IsEnabled = editable; edit.IsEnabled = remove.IsEnabled = editable && systems.SelectedItem != null;
        systems.SelectionChanged += (_, _) => edit.IsEnabled = remove.IsEnabled = editable && systems.SelectedItem != null;
        add.Click += async (_, _) => { if (_operation == null && await DiscardConnectionChangesAsync()) await EditConnectionAsync(null); };
        edit.Click += async (_, _) => { if (_operation == null && systems.SelectedItem is SapSystem selected && await DiscardConnectionChangesAsync()) await EditConnectionAsync(selected); };
        remove.Click += async (_, _) => await RunAsync(async () =>
        {
            if (systems.SelectedItem is not SapSystem selected || !await DiscardConnectionChangesAsync()) return;
            var affected = _settings.Profiles.Where(p => p.TargetKey.Equals(selected.TargetKey, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (!await ConfirmAsync(UiText.Get("Connections.DeleteTitle"), UiText.Get("Connections.DeleteBody", selected.Name, affected.Length, string.Join("\n", affected.Select(p => p.ToString()))))) return;
            _settings = await new ConnectionManager(_store).DeleteSystemAsync(_settings, selected.Id, Token);
            await LoadConnectionsAsync(); ShowPage(3); SetStatus(UiText.Get("Connections.Deleted"));
            await AuditConnectionChangeAsync("connections.system.deleted", new { profiles = affected.Length });
        });
        return Row(add, edit, remove);
    }

    private async Task EditConnectionAsync(SapSystem? existing)
    {
        var dialog = new Window { Title = UiText.Get(existing == null ? "Connections.Add" : "Connections.Edit"), Width = 600, Height = 640,
            MinWidth = 500, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var form = new StackPanel { Spacing = 12, Margin = new(24) };
        var name = Field(form, UiText.Get("Connections.Name"), existing?.Name ?? ""); name.Name = "ConnectionName";
        var host = Field(form, UiText.Get("Connections.Host"), existing?.Host ?? ""); host.Name = "ConnectionHost";
        var number = Field(form, UiText.Get("Connections.Number"), existing?.Sysnr ?? "00"); number.Name = "ConnectionNumber";
        var sid = Field(form, "SID", existing?.Sid ?? ""); sid.Name = "ConnectionSid";
        var router = Field(form, "SAProuter", existing?.Router ?? ""); router.Name = "ConnectionRouter";
        form.Children.Add(Text(UiText.Get("Connections.AccountHint"), "muted"));
        var error = Text(""); error.Foreground = Brushes.IndianRed;
        var save = new Button { Name = "SaveConnection", Content = UiText.Get("Connections.Save") }; save.Classes.Add("primary");
        var cancel = new Button { Content = UiText.Get("Text243") }; cancel.Click += (_, _) => dialog.Close();
        var saving = false; var closeRequested = false;
        dialog.Closing += (_, e) => { if (saving) { closeRequested = true; _operation?.Cancel(); e.Cancel = true; } };
        save.Click += async (_, _) =>
        {
            if (saving) return; saving = true; save.IsEnabled = false; var success = false;
            await RunAsync(async () =>
            {
                var entry = new ManagedConnection { Id = existing?.Id ?? Guid.NewGuid().ToString("N"), Name = Read(name), Host = Read(host), Sysnr = Read(number), Sid = Read(sid), Router = Read(router) };
                entry.NormalizeAndValidate();
                if (existing != null && !entry.TargetKey.Equals(existing.TargetKey, StringComparison.Ordinal))
                {
                    var affected = _settings.Profiles.Where(p => p.TargetKey.Equals(existing.TargetKey, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (!await ConfirmAsync(UiText.Get("Connections.MoveTitle"), UiText.Get("Connections.MoveBody", existing.TargetKey, entry.TargetKey, affected.Length, string.Join("\n", affected.Select(p => p.ToString()))), dialog)) return;
                }
                _settings = await new ConnectionManager(_store).SaveSystemAsync(_settings, entry, Token);
                _preferredSystemId = entry.Id; await LoadConnectionsAsync(); ShowPage(3);
                SetStatus(UiText.Get("Connections.Saved")); success = true;
                await AuditConnectionChangeAsync("connections.system.saved", new { created = existing == null });
            });
            saving = false; save.IsEnabled = true;
            if (success || closeRequested) dialog.Close(); else error.Text = this.FindControl<TextBlock>("StatusText")!.Text;
        };
        form.Children.Add(error); form.Children.Add(Row(cancel, save)); dialog.Content = new ScrollViewer { Content = form };
        await dialog.ShowDialog(this);
    }
}
