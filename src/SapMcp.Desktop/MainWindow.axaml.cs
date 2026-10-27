using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using SapMcp.Core;

namespace SapMcp.Desktop;

public partial class MainWindow : Window
{
    private readonly SettingsStore _store;
    private readonly PackageService _packages = new(Path.Combine(AppContext.BaseDirectory, "payload"));
    private readonly DpapiSecretStore _secrets = new();
    private AppSettings _settings = AppSettings.CreateDefault();
    private IReadOnlyList<SapSystem> _systems = [];
    private List<Requirement> _checks = [];
    private CancellationTokenSource? _operation;
    private bool _loaded;
    private int _page;
    private readonly string[] _titles = [UiText.Get("Text088"), UiText.Get("Text089"), UiText.Get("Text090"), UiText.Get("Text091"), UiText.Get("Text092"), UiText.Get("Text093")];
    private readonly string[] _subtitles = [UiText.Get("Text094"), UiText.Get("Text095"), UiText.Get("Text096"), UiText.Get("Text097"), UiText.Get("Text098"), UiText.Get("Text099")];
    private StackPanel Body => this.FindControl<StackPanel>("PageBody")!;
    private CancellationToken Token => _operation?.Token ?? CancellationToken.None;

    public MainWindow() : this(null) { }
    public MainWindow(string? configPath)
    {
        AvaloniaXamlLoader.Load(this);
        _store = new(configPath ?? SettingsStore.DefaultPath);
        var nav = this.FindControl<StackPanel>("Navigation")!;
        var labels = new[] { UiText.Get("Text100"), UiText.Get("Text101"), UiText.Get("Text102"), UiText.Get("Text103"), UiText.Get("Text104"), UiText.Get("Text105") };
        for (var i = 0; i < labels.Length; i++) { var index = i; var b = new Button { Content = labels[i], HorizontalAlignment = HorizontalAlignment.Stretch }; b.Classes.Add("nav"); b.Click += (_, _) => { if (_operation == null) ShowPage(index); }; nav.Children.Add(b); }
        var theme = this.FindControl<ComboBox>("ThemePicker")!;
        theme.ItemsSource = new[] { UiText.Get("Text106"), UiText.Get("Text107"), UiText.Get("Text108") }; theme.SelectedIndex = 0;
        theme.SelectionChanged += (_, _) => Application.Current!.RequestedThemeVariant = theme.SelectedIndex switch { 1 => ThemeVariant.Light, 2 => ThemeVariant.Dark, _ => ThemeVariant.Default };
        var languages = this.FindControl<ComboBox>("LanguagePicker")!;
        languages.ItemsSource = new[] { "English", "Türkçe" };
        languages.SelectedIndex = UiText.Language == "tr" ? 1 : 0;
        languages.SelectionChanged += async (_, _) =>
        {
            if (!_loaded || _operation != null) return;
            var language = languages.SelectedIndex == 1 ? "tr" : "en";
            await RunAsync(async () => { ApplyLanguage(language); _settings.UiLanguage = language; await PersistAsync(); });
        };
        ApplyLanguage(UiText.Language);
        this.FindControl<Button>("CancelAction")!.Click += (_, _) => _operation?.Cancel();
        this.FindControl<Button>("HelpButton")!.Click += async (_, _) =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, "docs", UiText.Language == "tr" ? "USER-GUIDE.tr.md" : "USER-GUIDE.md");
            var guide = File.Exists(file) ? await File.ReadAllTextAsync(file) : UiText.Get("Text265");
            var dialog = new Window { Title = UiText.Get("Text109"), Width = 760, Height = 650, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBox { Text = guide, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new(20) } };
            await dialog.ShowDialog(this);
        };
        Closing += (_, e) => { if (_operation != null) { _operation.Cancel(); e.Cancel = true; SetStatus(UiText.Get("Text110")); } };
        Opened += async (_, _) => await RunAsync(async () =>
        {
            _settings = await _store.LoadAsync(Token);
            ApplyLanguage(UiText.ResolveLanguage(_settings.UiLanguage));
            _settings.HostExecutable = Path.Combine(AppContext.BaseDirectory, "SapMcpHost.exe");
            var setupSelection = Path.Combine(AppContext.BaseDirectory, "setup-selection.json");
            if (File.Exists(setupSelection))
            {
                using var setup = JsonDocument.Parse(await File.ReadAllTextAsync(setupSelection, Token));
                var root = setup.RootElement;
                var setupId = root.GetProperty("id").GetString() ?? "";
                if (setupId != _settings.AppliedSetupId)
                {
                    _settings.Rfc.Selected = root.GetProperty("rfc").GetBoolean(); _settings.Adt.Selected = root.GetProperty("adt").GetBoolean();
                    _settings.AppliedSetupId = setupId; await PersistAsync();
                }
            }
            var arguments = Environment.GetCommandLineArgs();
            if (arguments.Contains("--rfc=0")) _settings.Rfc.Selected = false;
            if (arguments.Contains("--rfc=1")) _settings.Rfc.Selected = true;
            if (arguments.Contains("--adt=0")) _settings.Adt.Selected = false;
            if (arguments.Contains("--adt=1")) _settings.Adt.Selected = true;
            if (_settings.ConnectionSourceMode == null && string.IsNullOrEmpty(_settings.LandscapeFile)) _settings.ExternalLandscapeFile ??= LandscapeService.Discover().FirstOrDefault();
            await LoadConnectionsAsync(repairManaged: true);
            _loaded = true; ShowPage(0); SetStatus(UiText.Get("Text111"));
        });
    }

    private void ApplyLanguage(string language)
    {
        UiText.SetLanguage(language);
        Title = UiText.Get("Text253");
        this.FindControl<TextBlock>("BrandCaption")!.Text = UiText.Get("Text254");
        this.FindControl<TextBlock>("PrivacyCaption")!.Text = UiText.Get("Text255");
        this.FindControl<TextBlock>("PrivacyDescription")!.Text = UiText.Get("Text256");
        this.FindControl<TextBlock>("Eyebrow")!.Text = UiText.Get("Text258");
        this.FindControl<Button>("HelpButton")!.Content = UiText.Get("Text257");
        this.FindControl<Button>("CancelAction")!.Content = UiText.Get("Text260");
        if (string.IsNullOrEmpty(this.FindControl<TextBlock>("StatusText")!.Text)) SetStatus(UiText.Get("Text259"));
        // Do not recreate the page or touch TextBox.Text, selectors, or check states:
        // they may contain unsaved profile data and a password.
        foreach (var control in this.GetLogicalDescendants().OfType<Control>())
        {
            if (control is TextBlock text && text.Text != null) text.Text = UiText.Redisplay(text.Text);
            if (control is ContentControl content && content is not ComboBoxItem && content.Content is string caption)
                content.Content = UiText.Redisplay(caption);
            if (control is TextBox field)
            {
                var name = Avalonia.Automation.AutomationProperties.GetName(field);
                if (name != null) Avalonia.Automation.AutomationProperties.SetName(field, UiText.Redisplay(name));
            }
        }
        var theme = this.FindControl<ComboBox>("ThemePicker")!;
        var selectedTheme = theme.SelectedIndex;
        theme.ItemsSource = new[] { UiText.Get("Text106"), UiText.Get("Text107"), UiText.Get("Text108") };
        theme.SelectedIndex = selectedTheme;
        _refreshConnectionLanguage?.Invoke();
        this.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex = language == "tr" ? 1 : 0;
    }
    private void SetStatus(string message) => this.FindControl<TextBlock>("StatusText")!.Text = UiText.Redisplay(message);
    private async Task RunAsync(Func<Task> action)
    {
        if (_operation != null) return;
        _operation = new();
        this.FindControl<ProgressBar>("BusyIndicator")!.IsVisible = true;
        this.FindControl<Button>("CancelAction")!.IsVisible = true;
        Body.IsEnabled = false;
        this.FindControl<ComboBox>("LanguagePicker")!.IsEnabled = false;
        try { await action(); }
        catch (OperationCanceledException) { SetStatus(UiText.Get("Text112")); }
        catch (Exception ex)
        {
            SetStatus(UiText.Get("OperationFailed", Redaction.Text(ex.Message)));
            try { await AuditAsync("operation.failed", new { message = ex.GetType().Name }, LogLevel.Error); } catch { }
            if (!_loaded) { Body.Children.Clear(); Body.Children.Add(Notice(UiText.Get("Text114"), true)); }
        }
        finally { _operation.Dispose(); _operation = null; Body.IsEnabled = true; this.FindControl<ComboBox>("LanguagePicker")!.IsEnabled = true; this.FindControl<ProgressBar>("BusyIndicator")!.IsVisible = false; this.FindControl<Button>("CancelAction")!.IsVisible = false; }
    }

    private Task AuditAsync(string name, object? detail = null, LogLevel level = LogLevel.Info) => new RollingLog(Path.Combine(_settings.DataDirectory, "logs", "manager")).WriteAsync(level, name, detail);
    private async Task PersistAsync() { await _store.SaveAsync(_settings, Token); }
    private IProgress<string> Progress => new Progress<string>(SetStatus);
    private static TextBlock Text(string text, string? css = null) { var t = new TextBlock { Text = UiText.Redisplay(text), TextWrapping = TextWrapping.Wrap }; if (css != null) t.Classes.Add(css); return t; }
    private static StackPanel Stack(params Control[] controls) { var s = new StackPanel { Spacing = 12 }; foreach (var c in controls) s.Children.Add(c); return s; }
    private static WrapPanel Row(params Control[] controls) { var s = new WrapPanel { Orientation = Orientation.Horizontal }; foreach (var c in controls) { c.Margin = new(0, 0, 10, 8); s.Children.Add(c); } return s; }
    private static Border Card(Control content) => new() { BorderBrush = new SolidColorBrush(Color.Parse("#35808080")), BorderThickness = new(1), CornerRadius = new(12), Padding = new(22), Child = content };
    private static Border Notice(string text, bool error = false) => new() { Background = new SolidColorBrush(Color.Parse(error ? "#18B23B3B" : "#18147D70")), CornerRadius = new(8), Padding = new(15), Child = Text(text) };
    private Button Action(string caption, Func<Task> action, bool primary = false)
    {
        var b = new Button { Content = caption }; if (primary) b.Classes.Add("primary");
        b.Click += async (_, _) => await RunAsync(action); return b;
    }
    private static TextBox Field(StackPanel parent, string label, string value, bool password = false)
    {
        parent.Children.Add(Text(label, "muted")); var field = new TextBox { Text = value, PasswordChar = password ? '●' : '\0' }; Avalonia.Automation.AutomationProperties.SetName(field, label); parent.Children.Add(field); return field;
    }
    private static string Read(TextBox box) => box.Text?.Trim() ?? "";

    private void ShowPage(int index)
    {
        _refreshConnectionLanguage = null;
        _page = index; this.FindControl<TextBlock>("PageTitle")!.Text = UiText.Redisplay(_titles[index]); this.FindControl<TextBlock>("PageSubtitle")!.Text = UiText.Redisplay(_subtitles[index]);
        var nav = this.FindControl<StackPanel>("Navigation")!;
        for (var i = 0; i < nav.Children.Count; i++) { nav.Children[i].Classes.Set("active", i == index); }
        Body.Children.Clear();
        switch (index) { case 0: Overview(); break; case 1: Requirements(); break; case 2: Sdk(); break; case 3: Connections(); break; case 4: Clients(); break; case 5: Settings(); break; }
    }

    private void Overview()
    {
        Body.Children.Add(Notice(UiText.Get("Text115")));
        var cards = new Grid { ColumnDefinitions = new("*,*"), ColumnSpacing = 18 };
        foreach (var (name, column, title, description) in new[] { ("rfc", 0, "SAP RFC", UiText.Get("Text116")), ("adt", 1, "SAP ADT", UiText.Get("Text117")) })
        {
            var component = _settings.Component(name);
            var check = new CheckBox { Content = UiText.Get("Text118"), IsChecked = component.Selected };
            check.IsCheckedChanged += (_, _) => component.Selected = check.IsChecked == true;
            var state = component.Installed ? UiText.Get("InstalledVersion", component.Version) : UiText.Get("Text120");
            var content = Stack(Text(name == "rfc" ? "01 / RFC" : "02 / ADT", "muted"), Text(title, "h1"), Text(description), Text(state, "muted"), check);
            if (component.Installed) content.Children.Add(Action(UiText.Get("Text121"), async () =>
            {
                if (!await ConfirmAsync(UiText.Get("RemoveComponent", title), UiText.Get("Text123"))) return;
                await DeploymentService.RemoveAsync(_settings, name, Token); await PersistAsync(); await AuditAsync("component.removed", new { component = name }); ShowPage(0); SetStatus(UiText.Get("ComponentRemoved", title));
            }));
            var card = Card(content); Grid.SetColumn(card, column); cards.Children.Add(card);
        }
        Body.Children.Add(cards);
        Body.Children.Add(Row(Action(UiText.Get("Text125"), async () =>
        {
            if (!_settings.Rfc.Selected && !_settings.Adt.Selected) throw new InvalidOperationException(UiText.Get("Text126"));
            await PersistAsync(); await RefreshChecksAsync(); ShowPage(1);
        }, true), Action(UiText.Get("Text127"), VerifyLocalAsync)));
        Body.Children.Add(Card(Stack(Text(UiText.Get("Text128"), "h2"), Text(UiText.Get("Text129")),
            Text(UiText.Get("Text130"), "muted"))));
    }

    private async Task RefreshChecksAsync() { SetStatus(UiText.Get("Text131")); _checks = await new PrerequisiteService(_packages).CheckAsync(_settings, Token); SetStatus(UiText.Get("ChecksComplete", _checks.Count(c => c.State == CheckState.Ready), _checks.Count)); }

    private void Requirements()
    {
        Body.Children.Add(Row(Action(UiText.Get("Text134"), async () => { await RefreshChecksAsync(); ShowPage(1); }), Action(UiText.Get("Text261"), RequiredApplicationsAsync, true)));
        if (_checks.Count == 0) Body.Children.Add(Notice(UiText.Get("Text135")));
        foreach (var r in _checks)
        {
            var state = r.State switch { CheckState.Ready => UiText.Get("Text136"), CheckState.Waiting => UiText.Get("Text137"), CheckState.Warning => UiText.Get("Text138"), _ => UiText.Get("Text139") };
            Body.Children.Add(Card(Stack(Row(Text(r.Name, "h2"), Text(state, "muted")), Text(r.Version, "muted"), Text(r.Detail))));
        }
        Body.Children.Add(Action(UiText.Get("Text140"), async () =>
        {
            var installed = await new DeploymentService(_packages, new(_packages)).InstallSelectedAsync(_settings, Progress, Token);
            await PersistAsync(); await AuditAsync("components.installed", new { components = installed });
            SetStatus(UiText.Get("ComponentsInstalled", string.Join(", ", installed).ToUpperInvariant())); ShowPage(_settings.Rfc.Selected ? 2 : 3);
        }, true));
    }

    private async Task RequiredApplicationsAsync()
    {
        await RefreshChecksAsync();
        var win = new Window { Title = UiText.Get("Text261"), Width = 780, Height = 650, MinWidth = 600, MinHeight = 450, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var entries = new Dictionary<string, CheckBox>(); var buttons = new Dictionary<string, Button>(); var list = new StackPanel { Spacing = 14, Margin = new(24) };
        list.Children.Add(Text(UiText.Get("Text143"), "h1")); list.Children.Add(Text(UiText.Get("Text144"), "muted"));
        var status = Text(""); var cancel = new CancellationTokenSource(); var busy = false;
        win.Closing += (_, e) => { if (busy) { cancel.Cancel(); e.Cancel = true; } };
        async Task Install(IEnumerable<string> ids)
        {
            if (busy) return; busy = true; list.IsEnabled = false;
            try { await new PrerequisiteService(_packages).InstallAsync(_settings, ids, new Progress<string>(s => status.Text = s), cancel.Token); await RefreshChecksAsync(); await PersistAsync(); await AuditAsync("requirements.installed", new { ids }); status.Text = UiText.Get("Text145"); }
            catch (OperationCanceledException) { status.Text = UiText.Get("Text146"); }
            catch (Exception ex) { status.Text = Redaction.Text(ex.Message); }
            finally
            {
                try { await RefreshChecksAsync(); foreach (var r in _checks.Where(r => entries.ContainsKey(r.Id))) { var ready = r.State == CheckState.Ready; entries[r.Id].IsChecked = !ready; entries[r.Id].IsEnabled = !ready; buttons[r.Id].IsEnabled = !ready; buttons[r.Id].Content = ready ? UiText.Get("Text147") : UiText.Get("Text148"); } } catch { }
                busy = false; list.IsEnabled = true; if (cancel.IsCancellationRequested) win.Close();
            }
        }
        foreach (var r in _checks.Where(r => r.Installable))
        {
            var check = new CheckBox { Content = $"{r.Name} · {r.Version}", IsChecked = r.State != CheckState.Ready, IsEnabled = r.State != CheckState.Ready };
            entries[r.Id] = check;
            var one = new Button { Content = r.State == CheckState.Ready ? UiText.Get("Text149") : UiText.Get("Text263"), IsEnabled = r.State != CheckState.Ready };
            buttons[r.Id] = one;
            one.Click += async (_, _) => await Install([r.Id]);
            list.Children.Add(Card(Stack(check, Text(r.Detail, "muted"), one)));
        }
        if (entries.Count == 0) list.Children.Add(Notice(UiText.Get("Text150")));
        var install = new Button { Content = UiText.Get("Text262") }; install.Classes.Add("primary");
        install.Click += async (_, _) => { var ids = entries.Where(e => e.Value.IsChecked == true && e.Value.IsEnabled).Select(e => e.Key).ToArray(); if (ids.Length == 0) { status.Text = UiText.Get("Text151"); return; } await Install(ids); };
        list.Children.Add(install); list.Children.Add(status); win.Content = new ScrollViewer { Content = list };
        await win.ShowDialog(this); cancel.Dispose(); ShowPage(1);
    }

    private void Sdk()
    {
        Body.Children.Add(Card(Stack(Text(UiText.Get("Text152"), "h2"),
            Text(UiText.Get("Text264")),
            Action(UiText.Get("Text153"), () => { OpenUrl("https://support.sap.com/en/product/connectors/nwrfcsdk.html"); return Task.CompletedTask; }))));
        var form = new StackPanel { Spacing = 10 };
        var path = Field(form, UiText.Get("Text154"), _settings.SdkDirectory);
        form.Children.Add(Row(Action(UiText.Get("Text155"), async () => { var selected = await PickFolderAsync(UiText.Get("Text156")); if (selected != null) path.Text = selected; }),
            Action(UiText.Get("Text157"), async () =>
            {
                _settings.SdkDirectory = Read(path); _settings.Validate();
                var result = await PrerequisiteService.ValidateSdkAsync(_settings, Token);
                await PersistAsync(); await AuditAsync("sdk.checked", new { state = result.State.ToString() });
                SetStatus(result.Detail); ShowPage(2);
            }, true)));
        form.Children.Add(Text(UiText.Get("Text158"), "muted"));
        Body.Children.Add(Card(form));
        Body.Children.Add(Notice(string.IsNullOrWhiteSpace(_settings.SdkDirectory) ? UiText.Get("Text159") : UiText.Get("Text160")));
        Body.Children.Add(Action(UiText.Get("Text161"), () => { ShowPage(3); return Task.CompletedTask; }));
    }

    private void Connections()
    {
        _connectionsDirty = false;
        Body.Children.Add(ConnectionSourceCard());
        if (_connectionError != null) Body.Children.Add(Notice(_connectionError, true));
        var systemPicker = new ComboBox { Name = "ConnectionSystems", ItemsSource = _systems };
        systemPicker.SelectedItem = _systems.FirstOrDefault(s => s.Id == _preferredSystemId) ?? _systems.FirstOrDefault();
        Body.Children.Add(Text(UiText.Get("Text172"), "muted")); Body.Children.Add(systemPicker);
        Body.Children.Add(SystemToolbar(systemPicker));
        Body.Children.Add(Action(UiText.Get("Text167"), async () =>
        {
            if (!await DiscardConnectionChangesAsync()) return;
            var source = await PickFileAsync(UiText.Get("Text168"), "*.json"); if (source == null) return;
            if (_systems.Count == 0) throw new InvalidOperationException(UiText.Get("Text169"));
            var count = await new ProfileService(_secrets).ImportAsync(_settings, source, _systems, Token);
            await PersistAsync(); await AuditAsync("profiles.imported", new { count }); ShowPage(3); SetStatus(UiText.Get("Text170", count));
        }));
        if (_systems.Count == 0) { Body.Children.Add(Notice(UiText.Get("Connections.Empty"))); return; }
        var edit = new StackPanel { Spacing = 10 };
        var profilePicker = new ComboBox(); edit.Children.Add(Text(UiText.Get("Text173"), "muted")); edit.Children.Add(profilePicker);
        var activeLabel = Text("", "muted"); edit.Children.Add(activeLabel);
        var name = Field(edit, UiText.Get("Text174"), ""); var user = Field(edit, UiText.Get("Text175"), "");
        var pair = new Grid { ColumnDefinitions = new("*,*"), ColumnSpacing = 16 }; var l = new StackPanel { Spacing = 8 }; var r = new StackPanel { Spacing = 8 };
        var client = Field(l, "Client", "100"); var language = Field(r, UiText.Get("Text176"), "TR"); pair.Children.Add(l); Grid.SetColumn(r, 1); pair.Children.Add(r); edit.Children.Add(pair);
        var password = Field(edit, UiText.Get("Text177"), "", true);
        var adt = Field(edit, UiText.Get("Text178"), ""); var bw = Field(edit, UiText.Get("Text179"), "");
        var insecure = new CheckBox { Content = UiText.Get("Text180") }; edit.Children.Add(insecure);
        var activate = new CheckBox { Content = UiText.Get("Text181"), IsChecked = true }; edit.Children.Add(activate);
        ConnectionProfile? current = null;
        void Fill(ConnectionProfile? p) { _fillingProfiles = true; current = p; name.Text = p?.Name ?? ""; user.Text = p?.User ?? ""; client.Text = p?.Client ?? "100"; language.Text = p?.Language ?? "TR"; password.Text = ""; adt.Text = p?.AdtBaseUrl ?? ""; bw.Text = p?.BwModelingBaseUrl ?? ""; insecure.IsChecked = p?.AllowInsecureTls ?? false; _fillingProfiles = false; _connectionsDirty = false; }
        void RefreshProfiles()
        {
            if (systemPicker.SelectedItem is not SapSystem system) return;
            var profiles = _settings.Profiles.Where(p => p.TargetKey.Equals(system.TargetKey, StringComparison.OrdinalIgnoreCase)).ToArray(); profilePicker.ItemsSource = profiles;
            var activeId = _settings.ActiveProfiles.GetValueOrDefault(system.TargetKey); profilePicker.SelectedItem = profiles.FirstOrDefault(p => p.Id == activeId) ?? profiles.FirstOrDefault();
            activeLabel.Text = profiles.FirstOrDefault(p => p.Id == activeId) is { } active ? UiText.Get("ActiveProfile", active.ToString()) : UiText.Get("ActiveProfileNone"); Fill(profilePicker.SelectedItem as ConnectionProfile);
        }
        systemPicker.SelectionChanged += (_, _) => RefreshProfiles(); profilePicker.SelectionChanged += (_, _) => Fill(profilePicker.SelectedItem as ConnectionProfile); RefreshProfiles();
        edit.Children.Add(Row(Action(UiText.Get("Text184"), () => { profilePicker.SelectedItem = null; Fill(null); return Task.CompletedTask; }),
            Action(UiText.Get("Text185"), async () =>
            {
                if (systemPicker.SelectedItem is not SapSystem system) return;
                var p = current == null ? new ConnectionProfile() : JsonSerializer.Deserialize<ConnectionProfile>(JsonSerializer.Serialize(current, JsonDefaults.Options), JsonDefaults.Options)!;
                p.TargetKey = system.TargetKey; p.Name = Read(name); p.User = Read(user); p.Client = Read(client); p.Language = Read(language).ToUpperInvariant(); p.AdtBaseUrl = Read(adt); p.BwModelingBaseUrl = Read(bw); p.AllowInsecureTls = insecure.IsChecked == true;
                var secret = password.Text; password.Text = "";
                await new ProfileService(_secrets).SaveAsync(_settings, p, secret, activate.IsChecked == true, Token); await PersistAsync(); await AuditAsync("profile.saved", new { target = p.TargetKey, profile = p.Name }); RefreshProfiles(); SetStatus(UiText.Get("Text186"));
            }, true),
            Action(UiText.Get("Text188"), async () => { if (current == null) throw new InvalidOperationException(UiText.Get("Text187")); _settings.ActiveProfiles[current.TargetKey] = current.Id; await ProfileService.MaterializeAsync(_settings, Token); await PersistAsync(); await AuditAsync("profile.activated", new { target = current.TargetKey, profile = current.Name }); RefreshProfiles(); SetStatus(UiText.Get("Text189")); })));
        edit.Children.Add(Row(Action(UiText.Get("Text190"), () => PingAsync(systemPicker.SelectedItem as SapSystem, "rfc")), Action(UiText.Get("Text191"), () => PingAsync(systemPicker.SelectedItem as SapSystem, "adt")),
            Action(UiText.Get("Text192"), async () => { if (current == null) return; if (!await ConfirmAsync(UiText.Get("Text193"), UiText.Get("RemoveProfile", current.Name))) return; var p = current; _settings.Profiles.RemoveAll(x => x.Id == p.Id); if (_settings.ActiveProfiles.GetValueOrDefault(p.TargetKey) == p.Id) _settings.ActiveProfiles.Remove(p.TargetKey); await ProfileService.MaterializeAsync(_settings, Token); await PersistAsync(); RefreshProfiles(); SetStatus(UiText.Get("Text195")); })));
        foreach (var field in new[] { name, user, client, language, password, adt, bw })
            field.PropertyChanged += (_, change) => { if (change.Property == TextBox.TextProperty && !_fillingProfiles) _connectionsDirty = true; };
        insecure.IsCheckedChanged += (_, _) => { if (!_fillingProfiles) _connectionsDirty = true; };
        activate.IsCheckedChanged += (_, _) => { if (!_fillingProfiles) _connectionsDirty = true; };
        Body.Children.Add(Card(edit));
    }

    private async Task PingAsync(SapSystem? system, string component)
    {
        if (system == null) throw new InvalidOperationException(UiText.Get("Text196"));
        await PersistAsync(); SetStatus(UiText.Get("ConnectionTesting", component.ToUpperInvariant()));
        await using var session = await McpSession.StartAsync(_settings, _store.FilePath, component, Token);
        var result = await session.RequestAsync("tools/call", new { name = component == "rfc" ? "sap_rfc_ping_system" : "ping_system", arguments = new { target_key = system.TargetKey } }, Token);
        var failed = result.TryGetProperty("isError", out var error) && error.ValueKind == JsonValueKind.True;
        await AuditAsync("connection.tested", new { component, target = system.TargetKey, success = !failed });
        SetStatus(failed ? UiText.Get("Text198") : UiText.Get("ConnectionSuccess", system.Name, component.ToUpperInvariant()));
    }

    private async Task VerifyLocalAsync()
    {
        await PersistAsync(); var summaries = new List<string>();
        foreach (var component in new[] { "rfc", "adt" }.Where(c => _settings.Component(c).Installed && _settings.Component(c).Selected))
        {
            SetStatus(UiText.Get("VerifyingMcp", component.ToUpperInvariant()));
            await using var session = await McpSession.StartAsync(_settings, _store.FilePath, component, Token);
            var result = await session.RequestAsync("tools/list", ct: Token);
            summaries.Add(component.ToUpperInvariant() + ": " + result.GetProperty("tools").GetArrayLength());
        }
        if (summaries.Count == 0) throw new InvalidOperationException(UiText.Get("Text202"));
        SetStatus(UiText.Get("LocalVerified", string.Join(" · ", summaries))); await AuditAsync("mcp.verified", new { summaries });
    }

    private void Clients()
    {
        var form = new StackPanel { Spacing = 12 };
        var codex = new CheckBox { Content = "Codex", IsChecked = _settings.CodexEnabled }; var claude = new CheckBox { Content = "Claude Desktop", IsChecked = _settings.ClaudeEnabled };
        form.Children.Add(Row(codex, claude)); var codexPath = Field(form, "Codex config.toml", ClientIntegration.CodexPath); var claudePath = Field(form, UiText.Get("Text205"), ClientIntegration.ClaudePath);
        var replace = new CheckBox { Content = UiText.Get("Text206") }; form.Children.Add(replace);
        var preview = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 180, MaxHeight = 320, FontFamily = new("Consolas") };
        List<ClientPreview> previews = [];
        async Task<List<string>> Components()
        {
            var list = new List<string>(); if (_settings.Adt.Installed && _settings.Adt.Selected) list.Add("adt");
            if (_settings.Rfc.Installed && _settings.Rfc.Selected && (await PrerequisiteService.ValidateSdkAsync(_settings, Token)).State == CheckState.Ready) list.Add("rfc");
            if (list.Count == 0) throw new InvalidOperationException(UiText.Get("Text207")); return list;
        }
        form.Children.Add(Row(Action(UiText.Get("Text208"), async () =>
        {
            var components = await Components(); previews = [];
            if (codex.IsChecked == true) previews.Add(ClientIntegration.Preview("Codex", Read(codexPath), _settings, _store.FilePath, components));
            if (claude.IsChecked == true) previews.Add(ClientIntegration.Preview("Claude Desktop", Read(claudePath), _settings, _store.FilePath, components));
            if (previews.Count == 0) throw new InvalidOperationException(UiText.Get("Text209"));
            // Only our generated records are shown; existing client files can contain unrelated credentials.
            preview.Text = string.Join("\n\n", previews.Select(p => p.Client + " → " + p.Path + UiText.Get("Text210") + string.Join(", ", p.Names) + UiText.Get("Text211") + (p.Conflicts.Count == 0 ? UiText.Get("Text212") : string.Join(", ", p.Conflicts)))) + "\n\n" + ExportJson(components);
            SetStatus(UiText.Get("Text213"));
        }), Action(UiText.Get("Text214"), async () =>
        {
            if (previews.Count == 0) throw new InvalidOperationException(UiText.Get("Text215"));
            foreach (var p in previews) if (p.Conflicts.Count > 0 && replace.IsChecked != true) throw new InvalidOperationException(UiText.Get("Text216"));
            foreach (var p in previews) { await ClientIntegration.ApplyAsync(p, replace.IsChecked == true, Token); _settings.Registrations.RemoveAll(r => r.Client == p.Client && r.ConfigPath == p.Path); _settings.Registrations.Add(new(p.Client, p.Path, p.Names)); }
            _settings.CodexEnabled = codex.IsChecked == true; _settings.ClaudeEnabled = claude.IsChecked == true; await PersistAsync(); await AuditAsync("clients.configured", new { clients = previews.Select(p => p.Client) }); previews = [];
            SetStatus(UiText.Get("Text217"));
        }, true)));
        form.Children.Add(preview); form.Children.Add(Action(UiText.Get("Text218"), async () => { var components = await Components(); var file = await PickSaveAsync("sap-mcp-config.json"); if (file == null) return; await AtomicFile.WriteAsync(file, ExportJson(components), Token); SetStatus(UiText.Get("Text219")); }));
        Body.Children.Add(Card(form)); Body.Children.Add(Notice(UiText.Get("Text220")));
        Body.Children.Add(Action(UiText.Get("Text221"), VerifyLocalAsync));
    }

    private string ExportJson(IEnumerable<string> components)
    {
        var servers = new JsonObject(); foreach (var component in components) servers[ClientIntegration.ServerName(component)] = ClientIntegration.JsonEntry(component, _settings.HostExecutable, _store.FilePath);
        return new JsonObject { ["mcpServers"] = servers }.ToJsonString(JsonDefaults.Options);
    }

    private void Settings()
    {
        var paths = new StackPanel { Spacing = 10 }; var mapping = Field(paths, UiText.Get("Text222"), _settings.CredentialsFile); var secrets = Field(paths, UiText.Get("Text223"), _settings.SecretsDirectory);
        var node = Field(paths, UiText.Get("Text224"), _settings.NodeExecutable); var python = Field(paths, UiText.Get("Text225"), _settings.PythonExecutable); var powershell = Field(paths, "PowerShell", _settings.PowerShellExecutable);
        Body.Children.Add(Card(Stack(Text(UiText.Get("Text226"), "h2"), paths)));
        var editors = new List<(ComponentSettings Component, TextBox Directory, ComboBox Level, TextBox Days, TextBox Mb, TextBox Count, TextBox Timeout, TextBox Allowlist)>();
        foreach (var name in new[] { "rfc", "adt" })
        {
            var c = _settings.Component(name); var form = new StackPanel { Spacing = 10 }; form.Children.Add(Text("SAP " + name.ToUpperInvariant(), "h2"));
            var dir = Field(form, UiText.Get("Text227"), c.LogDirectory); form.Children.Add(Text(UiText.Get("Text228"), "muted")); var level = new ComboBox { ItemsSource = Enum.GetValues<LogLevel>(), SelectedItem = c.LogLevel }; form.Children.Add(level);
            var row = new Grid { ColumnDefinitions = new("*,*,*"), ColumnSpacing = 12 }; var a = new StackPanel { Spacing = 8 }; var b = new StackPanel { Spacing = 8 }; var d = new StackPanel { Spacing = 8 };
            var days = Field(a, UiText.Get("Text229"), c.RetentionDays.ToString()); var mb = Field(b, UiText.Get("Text230"), (c.MaxFileBytes / 1024 / 1024).ToString()); var count = Field(d, UiText.Get("Text231"), c.MaxFiles.ToString()); row.Children.Add(a); Grid.SetColumn(b, 1); row.Children.Add(b); Grid.SetColumn(d, 2); row.Children.Add(d); form.Children.Add(row);
            var timeout = name == "rfc" ? Field(form, UiText.Get("Text232"), c.TimeoutSeconds.ToString()) : new TextBox { Text = c.TimeoutSeconds.ToString() }; var allowlist = new TextBox { Text = c.FunctionAllowlist };
            if (name == "rfc") { form.Children.Add(Text(UiText.Get("Text233"), "muted")); form.Children.Add(allowlist); }
            editors.Add((c, dir, level, days, mb, count, timeout, allowlist));
            form.Children.Add(Action(UiText.Get("Text234"), () => { Directory.CreateDirectory(c.LogDirectory); Process.Start(new ProcessStartInfo(c.LogDirectory) { UseShellExecute = true }); return Task.CompletedTask; }));
            Body.Children.Add(Card(form));
        }
        Body.Children.Add(Action(UiText.Get("Text235"), async () =>
        {
            var candidate = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(_settings, JsonDefaults.Options), JsonDefaults.Options)!;
            var oldMapping = candidate.CredentialsFile;
            candidate.CredentialsFile = Read(mapping); candidate.SecretsDirectory = Read(secrets); candidate.NodeExecutable = Read(node); candidate.PythonExecutable = Read(python); candidate.PowerShellExecutable = Read(powershell);
            for (var i = 0; i < editors.Count; i++) { var e = editors[i]; var c = i == 0 ? candidate.Rfc : candidate.Adt; c.LogDirectory = Read(e.Directory); c.LogLevel = (LogLevel)e.Level.SelectedItem!; c.RetentionDays = int.Parse(Read(e.Days)); c.MaxFileBytes = checked(long.Parse(Read(e.Mb)) * 1024 * 1024); c.MaxFiles = int.Parse(Read(e.Count)); c.TimeoutSeconds = int.Parse(Read(e.Timeout)); c.FunctionAllowlist = Read(e.Allowlist); if (c.TimeoutSeconds is < 1 or > 3600) throw new InvalidDataException(UiText.Get("Text236")); }
            if (candidate.CredentialsFile != oldMapping)
                foreach (var p in candidate.Profiles) p.SecretRef = Path.GetRelativePath(Path.GetDirectoryName(candidate.CredentialsFile)!, ProfileService.ResolveSecret(oldMapping, p.SecretRef)).Replace('\\', '/');
            candidate.Validate(); await ProfileService.MaterializeAsync(candidate, Token); await _store.SaveAsync(candidate, Token); _settings = candidate; await AuditAsync("settings.saved"); SetStatus(UiText.Get("Text237"));
        }, true));
        var viewer = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 230, FontFamily = new("Consolas") };
        Body.Children.Add(Card(Stack(Text(UiText.Get("Text238"), "h2"), Action(UiText.Get("Text239"), async () =>
        {
            var files = new[] { Path.Combine(_settings.DataDirectory, "logs", "manager", "manager.jsonl"), Path.Combine(_settings.Rfc.LogDirectory, "operations.jsonl"), Path.Combine(_settings.Adt.LogDirectory, "operations.jsonl") };
            var lines = new List<string>(); foreach (var file in files.Where(File.Exists)) { var tail = new Queue<string>(); foreach (var line in File.ReadLines(file)) { tail.Enqueue(line); if (tail.Count > 60) tail.Dequeue(); } lines.AddRange(tail.Select(Redaction.Text)); }
            viewer.Text = lines.Count == 0 ? UiText.Get("Text240") : string.Join("\n", lines); await Task.CompletedTask;
        }), viewer)));
    }

    private async Task<string?> PickFolderAsync(string title) { var selected = await StorageProvider.OpenFolderPickerAsync(new() { Title = title, AllowMultiple = false }); return selected.FirstOrDefault()?.TryGetLocalPath(); }
    private async Task<string?> PickFileAsync(string title, string pattern) { var selected = await StorageProvider.OpenFilePickerAsync(new() { Title = title, AllowMultiple = false, FileTypeFilter = [new(UiText.Get("Text241")) { Patterns = [pattern] }] }); return selected.FirstOrDefault()?.TryGetLocalPath(); }
    private async Task<string?> PickSaveAsync(string name) { var selected = await StorageProvider.SaveFilePickerAsync(new() { SuggestedFileName = name, ShowOverwritePrompt = true }); return selected?.TryGetLocalPath(); }
    private static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    private async Task<bool> ConfirmAsync(string title, string description, Window? owner = null)
    {
        var dialog = new Window { Title = title, Width = 520, MaxHeight = 650, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false };
        var yes = new Button { Content = UiText.Get("Text242") }; var no = new Button { Content = UiText.Get("Text243") }; yes.Click += (_, _) => dialog.Close(true); no.Click += (_, _) => dialog.Close(false);
        dialog.Content = new ScrollViewer { Content = new StackPanel { Margin = new(24), Spacing = 20, Children = { Text(description), Row(no, yes) } } };
        return await dialog.ShowDialog<bool>(owner ?? this);
    }
}
