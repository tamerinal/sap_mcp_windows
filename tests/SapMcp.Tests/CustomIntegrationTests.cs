using System.Text.Json.Nodes;
using SapMcp.Core;
using Tomlyn;
using Tomlyn.Model;
using Xunit;

namespace SapMcp.Tests;

public sealed class CustomIntegrationTests
{
    [Theory]
    [InlineData("adt")]
    [InlineData("rfc")]
    [InlineData("rfc,adt,adt")]
    public void ExamplesRoundTripAndMatchAutomaticRegistration(string selected)
    {
        using var temp = new TestDirectory();
        var settings = AppSettings.CreateDefault(temp.Root);
        settings.HostExecutable = "C:\\Program Files\\Türkçe # SAP\\SapMcpHost.exe";
        var config = "D:\\SAP şube\\özel \"alıntı\"\\settings.json";
        var components = selected.Split(',');
        var example = ClientIntegration.CustomConfiguration(settings.HostExecutable, config, components);
        var json = JsonNode.Parse(example.Json)!;
        Assert.True(JsonNode.DeepEquals(json["mcpServers"], JsonNode.Parse(example.JsonEntries)));
        var tables = (TomlTable)Toml.ToModel(example.Toml)["mcp_servers"];
        Assert.Equal(components.Distinct().Count(), tables.Count);
        var automaticJson = ClientIntegration.Preview("Claude Desktop", Path.Combine(temp.Root, "client.json"), settings, config, components.Distinct());
        Assert.True(JsonNode.DeepEquals(json["mcpServers"], JsonNode.Parse(automaticJson.After)!["mcpServers"]));
        var automaticToml = ClientIntegration.Preview("Codex", Path.Combine(temp.Root, "client.toml"), settings, config, components.Distinct());
        var automaticTables = (TomlTable)Toml.ToModel(automaticToml.After)["mcp_servers"];
        foreach (var entry in example.Entries)
        {
            Assert.Equal(settings.HostExecutable, entry.Command);
            var table = (TomlTable)tables[entry.Name];
            Assert.Equal(entry.Command, table["command"]);
            Assert.Equal(entry.Arguments, ((TomlArray)table["args"]).Cast<string>());
            Assert.Equal(table["command"], ((TomlTable)automaticTables[entry.Name])["command"]);
            Assert.Equal(entry.Arguments, json["mcpServers"]![entry.Name]!["args"]!.AsArray().Select(n => n!.GetValue<string>()));
            Assert.Equal(new[] { "--component", entry.Name == "sap_rfc_call" ? "rfc" : "adt", "--config", config }, entry.Arguments);
        }
        Assert.False(File.Exists(automaticJson.Path)); Assert.False(File.Exists(automaticToml.Path));
    }

    [Fact]
    public void EmptyAndInvalidSelectionsCannotProduceRunnableEntries()
    {
        var empty = ClientIntegration.CustomConfiguration("", "", []);
        Assert.Empty(empty.Entries); Assert.Empty(empty.Json); Assert.Empty(empty.JsonEntries); Assert.Empty(empty.Toml);
        Assert.Throws<ArgumentException>(() => ClientIntegration.CustomConfiguration("host", "config", ["unknown"]));
    }

    [Fact]
    public void BasicFilteringOmitsFailedRfcAndInvalidatedProfilesWithoutDisclosingSecrets()
    {
        using var temp = new TestDirectory(); var settings = AppSettings.CreateDefault(temp.Root);
        var profile = new ConnectionProfile { TargetKey = "fixture.invalid:04", User = "PRIVATE_USER", SecretRef = "PRIVATE_SECRET_REFERENCE" };
        settings.ActiveProfiles[profile.TargetKey] = profile.Id;
        profile.BasicValidation = new(BasicCheck.Passed, BasicCheck.Failed("AUTHENTICATION_FAILED"), DateTimeOffset.UtcNow, BasicPolicy.Fingerprint(settings, profile), 2);
        var example = ClientIntegration.CustomConfiguration(settings.HostExecutable, "config.json", RfcReadinessService.Eligible(settings, profile, null, true));
        Assert.Equal("sap_adt_mcp", Assert.Single(example.Entries).Name);
        Assert.DoesNotContain("PRIVATE_", example.Json + example.JsonEntries + example.Toml);
        profile.User = "CHANGED";
        Assert.Empty(ClientIntegration.CustomConfiguration(settings.HostExecutable, "config.json", RfcReadinessService.Eligible(settings, profile, null, true)).Entries);
    }
}
