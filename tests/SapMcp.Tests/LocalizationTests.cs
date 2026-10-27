using System.Globalization;
using System.Text.Json;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void MissingLanguagePreservesSchemaAndProfiles()
    {
        var oldJson = "{\"schemaVersion\":1,\"profiles\":[{\"name\":\"Existing\",\"user\":\"DEMO\",\"language\":\"DE\",\"secretRef\":\"unchanged.password.dpapi\"}]}";
        var settings = JsonSerializer.Deserialize<AppSettings>(oldJson, JsonDefaults.Options)!;
        Assert.Null(settings.UiLanguage); Assert.Equal(1, settings.SchemaVersion);
        settings.UiLanguage = "en";
        var roundtrip = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings, JsonDefaults.Options), JsonDefaults.Options)!;
        Assert.Equal("DE", roundtrip.Profiles[0].Language);
        Assert.Equal("unchanged.password.dpapi", roundtrip.Profiles[0].SecretRef);
        Assert.Equal("DEMO", roundtrip.Profiles[0].User);
    }

    [Theory]
    [InlineData("tr-TR", "tr")]
    [InlineData("en-US", "en")]
    [InlineData("de-DE", "en")]
    public void InitialLanguageFollowsSystemUiAndSavedChoiceWins(string system, string expected)
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(system);
            Assert.Equal(expected, UiText.ResolveLanguage(null));
            Assert.Equal("en", UiText.ResolveLanguage("en")); Assert.Equal("tr", UiText.ResolveLanguage("tr"));
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }

    [Fact]
    public void ResourceCatalogContainsBothLanguagesWithMatchingPlaceholders()
    {
        using var stream = typeof(UiText).Assembly.GetManifestResourceStream("SapMcp.Core.Resources.Strings.json")!;
        var catalog = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!;
        foreach (var (key, entry) in catalog)
        {
            Assert.Equal(2, entry.Count); Assert.False(string.IsNullOrWhiteSpace(entry["en"]), key);
            Assert.False(string.IsNullOrWhiteSpace(entry["tr"]), key);
            var en = System.Text.RegularExpressions.Regex.Matches(entry["en"], @"\{\d+\}").Select(m => m.Value).Order().ToArray();
            var tr = System.Text.RegularExpressions.Regex.Matches(entry["tr"], @"\{\d+\}").Select(m => m.Value).Order().ToArray();
            Assert.Equal(en, tr);
        }
    }
}
