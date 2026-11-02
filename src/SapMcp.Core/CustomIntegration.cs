using System.Text.Json;
using System.Text.Json.Nodes;

namespace SapMcp.Core;

public sealed record CustomMcpEntry(string Name, string Command, IReadOnlyList<string> Arguments);
public sealed record CustomMcpConfiguration(string Json, string JsonEntries, string Toml, IReadOnlyList<CustomMcpEntry> Entries);

public static partial class ClientIntegration
{
    public static string[] LaunchArguments(string component, string config) => ["--component", component, "--config", config];

    /// <summary>Builds local stdio examples only; never reads or writes client configuration.</summary>
    public static CustomMcpConfiguration CustomConfiguration(string host, string settingsPath, IEnumerable<string> components)
    {
        var selected = components.ToHashSet(StringComparer.Ordinal);
        if (selected.Any(c => c is not ("adt" or "rfc"))) throw new ArgumentException("Unknown MCP component", nameof(components));
        if (selected.Count == 0) return new("", "", "", []);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        var servers = new JsonObject();
        var entries = new List<CustomMcpEntry>();
        var toml = new List<string>();
        foreach (var component in new[] { "adt", "rfc" }.Where(selected.Contains))
        {
            var name = ServerName(component);
            servers[name] = JsonEntry(component, host, settingsPath);
            entries.Add(new(name, host, LaunchArguments(component, settingsPath)));
            toml.Add(TomlEntry(name, component, host, settingsPath));
        }
        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonEntries = servers.ToJsonString(options);
        return new(new JsonObject { ["mcpServers"] = servers }.ToJsonString(options), jsonEntries, string.Join("\n", toml), entries);
    }
}
