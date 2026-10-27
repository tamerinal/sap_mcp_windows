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
