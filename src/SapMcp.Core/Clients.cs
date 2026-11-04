using System.Text.Json;
using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;

namespace SapMcp.Core;

public enum ClientEntryState { New, Update, Current }
public sealed record ClientEntryChange(string Name, ClientEntryState State, string Before, string After);
public sealed record ClientApplyResult(string Client, string Path, bool Success, string? BackupPath, string? Error);
public sealed record ClientPreview(string Client, string Path, string Before, string After, List<string> Conflicts, List<string> Names)
{
    public List<ClientEntryChange> Entries { get; init; } = [];
    public List<string> Components { get; init; } = [];
    public bool Changed => Before != After;
}

public static partial class ClientIntegration
{
    public static string CodexPath => Path.Combine(Environment.GetEnvironmentVariable("CODEX_HOME") is { Length: > 0 } dir ? dir : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex"), "config.toml");
    public static string ClaudePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Claude", "claude_desktop_config.json");
    public static string ClaudeCodePath => Path.Combine(Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") is { Length: > 0 } dir ? dir : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude.json");
    public static string ServerName(string component) => component == "rfc" ? "sap_rfc_call" : "sap_adt_mcp";

    public static ClientPreview Preview(string client, string path, AppSettings s, string settingsPath, IEnumerable<string> components)
    {
        var before = File.Exists(path) ? File.ReadAllText(path) : "";
        var after = before; var conflicts = new List<string>(); var entries = new List<ClientEntryChange>();
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
                var generated = TomlEntry(name, component, s.HostExecutable, settingsPath);
                var desired = ((TomlTable)Toml.ToModel(generated)["mcp_servers"])[name];
                var existing = servers != null && servers.TryGetValue(name, out var entry) ? entry : null;
                var state = existing == null ? ClientEntryState.New : Equivalent(existing, desired) ? ClientEntryState.Current : ClientEntryState.Update;
                entries.Add(new(name, state, existing == null ? "" : JsonSerializer.Serialize(existing, JsonDefaults.Options), generated));
                if (state == ClientEntryState.Current) continue;
                if (state == ClientEntryState.Update) conflicts.Add(name);
                after = RemoveTomlServer(after, name);
                after = after.TrimEnd() + "\n\n" + generated;
            }
            if (Toml.Parse(after).HasErrors) throw new InvalidDataException(UiText.Get("Text002"));
        }
        else if (client is "Claude Desktop" or "Claude Code")
        {
            var root = string.IsNullOrWhiteSpace(before) ? new JsonObject() : JsonNode.Parse(before)?.AsObject() ?? throw new InvalidDataException(UiText.Get("Text003"));
            if (root["mcpServers"] is not JsonObject servers) { if (root.ContainsKey("mcpServers")) throw new InvalidDataException(UiText.Get("Text004")); root["mcpServers"] = servers = new JsonObject(); }
            foreach (var component in components)
            {
                var name = ServerName(component);
                var desired = JsonEntry(component, s.HostExecutable, settingsPath);
                if (client == "Claude Code") desired["type"] = "stdio";
                var existing = servers[name];
                var comparable = existing?.DeepClone();
                // Claude Code treats an omitted type as stdio.
                if (client == "Claude Code" && comparable is JsonObject obj && !obj.ContainsKey("type")) obj["type"] = "stdio";
                var state = existing == null ? ClientEntryState.New : JsonNode.DeepEquals(comparable, desired) ? ClientEntryState.Current : ClientEntryState.Update;
                entries.Add(new(name, state, existing?.ToJsonString(JsonDefaults.Options) ?? "", desired.ToJsonString(JsonDefaults.Options)));
                if (state == ClientEntryState.Current) continue;
                if (state == ClientEntryState.Update) conflicts.Add(name);
                servers[name] = desired;
            }
            after = entries.All(e => e.State == ClientEntryState.Current) ? before : root.ToJsonString(JsonDefaults.Options);
        }
        else throw new ArgumentException(UiText.Get("UnsupportedClient"));
        return new(client, path, before, after, conflicts, names) { Entries = entries };
    }

    private static bool Equivalent(object? left, object? right) => JsonNode.DeepEquals(JsonSerializer.SerializeToNode(left), JsonSerializer.SerializeToNode(right));

    public static void ValidatePreview(ClientPreview preview, bool replaceConflicts)
    {
        if (preview.Conflicts.Count > 0 && !replaceConflicts) throw new InvalidOperationException(UiText.Get("Text216"));
        if (Directory.Exists(preview.Path)) throw new IOException(UiText.Get("Clients.ReadOnly"));
        var current = File.Exists(preview.Path) ? File.ReadAllText(preview.Path) : "";
        if (current != preview.Before) throw new IOException(UiText.Get("Text007"));
        if (File.Exists(preview.Path) && (File.GetAttributes(preview.Path) & FileAttributes.ReadOnly) != 0) throw new IOException(UiText.Get("Clients.ReadOnly"));
    }

    public static async Task<string?> ApplyAsync(ClientPreview preview, bool replaceConflicts, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePreview(preview, replaceConflicts);
        if (!preview.Changed) return null;
        string? backup = null;
        if (File.Exists(preview.Path))
        {
            backup = preview.Path + ".sapmcp-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N")[..8] + ".bak";
            File.Copy(preview.Path, backup);
        }
        try { await AtomicFile.WriteAsync(preview.Path, preview.After, ct); }
        catch(Exception ex) { if(backup!=null)ex.Data["SapMcpBackupPath"]=backup;throw; }
        return backup;
    }

    public static async Task<List<ClientApplyResult>> ApplyAllAsync(IReadOnlyList<ClientPreview> previews, bool replaceConflicts, CancellationToken ct = default)
    {
        // Preflight every target before writing the first. Recheck each target immediately before writing.
        foreach (var preview in previews) ValidatePreview(preview, replaceConflicts);
        var results = new List<ClientApplyResult>();
        foreach (var preview in previews)
        {
            try { results.Add(new(preview.Client, preview.Path, true, await ApplyAsync(preview, replaceConflicts, ct), null)); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException or InvalidOperationException)
            { results.Add(new(preview.Client, preview.Path, false, ex.Data["SapMcpBackupPath"] as string, ex is OperationCanceledException ? UiText.Get("Text112") : Redaction.Text(ex.Message))); }
        }
        return results;
    }

    public static string Describe(ClientPreview preview) => preview.Client + " → " + preview.Path + "\n" + string.Join("\n", preview.Entries.Select(e => e.Name + " · " + UiText.Get("Clients." + e.State)));
    public static string Details(ClientPreview preview) => string.Join("\n\n", preview.Entries.Select(e => e.Name + "\n" + UiText.Get("Clients.Before") + "\n" + (e.Before.Length == 0 ? "—" : Redaction.Text(e.Before)) + "\n" + UiText.Get("Clients.After") + "\n" + e.After));

    private static string Quote(string text) => JsonSerializer.Serialize(text);
    private static string TomlEntry(string name, string component, string host, string config) =>
        $"[mcp_servers.{name}]\ncommand = {Quote(host)}\nargs = [{string.Join(", ", LaunchArguments(component, config).Select(Quote))}]\nstartup_timeout_sec = 60\ntool_timeout_sec = 180\nenabled = true\n";

    public static JsonObject JsonEntry(string component, string host, string config) => new()
    {
        ["command"] = host,
        ["args"] = new JsonArray(LaunchArguments(component, config).Select(a => (JsonNode?)JsonValue.Create(a)).ToArray())
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
        if (registration.Client == "OpenCode") { await RemoveOpenCodeOwnedAsync(registration,host,ct); return; }
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
