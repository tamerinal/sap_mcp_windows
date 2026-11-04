using System.Text.Json.Nodes;

namespace SapMcp.Core;

public enum OpenCodeVersion { V1 = 1, V2 = 2 }
public sealed class OpenCodePreferences
{
    public bool Enabled { get; set; }
    public string Scope { get; set; } = "global";
    public string ConfigPath { get; set; } = "";
    public OpenCodeVersion Version { get; set; } = OpenCodeVersion.V2;
}

public static partial class ClientIntegration
{
    public static string OpenCodeDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "opencode");
    public static string[] OpenCodeFiles(string directory) => new[] { Path.Combine(directory,"opencode.jsonc"), Path.Combine(directory,"opencode.json"), Path.Combine(directory,".opencode","opencode.jsonc"), Path.Combine(directory,".opencode","opencode.json") }.Where(File.Exists).ToArray();
    public static OpenCodeVersion? DetectOpenCodeVersion(string text)
    {
        var root = JsoncEditor.Read(text);
        if (root["mcp"] == null) return null;
        if (root["mcp"] is not JsonObject mcp) throw new InvalidDataException(UiText.Get("OpenCode.Invalid"));
        if (mcp.ContainsKey("servers"))
        {
            if (mcp["servers"] is not JsonObject || mcp.Any(p => p.Key != "servers" && p.Value is JsonObject o && o.ContainsKey("type"))) throw new InvalidDataException(UiText.Get("OpenCode.Mixed"));
            return OpenCodeVersion.V2;
        }
        return mcp.Count > 0 ? OpenCodeVersion.V1 : null;
    }
    public static JsonObject OpenCodeEntry(string component, string host, string config, OpenCodeVersion version)
    {
        if (version is not (OpenCodeVersion.V1 or OpenCodeVersion.V2)) throw new ArgumentOutOfRangeException(nameof(version));
        if (component is not ("adt" or "rfc")) throw new ArgumentException("Unknown MCP component",nameof(component));
        var entry = new JsonObject { ["type"] = "local", ["command"] = new JsonArray(new[] { host }.Concat(LaunchArguments(component,config)).Select(a => (JsonNode?)JsonValue.Create(a)).ToArray()) };
        if (version == OpenCodeVersion.V1) entry["enabled"] = true;
        else entry["disabled"] = false;
        return entry;
    }
    public static string OpenCodeConfiguration(string host, string config, IEnumerable<string> components, OpenCodeVersion version)
    {
        var servers = new JsonObject(); foreach (var c in components.Distinct()) servers[ServerName(c)] = OpenCodeEntry(c,host,config,version);
        return servers.Count == 0 ? "" : new JsonObject { ["$schema"] = "https://opencode.ai/config.json", ["mcp"] = version == OpenCodeVersion.V1 ? servers : new JsonObject { ["servers"] = servers } }.ToJsonString(JsonDefaults.Options);
    }
    private static string[] OpenCodePath(OpenCodeVersion version, string name) => version == OpenCodeVersion.V1 ? ["mcp",name] : ["mcp","servers",name];
    private static JsonObject? OpenCodeServers(JsonObject root, OpenCodeVersion version) => version == OpenCodeVersion.V1 ? root["mcp"] as JsonObject : root["mcp"]?["servers"] as JsonObject;
    private static bool OpenCodeOwned(JsonNode? node, string host, string? component = null, string? config = null)
    {
        static string? Text(JsonNode? value)=>value is JsonValue v && v.TryGetValue<string>(out var text)?text:null;
        if (node is not JsonObject entry || Text(entry["type"]) != "local" || entry["command"] is not JsonArray command || command.Count != 5) return false;
        return string.Equals(Text(command[0]),host,StringComparison.OrdinalIgnoreCase) && Text(command[1]) == "--component" && Text(command[3]) == "--config"
            && Text(command[2]) is "adt" or "rfc" && (component == null || Text(command[2]) == component) && (config == null || string.Equals(Text(command[4]),config,StringComparison.OrdinalIgnoreCase));
    }
    public static ClientPreview PreviewOpenCode(string path, AppSettings s, string settingsPath, IEnumerable<string> components, OpenCodeVersion version)
    {
        var selected = components.Distinct().ToArray();
        var before = File.Exists(path) ? File.ReadAllText(path) : "";
        var detected = DetectOpenCodeVersion(before);
        if (detected != null && detected != version) throw new InvalidDataException(UiText.Get("OpenCode.VersionMismatch"));
        var root = JsoncEditor.Read(before); var servers = OpenCodeServers(root,version);
        var after = before; var entries = new List<ClientEntryChange>(); var conflicts = new List<string>(); var names = new List<string>();
        foreach (var component in selected)
        {
            var aliases = servers?.Where(p => OpenCodeOwned(p.Value,s.HostExecutable,component,settingsPath)).Select(p => p.Key).ToArray() ?? [];
            if (aliases.Length > 1) throw new InvalidDataException(UiText.Get("OpenCode.Duplicate"));
            var name = aliases.SingleOrDefault() ?? ServerName(component); names.Add(name);
            var desired = OpenCodeEntry(component,s.HostExecutable,settingsPath,version);
            var existing = servers?[name];
            // Preserve additional per-server settings on registrations already pointing to this Host.
            if (OpenCodeOwned(existing,s.HostExecutable,component,settingsPath) && existing is JsonObject owned)
            {
                if(owned.ContainsKey(version==OpenCodeVersion.V1?"disabled":"enabled"))throw new InvalidDataException(UiText.Get("OpenCode.Mixed"));
                foreach (var p in owned) if (!desired.ContainsKey(p.Key)) desired[p.Key] = p.Value?.DeepClone();
            }
            var state = existing == null ? ClientEntryState.New : JsonNode.DeepEquals(existing,desired) ? ClientEntryState.Current : ClientEntryState.Update;
            entries.Add(new(name,state,existing?.ToJsonString() ?? "",desired.ToJsonString()));
            if (state == ClientEntryState.Current) continue;
            if (state == ClientEntryState.Update) conflicts.Add(name);
            after = JsoncEditor.Set(after,OpenCodePath(version,name),desired);
        }
        if (after != before && !root.ContainsKey("$schema")) after = JsoncEditor.Set(after,["$schema"],JsonValue.Create("https://opencode.ai/config.json"));
        JsoncEditor.Read(after);
        return new("OpenCode",path,before,after,conflicts,names) { Entries = entries, Components = selected.ToList() };
    }
    private static async Task RemoveOpenCodeOwnedAsync(ClientRegistration registration, string host, CancellationToken ct)
    {
        var before = await File.ReadAllTextAsync(registration.ConfigPath,ct);
        var version = DetectOpenCodeVersion(before); if (version == null) return;
        var servers = OpenCodeServers(JsoncEditor.Read(before),version.Value); var after = before;
        foreach (var name in registration.ServerNames)
            if (OpenCodeOwned(servers?[name],host)) after = JsoncEditor.Set(after,OpenCodePath(version.Value,name),null);
        if (after != before) await ApplyAsync(new("OpenCode",registration.ConfigPath,before,after,[],[]),true,ct);
    }
}
