using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using SapMcp.Core;
using SapMcp.Desktop;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(SapMcp.UiTests.TestAppBuilder))]

namespace SapMcp.UiTests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseSkia().WithInterFont()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public sealed class WindowTests
{
    [AvaloniaFact]
    public async Task BasicDefaultsAndBothModeFormsSurviveLocaleAndModeSwitches()
    {
        using var temp=new UiTemporary();var config=Path.Combine(temp.Root,"config","settings.json");var store=new SettingsStore(config);var seed=await store.LoadAsync();
        var manager=new ConnectionManager(store);seed=await manager.SelectSourceAsync(seed,"managed");await manager.SaveSystemAsync(seed,new(){Name="Fictional",Host="fictional.example.invalid",Sysnr="00"});
        var window=new MainWindow(config);window.Show();
        try
        {
            await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.Equal(0,window.FindControl<ComboBox>("ModePicker")!.SelectedIndex);Assert.False(window.FindControl<StackPanel>("Navigation")!.IsVisible);
            var sdk=Named<TextBox>(window,"BasicSdkDirectory");sdk.Text="Fictional SDK path";
            var host=Named<TextBox>(window,"BasicHost");host.Text="fictional.example.invalid";
            var password=Named<TextBox>(window,"BasicPassword");password.Text="  unsaved şifre  ";
            var switcher=window.FindControl<ComboBox>("ModePicker")!;
            switcher.SelectedIndex=1;await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.True(window.FindControl<StackPanel>("Navigation")!.IsVisible);
            Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[3]);
            var advanced=window.GetLogicalDescendants().OfType<TextBox>().First(b=>b.PasswordChar!='\0');advanced.Text="advanced unsaved";
            switcher.SelectedIndex=0;await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.Same(sdk,Named<TextBox>(window,"BasicSdkDirectory"));Assert.Equal("  unsaved şifre  ",password.Text);
            foreach(var locale in new[]{"en","tr"})foreach(var theme in new[]{ThemeVariant.Light,ThemeVariant.Dark})foreach(var width in new[]{940d,1240d})
            {
                window.Width=width;Application.Current!.RequestedThemeVariant=theme;window.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex=locale=="tr"?1:0;
                await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                for(var step=0;step<3;step++)
                {
                    typeof(MainWindow).GetMethod("SetBasicStep",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(window,[step]);
                    await Task.Delay(25);Dispatcher.UIThread.RunJobs();
                    Assert.Equal("fictional.example.invalid",host.Text);Assert.Equal("Fictional SDK path",sdk.Text);Assert.Equal("  unsaved şifre  ",password.Text);
                    if(Environment.GetEnvironmentVariable("SAP_MCP_UI_SNAPSHOTS") is {Length:>0} output)
                    {Directory.CreateDirectory(output);using var frame=window.CaptureRenderedFrame();frame!.Save(Path.Combine(output,$"basic-{locale}-{step}-{width}-{theme}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);}
                }
            }
            switcher.SelectedIndex=1;await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);Assert.Equal("advanced unsaved",advanced.Text);
            Assert.Same(advanced,window.GetLogicalDescendants().OfType<TextBox>().First(b=>b.PasswordChar!='\0'));
        }
        finally{window.Close();}
    }
    private sealed class UiTemporary:IDisposable
    {public string Root{get;}=Path.Combine(Path.GetTempPath(),"Basic UI "+Guid.NewGuid().ToString("N"));public void Dispose(){if(Directory.Exists(Root))Directory.Delete(Root,true);}}
    [AvaloniaFact]
    public async Task AdtHelpAndProfileControlsPreserveUnsavedValuesInBothLanguages()
    {
        var root = Path.Combine(Path.GetTempPath(), "SapMcp ADT UI " + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var store = new SettingsStore(Path.Combine(root, "config", "settings.json"));
        var settings = AppSettings.CreateDefault(root); settings.UiLanguage = "en";
        await store.SaveAsync(settings); var manager = new ConnectionManager(store);
        settings = await manager.SelectSourceAsync(settings, "managed");
        settings = await manager.SaveSystemAsync(settings, new() { Name = "Fictional", Host = "fictional.example.invalid", Sysnr = "00" });
        var profile = new ConnectionProfile { Name = "Account", TargetKey = "fictional.example.invalid:00", User = "DEMO" };
        await new ProfileService(new DpapiSecretStore()).SaveAsync(settings, profile, "fictional-test-password", true); await store.SaveAsync(settings);
        var window = new MainWindow(store.FilePath); window.Show();
        try
        {
            await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[3]);
            var timeout = Named<TextBox>(window, "AdtTimeout"); timeout.Text = "27";
            var checkbox = Named<CheckBox>(window, "AdtAutoDiscovery"); checkbox.IsChecked = true;
            foreach (var locale in new[] { "en", "tr" })
            {
                window.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex = locale == "tr" ? 1 : 0;
                await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.Equal("27", timeout.Text); Assert.True(checkbox.IsChecked);
                Click(Named<Button>(window, "AdtUrlHelp")); await Until(() => window.OwnedWindows.Any());
                var dialog = window.OwnedWindows.Single(); var text = Named<TextBox>(dialog, "AdtUrlHelpText").Text!;
                Assert.Contains("SMICM", text); Assert.Contains("Basis", text); Assert.Contains("example.invalid", text);
                Assert.Equal(UiText.Get("Adt.UrlHelpBody") + "\n\n" + UiText.Get("Adt.BasisRequest"), text);
                if (Environment.GetEnvironmentVariable("SAP_MCP_UI_SNAPSHOTS") is { Length: > 0 } output)
                {
                    Directory.CreateDirectory(output); await Task.Delay(30); Dispatcher.UIThread.RunJobs();
                    using var frame=dialog.CaptureRenderedFrame(); frame!.Save(Path.Combine(output,$"{locale}-adt-url-help.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                }
                dialog.Close(); await Until(() => !window.OwnedWindows.Any());
            }
            Assert.Equal(15, (await store.LoadAsync()).Profiles.Single().AdtTimeoutSeconds); // No implicit form save.
            Click(Named<Button>(window, "AdtCertificates"));
            await Task.Delay(20); Dispatcher.UIThread.RunJobs(); Assert.Empty(window.OwnedWindows); // Dirty form requires save first.
            Click(Caption(window, UiText.Get("Text185"))); await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Click(Named<Button>(window, "AdtCertificates")); await Until(() => window.OwnedWindows.Any());
            var trust = window.OwnedWindows.Single(); Assert.Empty(Named<ListBox>(trust, "AdtTrustedCertificates").ItemsSource!);
            trust.Close();
            Assert.Equal(profile.Id, (await store.LoadAsync()).ActiveProfiles[profile.TargetKey]);
            window.Close();
        }
        finally { window.Close(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static T Named<T>(Window owner, string name) where T : Control => owner.GetLogicalDescendants().OfType<T>().Single(c => c.Name == name);
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Until(Func<bool> done)
    {
        for (var i = 0; i < 200 && !done(); i++) { await Task.Delay(20); Dispatcher.UIThread.RunJobs(); }
        Assert.True(done());
    }
    private static Button Caption(Window owner, string text) => owner.GetLogicalDescendants().OfType<Button>().Single(b => Equals(b.Content, text));

    [AvaloniaFact]
    public async Task ManagedSourceSupportsCrudProfilesConfirmationAndLanguage()
    {
        var root = Path.Combine(Path.GetTempPath(), "SapMcp managed UI " + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var store = new SettingsStore(Path.Combine(root, "config", "settings.json"));
        var settings = AppSettings.CreateDefault(root); settings.UiLanguage = "en"; settings.ConnectionSourceMode = "managed";
        await store.SaveAsync(settings); var window = new MainWindow(store.FilePath); window.Show();
        try
        {
            await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[3]);
            Assert.True(Named<Button>(window, "AddConnection").IsEnabled);
            Click(Named<Button>(window, "AddConnection")); await Until(() => window.OwnedWindows.Any());
            var dialog = window.OwnedWindows.Single();
            Named<TextBox>(dialog, "ConnectionName").Text = "Demo & Development";
            Named<TextBox>(dialog, "ConnectionHost").Text = "sap.example";
            Named<TextBox>(dialog, "ConnectionNumber").Text = "04";
            Click(Named<Button>(dialog, "SaveConnection")); await Until(() => !window.OwnedWindows.Any());
            Assert.Equal("sap.example:04", LandscapeService.Load((await store.LoadAsync()).LandscapeFile).Single().TargetKey);
            TextBox Field(string label) => window.GetLogicalDescendants().OfType<TextBox>().Single(b => Avalonia.Automation.AutomationProperties.GetName(b) == label);
            Field("Profile name").Text = "Demo account"; Field("SAP user").Text = "DEMO";
            var password = window.GetLogicalDescendants().OfType<TextBox>().Single(b => b.PasswordChar != '\0'); password.Text = "  Şifre!42  ";
            Click(Caption(window, "Save profile")); await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            settings = await store.LoadAsync(); Assert.Single(settings.Profiles);
            var secret = ProfileService.ResolveSecret(settings.CredentialsFile, settings.Profiles[0].SecretRef);

            // Cancel source switching with a dirty profile; neither form nor source is lost.
            Field("Profile name").Text = "Unsaved";
            Named<ComboBox>(window, "ConnectionSourcePicker").SelectedIndex = 0;
            Click(Named<Button>(window, "ApplyConnectionSource")); await Until(() => window.OwnedWindows.Any());
            Click(Caption(window.OwnedWindows.Single(), "Go back"));
            await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.Equal("managed", (await store.LoadAsync()).ConnectionSourceMode); Assert.Equal("Unsaved", Field("Profile name").Text);
            Named<ComboBox>(window, "ConnectionSourcePicker").SelectedIndex = 1;
            Field("Profile name").Text = "Demo account"; Click(Caption(window, "Save profile"));
            await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);

            // Render the managed page and editor in both locales/themes/widths.
            var output = Environment.GetEnvironmentVariable("SAP_MCP_UI_SNAPSHOTS");
            foreach (var locale in new[] { "en", "tr" })
            {
                window.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex = locale == "tr" ? 1 : 0;
                await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
                foreach (var width in new[] { 1240, 940 })
                {
                    Application.Current!.RequestedThemeVariant = theme; window.Width = width;
                    await Task.Delay(25); Dispatcher.UIThread.RunJobs();
                    if (!string.IsNullOrEmpty(output))
                    {
                        Directory.CreateDirectory(output); using var frame = window.CaptureRenderedFrame();
                        frame!.Save(Path.Combine(output, $"{locale}-managed-{width}-{theme}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                    }
                }
                Click(Named<Button>(window, "EditConnection")); await Until(() => window.OwnedWindows.Any());
                dialog = window.OwnedWindows.Single(); Assert.Equal("sap.example", Named<TextBox>(dialog, "ConnectionHost").Text);
                if (!string.IsNullOrEmpty(output))
                {
                    await Task.Delay(30); Dispatcher.UIThread.RunJobs(); using var frame = dialog.CaptureRenderedFrame();
                    frame!.Save(Path.Combine(output, $"{locale}-connection-editor.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                }
                dialog.Close(); await Task.Delay(20); Dispatcher.UIThread.RunJobs();
            }
            window.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex = 0;
            await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Click(Named<Button>(window, "EditConnection")); await Until(() => window.OwnedWindows.Any()); dialog = window.OwnedWindows.Single();
            Named<TextBox>(dialog, "ConnectionHost").Text = "new.example";
            Click(Named<Button>(dialog, "SaveConnection")); await Until(() => dialog.OwnedWindows.Any());
            Click(Caption(dialog.OwnedWindows.Single(), "Confirm")); await Until(() => !window.OwnedWindows.Any());
            Assert.Equal("new.example:04", (await store.LoadAsync()).Profiles.Single().TargetKey);
            Click(Named<Button>(window, "DeleteConnection")); await Until(() => window.OwnedWindows.Any());
            Click(Caption(window.OwnedWindows.Single(), "Go back")); await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.Single((await new ManagedJsonConnectionSource().ReadCatalogAsync(await store.LoadAsync())).Systems);
            Click(Named<Button>(window, "DeleteConnection")); await Until(() => window.OwnedWindows.Any());
            Click(Caption(window.OwnedWindows.Single(), "Confirm")); await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            settings = await store.LoadAsync(); Assert.Empty(settings.Profiles); Assert.Empty(LandscapeService.Load(settings.LandscapeFile)); Assert.True(File.Exists(secret));
        }
        finally { foreach (var owned in window.OwnedWindows.ToArray()) owned.Close(); window.Close(); Directory.Delete(root, true); }
    }

    [AvaloniaFact]
    public async Task BrokenGuiSourceDoesNotBlockManagerAndCannotBeEdited()
    {
        var root = Path.Combine(Path.GetTempPath(), "SapMcp bad source " + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var settings = AppSettings.CreateDefault(root); settings.UiLanguage = "en"; settings.ConnectionSourceMode = "sapGui";
        settings.LandscapeFile = settings.ExternalLandscapeFile = Path.Combine(root, "broken.xml"); await File.WriteAllTextAsync(settings.LandscapeFile, "<broken");
        var store = new SettingsStore(Path.Combine(root, "config", "settings.json")); await store.SaveAsync(settings);
        var window = new MainWindow(store.FilePath); window.Show();
        try
        {
            await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[3]);
            Assert.False(Named<Button>(window, "AddConnection").IsEnabled);
            Assert.False(Named<Button>(window, "EditConnection").IsEnabled); Assert.False(Named<Button>(window, "DeleteConnection").IsEnabled);
            Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text?.StartsWith("Connection source unavailable:") == true);
            Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[5]); Assert.NotEmpty(window.FindControl<StackPanel>("PageBody")!.Children);
            Assert.Equal("<broken", await File.ReadAllTextAsync(settings.LandscapeFile));
        }
        finally { window.Close(); Directory.Delete(root, true); }
    }

    [AvaloniaFact]
    public async Task AllSixPagesRenderAtSupportedWidthsAndThemes()
    {
        var root = Path.Combine(Path.GetTempPath(), "SapMcp UI " + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var s = AppSettings.CreateDefault(root); s.UiLanguage = "en"; s.LandscapeFile = Path.Combine(root, "SAPUILandscape.xml");
            await File.WriteAllTextAsync(s.LandscapeFile, "<Landscape><Services><Service type='SAPGUI' name='Demo Development' systemid='DEV' server='sap.example:3200'/></Services></Landscape>");
            var config = Path.Combine(root, "config", "settings.json"); await new SettingsStore(config).SaveAsync(s);
            var window = new MainWindow(config); window.Show();
            for (var i = 0; i < 80 && !window.FindControl<StackPanel>("PageBody")!.Children.Any(); i++) { await Task.Delay(25); Dispatcher.UIThread.RunJobs(); }
            Assert.Contains("Ready", window.FindControl<TextBlock>("StatusText")!.Text);
            var output = Environment.GetEnvironmentVariable("SAP_MCP_UI_SNAPSHOTS");
            foreach (var locale in new[] { "en", "tr" })
            {
            window.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex = locale == "tr" ? 1 : 0;
            await Task.Delay(80); Dispatcher.UIThread.RunJobs();
            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
            {
                Application.Current!.RequestedThemeVariant = theme;
                foreach (var width in new[] { 1240, 940 })
                {
                    window.Width = width;
                    for (var page = 0; page < 6; page++)
                    {
                        var nav = window.FindControl<StackPanel>("Navigation")!;
                        ((Button)nav.Children[page]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await Task.Delay(30); Dispatcher.UIThread.RunJobs();
                        Assert.NotEmpty(window.FindControl<StackPanel>("PageBody")!.Children);
                        if (page == 3)
                        {
                            Assert.False(Named<Button>(window, "AddConnection").IsEnabled);
                            Assert.False(Named<Button>(window, "EditConnection").IsEnabled);
                            Assert.False(Named<Button>(window, "DeleteConnection").IsEnabled);
                        }
                        Assert.True(window.FindControl<StackPanel>("PageBody")!.Bounds.Width > 550);
                        if (!string.IsNullOrEmpty(output))
                        {
                            Directory.CreateDirectory(output);
                            using var image = window.CaptureRenderedFrame(); Assert.NotNull(image); image.Save(Path.Combine(output, $"{locale}-page-{page}-{width}-{theme}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                        }
                    }
                }
            }
            }
            // A language switch must preserve the actual open form controls and their values.
            ((Button)window.FindControl<StackPanel>("Navigation")!.Children[3]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var fields = window.GetLogicalDescendants().OfType<TextBox>().Where(b => !b.IsReadOnly).ToArray();
            var password = fields.Single(b => b.PasswordChar != '\0');
            password.Text = "  Şifre örneği  ";
            var name = fields.First(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Profil adı");
            name.Text = "Unsaved profile";
            window.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex = 0;
            await Task.Delay(100); Dispatcher.UIThread.RunJobs();
            Assert.Same(password, window.GetLogicalDescendants().OfType<TextBox>().Single(b => b.PasswordChar != '\0'));
            Assert.Equal("  Şifre örneği  ", password.Text); Assert.Equal("Unsaved profile", name.Text);
            Assert.Equal("Profile name", Avalonia.Automation.AutomationProperties.GetName(name));
            Assert.Equal("en", (await new SettingsStore(config).LoadAsync()).UiLanguage);
            Assert.Equal("TR", fields.First(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Language").Text);
            foreach (var locale in new[] { "en", "tr" })
            {
                window.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex = locale == "tr" ? 1 : 0;
                await Task.Delay(100); Dispatcher.UIThread.RunJobs();
                window.FindControl<Button>("HelpButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(100); Dispatcher.UIThread.RunJobs();
                var help = window.OwnedWindows.Single();
                var content = Assert.IsType<TextBox>(help.Content);
                var guide = Path.Combine(AppContext.BaseDirectory, "docs", locale == "tr" ? "USER-GUIDE.tr.md" : "USER-GUIDE.md");
                Assert.Equal(await File.ReadAllTextAsync(guide), content.Text);
                help.Close(); await Task.Delay(30); Dispatcher.UIThread.RunJobs();
            }
            window.Close();
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
