using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using SapMcp.Core;
using SapMcp.Desktop;
using Xunit;

namespace SapMcp.UiTests;

public sealed partial class WindowTests
{
    [AvaloniaFact]
    public async Task AdvancedAddsSecondProfileAndSwitchesActiveAccountWithoutReplacingFirst()
    {
        foreach (var language in new[] { "en", "tr" })
        {
            using var temp = new UiTemporary(); var store = new SettingsStore(Path.Combine(temp.Root, "config", "settings.json"));
            var s = AppSettings.CreateDefault(temp.Root); s.UiMode = "advanced"; s.UiLanguage = language;
            await store.SaveAsync(s); var manager = new ConnectionManager(store);
            s = await manager.SelectSourceAsync(s, "managed");
            s = await manager.SaveSystemAsync(s, new() { Name = "Fictional", Sid = "TST", Host = "fictional.example.invalid", Sysnr = "02" });
            var first = new ConnectionProfile { Name = "First", User = "DEMO", TargetKey = "fictional.example.invalid:02", AdtTimeoutSeconds = 42, AdtAutoDiscovery = true };
            var secrets = new DpapiSecretStore(); await new ProfileService(secrets).SaveAsync(s, first, "first fixture password", true); await store.SaveAsync(s);
            var originalSecret = await File.ReadAllBytesAsync(ProfileService.ResolveSecret(s.CredentialsFile, first.SecretRef));
            var window = new MainWindow(store.FilePath); window.Show();
            try
            {
                await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[3]);
                Click(Caption(window, UiText.Get("Text184")));
                Named<TextBox>(window, "AdvancedProfileName").Text = "Second";
                Named<TextBox>(window, "AdvancedProfileUser").Text = "DEMO2";
                Named<TextBox>(window, "AdvancedProfileClient").Text = "200";
                Named<TextBox>(window, "AdvancedProfilePassword").Text = "second fixture password";
                Named<CheckBox>(window, "AdvancedProfileActivateOnSave").IsChecked = false;
                Click(Caption(window, UiText.Get("Text185"))); await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                var saved = await store.LoadAsync(); Assert.Equal(2, saved.Profiles.Count);
                var second = saved.Profiles.Single(p => p.Name == "Second");
                Assert.Equal(15, second.AdtTimeoutSeconds); Assert.False(second.AdtAutoDiscovery); Assert.Equal("200", second.Client);
                Assert.Equal(first.Id, saved.ActiveProfiles[first.TargetKey]);
                var retained = saved.Profiles.Single(p => p.Id == first.Id);
                Assert.Equal(42, retained.AdtTimeoutSeconds); Assert.True(retained.AdtAutoDiscovery);
                Assert.Equal(originalSecret, await File.ReadAllBytesAsync(ProfileService.ResolveSecret(saved.CredentialsFile, retained.SecretRef)));
                var picker = Named<ComboBox>(window, "AdvancedProfilePicker");
                picker.SelectedItem = picker.ItemsSource!.Cast<ConnectionProfile>().Single(p => p.Id == second.Id);
                Click(Caption(window, UiText.Get("Text188"))); await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.Equal(second.Id, (await store.LoadAsync()).ActiveProfiles[first.TargetKey]);
                Assert.Equal("second fixture password", await secrets.ReadAsync(ProfileService.ResolveSecret(saved.CredentialsFile, second.SecretRef)));
                picker.SelectedItem = picker.ItemsSource!.Cast<ConnectionProfile>().Single(p => p.Id == first.Id);
                Assert.Equal("First", Named<TextBox>(window, "AdvancedProfileName").Text);
                Click(Caption(window, UiText.Get("Text188"))); await Until(() => !window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.Equal(first.Id, (await store.LoadAsync()).ActiveProfiles[first.TargetKey]);
            }
            finally { window.Close(); }
        }
    }
}
