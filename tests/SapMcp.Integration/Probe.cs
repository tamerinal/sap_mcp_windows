using SapMcp.Core;

internal static class Probe
{
    public static async Task RunAsync(string config)
    {
        var s = await new SettingsStore(config).LoadAsync();
        await using var session = await McpSession.StartAsync(s, config, "adt");
        var tools = await session.RequestAsync("tools/list");
        Console.WriteLine("Probe: " + tools.GetProperty("tools").GetArrayLength() + " ADT tools");
    }
}
