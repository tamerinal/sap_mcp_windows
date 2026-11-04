using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using SapMcp.Core;

namespace SapMcp.Desktop;

public partial class MainWindow
{
    private Action? _refreshCustomSummary, _refreshCustomAdvanced;

    private string[] ClientComponents(bool basic)
    {
        if (!basic) return new[] { "adt", "rfc" }.Where(c => _settings.Component(c).Installed && _settings.Component(c).Selected).ToArray();
        var profile = _settings.Profiles.SingleOrDefault(p => p.Id == _basicDraft.ProfileId);
        if (profile == null || !_basicPrepared || BasicFields() != _basicLoadedFields) return [];
        return RfcReadinessService.Eligible(_settings, profile, _basicReadiness, !BasicInstallation.AdtOnly(_settings));
    }

    private Expander BuildCustomIntegration(bool basic, string prefix, out Action refresh)
    {
        var content = new StackPanel { Spacing = 12 };
        var help = Text(UiText.Get("Custom.Steps")); content.Children.Add(help);
        var scope = Text(UiText.Get("Custom.Scope")); content.Children.Add(scope);
        var empty = Text(""); empty.Name = prefix + "CustomEmpty"; content.Children.Add(empty);
        var format = new ComboBox { Name = prefix + "CustomFormat", SelectedIndex = 0 };
        content.Children.Add(format);
        var output = new TextBlock { Name = prefix + "CustomStatus", TextWrapping = TextWrapping.Wrap };
        var panels = new List<StackPanel>();
        var copyButtons = new List<Button>();
        Action update = () => { };
        TextBox Code(StackPanel panel, string suffix, string label, Func<string> value)
        {
            panel.Children.Add(Text(UiText.Get(label)));
            var box = new TextBox { Name = prefix + suffix, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap,
                MinHeight = 120, MaxHeight = 280, FontFamily = new("Consolas") };
            panel.Children.Add(box);
            var copy = new Button { Name = prefix + suffix + "Copy", Content = UiText.Get("Custom.Copy") };
            copy.Click += async (_, _) =>
            {
                update(); // Recompute eligibility and paths immediately before copying.
                var text = value(); if (text.Length == 0) return;
                try
                {
                    if (Clipboard == null) { output.Text = UiText.Get("Custom.NoClipboard"); return; }
                    await Clipboard.SetTextAsync(text); output.Text = UiText.Get("Custom.Copied");
                }
                catch (Exception) { output.Text = UiText.Get("Custom.NoClipboard"); }
            };
            copyButtons.Add(copy); panel.Children.Add(copy); return box;
        }
        for (var i = 0; i < 5; i++) { var panel = new StackPanel { Spacing = 8 }; panels.Add(panel); content.Children.Add(panel); }
        var json = Code(panels[0], "CustomJson", "Custom.FullJson", () => ClientIntegration.CustomConfiguration(_settings.HostExecutable, _store.FilePath, ClientComponents(basic)).Json);
        var entries = Code(panels[0], "CustomJsonEntries", "Custom.JsonEntries", () => ClientIntegration.CustomConfiguration(_settings.HostExecutable, _store.FilePath, ClientComponents(basic)).JsonEntries);
        var toml = Code(panels[1], "CustomToml", "Custom.TomlHelp", () => ClientIntegration.CustomConfiguration(_settings.HostExecutable, _store.FilePath, ClientComponents(basic)).Toml);
        var manual = Code(panels[2], "CustomFields", "Custom.FieldsHelp", () => ManualFields(ClientIntegration.CustomConfiguration(_settings.HostExecutable, _store.FilePath, ClientComponents(basic))));
        var openCode1=Code(panels[3],"CustomOpenCodeV1","OpenCode.V1Help",()=>ClientIntegration.OpenCodeConfiguration(_settings.HostExecutable,_store.FilePath,ClientComponents(basic),OpenCodeVersion.V1));
        var openCode2=Code(panels[4],"CustomOpenCodeV2","OpenCode.V2Help",()=>ClientIntegration.OpenCodeConfiguration(_settings.HostExecutable,_store.FilePath,ClientComponents(basic),OpenCodeVersion.V2));
        content.Children.Add(output);
        update = () =>
        {
            var example = ClientIntegration.CustomConfiguration(_settings.HostExecutable, _store.FilePath, ClientComponents(basic));
            help.Text = UiText.Get("Custom.Steps"); scope.Text = UiText.Get("Custom.Scope");
            var selected = format.SelectedIndex;
            format.ItemsSource = new[] { "JSON", "TOML", UiText.Get("Custom.Fields"),"OpenCode V1","OpenCode V2" }; format.SelectedIndex = Math.Clamp(selected, 0, 4);
            empty.Text = example.Entries.Count == 0 ? UiText.Get(basic ? "Custom.EmptyBasic" : "Custom.EmptyAdvanced") : "";
            empty.IsVisible = example.Entries.Count == 0;
            json.Text = example.Json; entries.Text = example.JsonEntries; toml.Text = example.Toml; manual.Text = ManualFields(example); openCode1.Text=ClientIntegration.OpenCodeConfiguration(_settings.HostExecutable,_store.FilePath,ClientComponents(basic),OpenCodeVersion.V1);openCode2.Text=ClientIntegration.OpenCodeConfiguration(_settings.HostExecutable,_store.FilePath,ClientComponents(basic),OpenCodeVersion.V2);
            foreach (var button in copyButtons) button.IsEnabled = example.Entries.Count > 0;
            for (var i = 0; i < panels.Count; i++) panels[i].IsVisible = i == format.SelectedIndex && example.Entries.Count > 0;
            output.Text = "";
        };
        format.SelectionChanged += (_, _) => { for (var i = 0; i < panels.Count; i++) panels[i].IsVisible = i == format.SelectedIndex && ClientComponents(basic).Length > 0; };
        var expander = new Expander { Name = prefix + "CustomHelp", Header = UiText.Get("Custom.Title"), Content = content };
        expander.Expanding += (_, _) => update();
        refresh = () => { expander.Header = UiText.Get("Custom.Title"); update(); };
        if (!basic) _refreshCustomAdvanced = refresh;
        refresh(); return expander;
    }

    private static string ManualFields(CustomMcpConfiguration configuration) => string.Join("\n\n", configuration.Entries.Select(e =>
        UiText.Get("Custom.ManualEntry", e.Name, e.Command) + "\n" + string.Join("\n", e.Arguments.Select((a, i) => UiText.Get("Custom.Argument", i + 1, a)))));
}
