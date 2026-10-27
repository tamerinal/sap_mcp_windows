using System.Text.Json;
using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;

namespace SapMcp.Core;

public sealed record ClientPreview(string Client, string Path, string Before, string After, List<string> Conflicts, List<string> Names);

public static partial class ClientIntegration
{
    public static string CodexPath => Path.Combine(Environment.GetEnvironmentVariable("CODEX_HOME") is { Length: > 0 } dir ? dir : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex"), "config.toml");
    public static string ClaudePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Claude", "claude_desktop_config.json");
    public static string ServerName(string component) => component == "rfc" ? "sap_rfc_call" : "sap_adt_mcp";

    public static ClientPreview Preview(string client, string path, AppSettings s, string settingsPath, IEnumerable<string> components)
    {
        var before = File.Exists(path) ? File.ReadAllText(path) : "";
        var after = before; var conflicts = new List<string>();
        var names = components.Select(ServerName).ToList();
        if (client == "Codex")
        {
            var syntax = Toml.Parse(before);
            if (syntax.HasErrors) throw new InvalidDataException(UiText.Get("Text001"));
            var model = Toml.ToModel(before);
            var servers = model.TryGetValue("mcp_servers", out var value) ? value as TomlTable : null;
            foreach (var component in components)
            {
                var name = ServerName(component);
                if (servers?.ContainsKey(name) == true) conflicts.Add(name);
                after = RemoveTomlServer(after, name);
                after = after.TrimEnd() + "\n\n" + TomlEntry(name, component, s.HostExecutable, settingsPath);
            }
            if (Toml.Parse(after).HasErrors) throw new InvalidDataException(UiText.Get("Text002"));
        }
        else if (client == "Claude Desktop")
        {
            var root = string.IsNullOrWhiteSpace(before) ? new JsonObject() : JsonNode.Parse(before)?.AsObject() ?? throw new InvalidDataException(UiText.Get("Text003"));
            if (root["mcpServers"] is not JsonObject servers) { if (root.ContainsKey("mcpServers")) throw new InvalidDataException(UiText.Get("Text004")); root["mcpServers"] = servers = new JsonObject(); }
            foreach (var component in components)
            {
                var name = ServerName(component);
                if (servers.ContainsKey(name)) conflicts.Add(name);
                servers[name] = JsonEntry(component, s.HostExecutable, settingsPath);
            }
            after = root.ToJsonString(JsonDefaults.Options);
        }
        else throw new ArgumentException("Desteklenmeyen istemci.");
        return new(client, path, before, after, conflicts, names);
    }

    public static async Task ApplyAsync(ClientPreview preview, bool replaceConflicts, CancellationToken ct = default)
    {
        if (preview.Conflicts.Count > 0 && !replaceConflicts) throw new InvalidOperationException(UiText.Get("Text005") + string.Join(", ", preview.Conflicts) + UiText.Get("Text006"));
        var current = File.Exists(preview.Path) ? await File.ReadAllTextAsync(preview.Path, ct) : "";
        if (current != preview.Before) throw new IOException(UiText.Get("Text007"));
        if (File.Exists(preview.Path)) File.Copy(preview.Path, preview.Path + ".sapmcp-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".bak");
        await AtomicFile.WriteAsync(preview.Path, preview.After, ct);
    }

    private static string Quote(string text) => JsonSerializer.Serialize(text);
    private static string TomlEntry(string name, string component, string host, string config) =>
        $"[mcp_servers.{name}]\ncommand = {Quote(host)}\nargs = [{Quote("--component")}, {Quote(component)}, {Quote("--config")}, {Quote(config)}]\nstartup_timeout_sec = 60\ntool_timeout_sec = 180\nenabled = true\n";

    public static JsonObject JsonEntry(string component, string host, string config) => new()
    {
        ["command"] = host,
        ["args"] = new JsonArray("--component", component, "--config", config)
    };

    private static string RemoveTomlServer(string input, string name)
    {
        var document = Toml.Parse(input);
        var ranges = new List<(int Start, int Length)>();
        foreach (var table in document.Tables)
        {
            // Parse the actual key. A quoted server name containing dots is not a nested table.
            var keyModel = Toml.ToModel("[" + table.Name + "]\n");
            if (!keyModel.TryGetValue("mcp_servers", out var value) || value is not TomlTable servers || !servers.ContainsKey(name)) continue;
            var start = table.Span.Start.Offset;
            var end = table.Span.End.Offset + 1;
            ranges.Add((start, end - start));
        }
        foreach (var range in ranges.OrderByDescending(r => r.Start)) input = input.Remove(range.Start, range.Length);
        return input;
    }

    public static async Task RemoveOwnedAsync(ClientRegistration registration, string host, CancellationToken ct = default)
    {
        if (!File.Exists(registration.ConfigPath)) return;
        var before = await File.ReadAllTextAsync(registration.ConfigPath, ct); var after = before;
        if (registration.Client == "Codex")
        {
            if (Toml.Parse(before).HasErrors) throw new InvalidDataException(UiText.Get("Text008"));
            var model = Toml.ToModel(before);
            if (model.TryGetValue("mcp_servers", out var value) && value is TomlTable servers)
                foreach (var name in registration.ServerNames)
                    if (servers.TryGetValue(name, out var server) && server is TomlTable table && table.TryGetValue("command", out var command) && string.Equals(command?.ToString(), host, StringComparison.OrdinalIgnoreCase))
                        after = RemoveTomlServer(after, name);
        }
        else
        {
            var root = JsonNode.Parse(before)!.AsObject();
            if (root["mcpServers"] is JsonObject servers)
                foreach (var name in registration.ServerNames)
                    if (string.Equals((string?)servers[name]?["command"], host, StringComparison.OrdinalIgnoreCase)) servers.Remove(name);
            after = root.ToJsonString(JsonDefaults.Options);
        }
        if (before != after) await ApplyAsync(new(registration.Client, registration.ConfigPath, before, after, [], []), true, ct);
    }
}
