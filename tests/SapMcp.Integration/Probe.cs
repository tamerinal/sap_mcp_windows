using SapMcp.Core;

internal static class Probe
{
    public static async Task RunAsync(string config, string component = "adt")
    {
        var s = await new SettingsStore(config).LoadAsync();
        await using var session = await McpSession.StartAsync(s, config, component);
        var tools = await session.RequestAsync("tools/list");
        Console.WriteLine("Probe: " + tools.GetProperty("tools").GetArrayLength() + " " + component.ToUpperInvariant() + " tools");
    }
}
