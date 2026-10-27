using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace SapMcp.Core;

/// <summary>Shared UI and diagnostic resources. Protocol IDs never pass through this catalog.</summary>
public static class UiText
{
    private static readonly Dictionary<string, Dictionary<string, string>> Catalog = Load();
    private static readonly ConcurrentDictionary<string, (string Key, object?[] Args)> Rendered = new();
    public static string Language { get; private set; } = ResolveLanguage(null);
    public static string ResolveLanguage(string? saved) => saved is "en" or "tr" ? saved :
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? "tr" : "en";
    public static void SetLanguage(string? language) => Language = ResolveLanguage(language);
    public static string Get(string key, params object?[] args)
    {
        var text = string.Format(CultureInfo.InvariantCulture, Catalog[key][Language], args);
        // Only resource-produced strings are eligible for redisplay translation.
        if (Rendered.Count > 4096) Rendered.Clear();
        Rendered[text] = (key, args);
        return text;
    }
    public static string Redisplay(string text)
    {
        if (Rendered.TryGetValue(text, out var rendered)) return Get(rendered.Key, rendered.Args);
        foreach (var (key, translations) in Catalog)
            if (translations.Values.Contains(text) && !text.Contains('{')) return Get(key);
        // Never search/replace fragments inside user names, paths or technical output.
        return text;
    }
    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        using var stream = typeof(UiText).Assembly.GetManifestResourceStream("SapMcp.Core.Resources.Strings.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!;
    }
}
