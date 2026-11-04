using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
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

public sealed partial class WindowTests
{
    [AvaloniaFact]
    public async Task CustomHelpCopiesActualPathsInEveryFormatWithoutWritingClientFiles()
    {
        using var temp=new UiTemporary(); var config=Path.Combine(temp.Root,"Türkçe # ayar","settings.json"); var store=new SettingsStore(config);
        var seed=await store.LoadAsync(); seed.UiMode="advanced"; seed.UiLanguage="en"; seed.Adt.Installed=true; seed.Rfc.Installed=true;
        await store.SaveAsync(seed); var window=new MainWindow(config); window.Show();
        try
        {
            await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[4]);
            var before=await File.ReadAllBytesAsync(config);
            var help=Named<Expander>(window,"AdvancedCustomHelp"); help.IsExpanded=true;
            var format=Named<ComboBox>(window,"AdvancedCustomFormat");
            foreach(var (index,suffix) in new[]{(0,"CustomJson"),(0,"CustomJsonEntries"),(1,"CustomToml"),(2,"CustomFields"),(3,"CustomOpenCodeV1"),(4,"CustomOpenCodeV2")})
            {
                format.SelectedIndex=index;
                var text=Named<TextBox>(window,"Advanced"+suffix).Text!;
                Assert.Contains("sap_adt_mcp",text); Assert.Contains("sap_rfc_call",text);
                Click(Named<Button>(window,"Advanced"+suffix+"Copy"));
                await Until(()=>Named<TextBlock>(window,"AdvancedCustomStatus").Text==UiText.Get("Custom.Copied"));
                Assert.Equal(text,await window.Clipboard!.TryGetTextAsync());
                if(Environment.GetEnvironmentVariable("SAP_MCP_UI_SNAPSHOTS") is {Length:>0} output)
                { format.BringIntoView();await Task.Delay(50);Dispatcher.UIThread.RunJobs();Directory.CreateDirectory(output);using var frame=window.CaptureRenderedFrame();frame!.Save(Path.Combine(output,$"custom-advanced-{index}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default); }
            }
            var json=System.Text.Json.Nodes.JsonNode.Parse(Named<TextBox>(window,"AdvancedCustomJson").Text!)!;
            Assert.Equal(config,json["mcpServers"]!["sap_adt_mcp"]!["args"]![3]!.GetValue<string>());
            Assert.Equal(before,await File.ReadAllBytesAsync(config));
            foreach(var locale in new[]{"tr","en"})
            {
                window.FindControl<ComboBox>("LanguagePicker")!.SelectedIndex=locale=="tr"?1:0;
                await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.Equal(UiText.Get("Custom.Title"),help.Header);
                Assert.Equal(UiText.Get("Custom.Fields"),format.Items.Cast<string>().ElementAt(2)); Assert.Equal("OpenCode V2",format.Items.Cast<string>().Last());
            }
            Assert.Empty((await store.LoadAsync()).Registrations); Assert.Null(typeof(MainWindow).GetField("_discoveryCancel",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window));
            // Only the explicit language changes above may persist settings; help itself is read-only.
            var current=await store.LoadAsync(); Assert.Equal(seed.ActiveProfiles,current.ActiveProfiles);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task BasicSummaryHelpSharesEligibilityAndClearsStaleExamples()
    {
        using var temp=new UiTemporary();var config=Path.Combine(temp.Root,"config","settings.json");var store=new SettingsStore(config);
        var seed=await store.LoadAsync();seed.UiMode="basic";seed.UiLanguage="en";
        seed=await new ConnectionManager(store).SelectSourceAsync(seed,"managed");
        seed=await new ConnectionManager(store).SaveSystemAsync(seed,new(){Name="Fixture",Sid="TST",Host="fictional.example.invalid",Sysnr="04"});
        var profile=new ConnectionProfile{Name="Fixture",Sid="TST",TargetKey="fictional.example.invalid:04",User="PRIVATE_USER"};
        seed.Profiles.Add(profile);seed.BasicProfileId=profile.Id;seed.ActiveProfiles[profile.TargetKey]=profile.Id;
        profile.BasicValidation=new(BasicCheck.Passed,BasicCheck.Failed("AUTHENTICATION_FAILED"),DateTimeOffset.UtcNow,BasicPolicy.Fingerprint(seed,profile),2);
        await store.SaveAsync(seed);var window=new MainWindow(config);window.Show();
        try
        {
            await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            typeof(MainWindow).GetField("_basicPrepared",flags)!.SetValue(window,true);
            typeof(MainWindow).GetMethod("SetBasicStep",flags)!.Invoke(window,[3]);
            var help=Named<Expander>(window,"BasicSummaryCustomHelp");help.IsExpanded=true;
            var json=Named<TextBox>(window,"BasicSummaryCustomJson");
            Assert.Contains("sap_adt_mcp",json.Text);Assert.DoesNotContain("sap_rfc_call",json.Text);Assert.DoesNotContain("PRIVATE_USER",json.Text);
            Click(Named<Button>(window,"BasicSummaryCustomJsonCopy"));
            await Until(()=>Named<TextBlock>(window,"BasicSummaryCustomStatus").Text==UiText.Get("Custom.Copied"));
            Assert.Equal(json.Text,await window.Clipboard!.TryGetTextAsync());
            Named<TextBox>(window,"BasicUser").Text="CHANGED";
            Assert.Equal("",json.Text);Assert.False(Named<Button>(window,"BasicSummaryCustomJsonCopy").IsEnabled);
            Assert.Equal("",Named<TextBox>(window,"BasicCustomJson").Text);Assert.True(Named<TextBlock>(window,"BasicSummaryCustomEmpty").IsVisible);
            Assert.Null(typeof(MainWindow).GetField("_discoveryCancel",flags)!.GetValue(window));
            Assert.Empty((await store.LoadAsync()).Registrations);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task BasicDefaultsAndBothModeFormsSurviveLocaleAndModeSwitches()
    {
        using var temp=new UiTemporary();var config=Path.Combine(temp.Root,"config","settings.json");var store=new SettingsStore(config);var seed=await store.LoadAsync();
        var manager=new ConnectionManager(store);seed=await manager.SelectSourceAsync(seed,"managed");await manager.SaveSystemAsync(seed,new(){Name="Fictional", Sid="TST",Host="fictional.example.invalid",Sysnr="00"});
        var window=new MainWindow(config);window.Show();
        try
        {
            await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.Equal(0,window.FindControl<ComboBox>("ModePicker")!.SelectedIndex);Assert.False(window.FindControl<StackPanel>("Navigation")!.IsVisible);
            var sdk=Named<TextBox>(window,"BasicSdkDirectory");sdk.Text="Fictional SDK path";
            var host=Named<TextBox>(window,"BasicHost");host.Text="fictional.example.invalid";
            typeof(MainWindow).GetField("_basicLastResult",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(window,(BasicCheck.Passed,BasicCheck.Passed));
            host.Text="changed.example.invalid";host.Text="fictional.example.invalid";
            await Task.Delay(40);Dispatcher.UIThread.RunJobs();
            Assert.Null(typeof(MainWindow).GetField("_basicLastResult",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window));
            Assert.Equal(UiText.Get("Basic.Changed"),Named<TextBlock>(window,"BasicResult").Text);
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
                for(var step=0;step<4;step++)
                {
                    typeof(MainWindow).GetMethod("SetBasicStep",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(window,[step]);
                    await Task.Delay(25);Dispatcher.UIThread.RunJobs();
                    Assert.Equal("fictional.example.invalid",host.Text);Assert.Equal("Fictional SDK path",sdk.Text);Assert.Equal("  unsaved şifre  ",password.Text);
                    if(Environment.GetEnvironmentVariable("SAP_MCP_UI_SNAPSHOTS") is {Length:>0} output)
                    {Directory.CreateDirectory(output);using var frame=window.CaptureRenderedFrame();frame!.Save(Path.Combine(output,$"basic-{locale}-{step}-{width}-{theme}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);}
                }
            }
            typeof(MainWindow).GetMethod("SetInferredInstance",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(window,[4]);
            Assert.Equal("04",Named<TextBox>(window,"BasicInstance").Text);host.Text="changed.example.invalid";await Task.Delay(30);Dispatcher.UIThread.RunJobs();Assert.Equal("",Named<TextBox>(window,"BasicInstance").Text);
            switcher.SelectedIndex=1;await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);Assert.Equal("advanced unsaved",advanced.Text);
            Assert.Same(advanced,window.GetLogicalDescendants().OfType<TextBox>().First(b=>b.PasswordChar!='\0'));
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public async Task PreparedAndLegacySetupReopenSummaryWithoutRunningInstallOrDiscovery()
    {
        using var temp=new UiTemporary();var config=Path.Combine(temp.Root,"config","settings.json");var store=new SettingsStore(config);
        var seed=await store.LoadAsync();seed.UiLanguage="en";seed.BasicSetup=new(DateTimeOffset.UtcNow,true);
        seed=await new ConnectionManager(store).SelectSourceAsync(seed,"managed");
        seed=await new ConnectionManager(store).SaveSystemAsync(seed,new(){Name="Saved system", Sid="TST",Host="saved.example.invalid",Sysnr="00"});
        var profile=new ConnectionProfile{Name="Saved account",TargetKey="saved.example.invalid:00",User="SAVED_USER",SecretRef=await new DpapiSecretStore().ProtectAsync("fictional-password",seed.SecretsDirectory)};
        seed.Profiles.Add(profile);seed.BasicProfileId=profile.Id;seed.ActiveProfiles[profile.TargetKey]=profile.Id;await store.SaveAsync(seed);
        for(var reopen=0;reopen<2;reopen++)
        {
            if(reopen==1)
            {
                var legacy=await store.LoadAsync();legacy.BasicSetup=null;legacy.Adt.Installed=true;legacy.Rfc.Installed=false;
                await store.SaveAsync(legacy);
            }
            var window=new MainWindow(config);window.Show();
            try
            {
                await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.True(Named<StackPanel>(window,"BasicSummaryStep").IsVisible);
                Assert.Equal("saved.example.invalid",Named<TextBox>(window,"BasicHost").Text);Assert.Equal("SAVED_USER",Named<TextBox>(window,"BasicUser").Text);
                Assert.Equal("",Named<TextBox>(window,"BasicPassword").Text);Assert.True(Named<CheckBox>(window,"BasicAdtOnly").IsChecked);
                Assert.Empty(Directory.EnumerateFiles(seed.PackageDirectory,"*",SearchOption.AllDirectories));Assert.False(Directory.Exists(Path.Combine(seed.DataDirectory,"runtimes")));
                Assert.Null(typeof(MainWindow).GetField("_discoveryCancel",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window));
                Click(Caption(window,UiText.Get("Basic.NavConnection")));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.True(Named<StackPanel>(window,"BasicConnectionStep").IsVisible);
                Named<TextBox>(window,"BasicUser").Text="UNSAVED";
                Click(Caption(window,UiText.Get("Basic.NavClients")));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.Empty(window.OwnedWindows);
                Assert.True(Named<StackPanel>(window,"BasicConnectionStep").IsVisible);Assert.Equal("UNSAVED",Named<TextBox>(window,"BasicUser").Text);
                Named<TextBox>(window,"BasicUser").Text="SAVED_USER";
                Click(Caption(window,UiText.Get("Basic.NavClients")));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.True(Named<StackPanel>(window,"BasicConnectionStep").IsVisible);Assert.False(Named<Button>(window,"BasicNext").IsEnabled);Assert.False(Named<Button>(window,"BasicSaveClients").IsEnabled);
                Assert.False(Named<CheckBox>(window,"BasicClaudeCode").IsChecked);
            }
            finally{window.Close();}
        }
    }
    [AvaloniaFact]
    public async Task ClientSelectionRegeneratesPreviewAndCannotSaveStaleTargets()
    {
        using var temp=new UiTemporary();var config=Path.Combine(temp.Root,"config","settings.json");var store=new SettingsStore(config);
        var seed=await store.LoadAsync();seed.UiMode="advanced";seed.UiLanguage="en";seed.Adt.Installed=true;seed.Rfc.Installed=false;
        await store.SaveAsync(seed);var window=new MainWindow(config);window.Show();
        try
        {
            await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[4]);
            // Use temporary targets. No test writes to the user's client configuration.
            var boxes=window.GetLogicalDescendants().OfType<TextBox>().Where(b=>!b.IsReadOnly && b.Name!="AdvancedOpenCodePath").ToArray();
            Assert.Equal(3,boxes.Length);for(var i=0;i<boxes.Length;i++)boxes[i].Text=Path.Combine(temp.Root,"client-"+i+(i==0?".toml":".json"));
            Named<CheckBox>(window,"AdvancedCodex").IsChecked=false;Named<CheckBox>(window,"AdvancedClaudeDesktop").IsChecked=false;
            Assert.False(Named<Button>(window,"AdvancedSaveClients").IsEnabled);
            Named<CheckBox>(window,"AdvancedClaudeCode").IsChecked=true;
            Assert.True(Named<Button>(window,"AdvancedSaveClients").IsEnabled);Assert.Contains("Claude Code",Named<TextBlock>(window,"AdvancedClientPreview").Text);
            Assert.DoesNotContain("Claude Desktop",Named<TextBlock>(window,"AdvancedClientPreview").Text);
            Click(Named<Button>(window,"AdvancedSaveClients"));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.True(File.Exists(boxes[2].Text));Assert.False(File.Exists(boxes[0].Text));Assert.False(File.Exists(boxes[1].Text));
            Assert.Equal("Claude Code",Assert.Single((await store.LoadAsync()).Registrations).Client);
            Named<CheckBox>(window,"AdvancedClaudeCode").IsChecked=false;Assert.False(Named<Button>(window,"AdvancedSaveClients").IsEnabled);
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public async Task BasicSaveDoesNotDiscoverAndVerificationRequiresCancelableConsent()
    {
        using var temp=new UiTemporary();var config=Path.Combine(temp.Root,"config","settings.json");var store=new SettingsStore(config);
        var seed=await store.LoadAsync();seed.UiMode="basic";seed.UiLanguage="en";seed=await new ConnectionManager(store).SelectSourceAsync(seed,"managed");await store.SaveAsync(seed);
        var window=new MainWindow(config);window.Show();
        try{
            await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Click(Caption(window,UiText.Get("Basic.NavConnection")));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Named<TextBox>(window,"BasicSid").Text="TST";Named<TextBox>(window,"BasicHost").Text="fictional.example.invalid";Named<TextBox>(window,"BasicInstance").Text="02";Named<TextBox>(window,"BasicUser").Text="DEMO";Named<TextBox>(window,"BasicPassword").Text="local-test-password";
            Assert.False(Named<Button>(window,"BasicNext").IsEnabled);
            Click(Named<Button>(window,"BasicSaveConnection"));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.Empty(window.OwnedWindows);var saved=await store.LoadAsync();Assert.Null(Assert.Single(saved.Profiles).BasicValidation);Assert.False(Directory.Exists(seed.PackageDirectory));Assert.False(Named<Button>(window,"BasicNext").IsEnabled);
            typeof(MainWindow).GetField("_basicPrepared",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(window,true);
            Click(Named<Button>(window,"BasicVerifyConnection"));await Until(()=>window.OwnedWindows.Any());
            Assert.False(Named<Button>(window,"BasicSaveConnection").IsEffectivelyEnabled);Assert.False(Named<Button>(window,"BasicNext").IsEffectivelyEnabled);
            Click(Caption(window.OwnedWindows.Single(),UiText.Get("Text243")));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
            Assert.Empty(window.OwnedWindows);Assert.Null(Assert.Single((await store.LoadAsync()).Profiles).BasicValidation);
            Click(Named<Button>(window,"BasicVerifyConnection"));await Until(()=>window.OwnedWindows.Any());
            Click(window.FindControl<Button>("CancelAction")!);await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);Assert.Empty(window.OwnedWindows);
        }finally{window.Close();}
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
        settings = await manager.SaveSystemAsync(settings, new() { Name = "Fictional", Sid="TST", Host = "fictional.example.invalid", Sysnr = "00" });
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
            Named<TextBox>(dialog,"ConnectionSid").Text="TST";
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
                await Until(()=>window.OwnedWindows.Any());
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
