using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace SapMcp.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var i = Array.IndexOf(args, "--config");
        var path = i >= 0 && i + 1 < args.Length ? args[i + 1] : SapMcp.Core.SettingsStore.DefaultPath;
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(System.IO.Path.GetFullPath(path).ToLowerInvariant())));
        using var mutex = new Mutex(false, "Local\\SapMcpManager-" + hash);
        bool acquired;
        try { acquired = mutex.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) return 2;
        try { return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace().StartWithClassicDesktopLifetime(args); }
        finally { mutex.ReleaseMutex(); }
    }
}

public sealed class App : Application
{
    public override void Initialize() { Styles.Add(new FluentTheme()); RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default; }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            var i = Array.IndexOf(args, "--config");
            desktop.MainWindow = new MainWindow(i >= 0 && i + 1 < args.Length ? args[i + 1] : null);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
