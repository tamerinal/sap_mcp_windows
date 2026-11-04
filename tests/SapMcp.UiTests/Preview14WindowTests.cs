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
    public async Task OpenCodeUiAppliesBothVersionsAndPreservesSharedInstructions()
    {
        foreach(var language in new[]{"en","tr"})
        foreach(var version in new[]{OpenCodeVersion.V1,OpenCodeVersion.V2})
        {
            using var temp=new UiTemporary();var store=new SettingsStore(Path.Combine(temp.Root,"config","settings.json"));
            var seed=AppSettings.CreateDefault(temp.Root);seed.UiMode="advanced";seed.UiLanguage=language;seed.Adt.Installed=true;seed.Rfc.Installed=false;
            seed.CodexEnabled=false;seed.ClaudeEnabled=false;
            var target=Path.Combine(temp.Root,"project","opencode.jsonc");Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await File.WriteAllTextAsync(target,"{ // shared instructions must survive\n \"instructions\":[\"agent-rules.md\"],\n \"model\":\"fixture/model\",\n}");
            await store.SaveAsync(seed);var window=new MainWindow(store.FilePath);window.Show();
            try
            {
                await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[4]);
                Named<ComboBox>(window,"AdvancedOpenCodeScope").SelectedIndex=1;
                Named<TextBox>(window,"AdvancedOpenCodePath").Text=target;
                Named<ComboBox>(window,"AdvancedOpenCodeVersion").SelectedIndex=version==OpenCodeVersion.V1?0:1;
                Named<CheckBox>(window,"AdvancedOpenCode").IsChecked=true;
                await Task.Delay(60);Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                Assert.True(Named<Button>(window,"AdvancedSaveClients").IsEnabled,Named<TextBlock>(window,"AdvancedClientPreview").Text);
                Assert.DoesNotContain("sap_adt_mcp",await File.ReadAllTextAsync(target));
                Click(Named<Button>(window,"AdvancedSaveClients"));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                var text=await File.ReadAllTextAsync(target);Assert.Contains("shared instructions must survive",text);Assert.Contains("\"instructions\":[\"agent-rules.md\"]",text);
                Assert.Equal(version,ClientIntegration.DetectOpenCodeVersion(text));
                var saved=await store.LoadAsync();var registration=Assert.Single(saved.Registrations);Assert.Equal("OpenCode",registration.Client);Assert.Equal(target,registration.ConfigPath);Assert.Equal("sap_adt_mcp",Assert.Single(registration.ServerNames));
                Assert.Equal("project",saved.OpenCode!.Scope);Assert.Equal(version,saved.OpenCode.Version);
                Click(Named<Button>(window,"AdvancedSaveClients"));await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.Equal(text,await File.ReadAllTextAsync(target));
            }
            finally { window.Close(); }
        }
    }

    [AvaloniaFact]
    public async Task AdvancedVerificationConsentCanBeCancelledWithoutSavingFormOrSource()
    {
        foreach(var language in new[]{"en","tr"})
        {
            using var temp=new UiTemporary();var store=new SettingsStore(Path.Combine(temp.Root,"config","settings.json"));
            var s=AppSettings.CreateDefault(temp.Root);s.UiMode="advanced";s.UiLanguage=language;s.ConnectionSourceMode="sapGui";
            s.LandscapeFile=Path.Combine(temp.Root,"external.xml");Directory.CreateDirectory(temp.Root);
            await File.WriteAllTextAsync(s.LandscapeFile,"<Landscape><Services><Service type='SAPGUI' uuid='fixture' name='Fixture' systemid='TST' server='fictional.example.invalid:3202'/></Services></Landscape>");
            var secret=await new DpapiSecretStore().ProtectAsync("original secret",s.SecretsDirectory);
            var p=new ConnectionProfile{Name="Fixture",User="DEMO",Sid="TST",TargetKey="fictional.example.invalid:02",SecretRef=secret};s.Profiles.Add(p);s.ActiveProfiles[p.TargetKey]=p.Id;
            await ProfileService.MaterializeAsync(s);await store.SaveAsync(s);var window=new MainWindow(store.FilePath);window.Show();
            try
            {
                await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Click((Button)window.FindControl<StackPanel>("Navigation")!.Children[3]);
                var password=window.GetLogicalDescendants().OfType<TextBox>().Single(b=>b.PasswordChar!='\0');password.Text="unsaved secret";
                var before=await File.ReadAllBytesAsync(store.FilePath);var mapping=await File.ReadAllBytesAsync(s.CredentialsFile);var external=await File.ReadAllBytesAsync(s.LandscapeFile);
                Click(Named<Button>(window,"AdvancedAutomaticVerify"));await Until(()=>window.OwnedWindows.Any());
                window.OwnedWindows.Single().Close(false);await Until(()=>!window.FindControl<ProgressBar>("BusyIndicator")!.IsVisible);
                Assert.Equal(before,await File.ReadAllBytesAsync(store.FilePath));Assert.Equal(mapping,await File.ReadAllBytesAsync(s.CredentialsFile));Assert.Equal(external,await File.ReadAllBytesAsync(s.LandscapeFile));
                Assert.Equal("unsaved secret",password.Text);Assert.Equal("sapGui",(await store.LoadAsync()).ConnectionSourceMode);
                Assert.False(Directory.Exists(Path.Combine(temp.Root,"cache","basic-tests")));
            }
            finally { window.Close(); }
        }
    }
}
