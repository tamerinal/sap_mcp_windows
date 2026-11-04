using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SapMcp.Core;

// Change only the selected property spans; comments and unrelated configuration stay intact.
internal static class JsoncEditor
{
    private sealed record Property(string Name, int Start, Node Value);
    private sealed record Node(int Start, int End, List<Property>? Properties);
    internal static readonly JsonDocumentOptions Options = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
    internal static JsonObject Read(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();
        Parse(text); // Also reject duplicate keys instead of silently choosing one.
        return JsonNode.Parse(text, documentOptions: Options) as JsonObject ?? throw new InvalidDataException(UiText.Get("OpenCode.Invalid"));
    }
    private static Node Parse(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        int Position(long offset) => Encoding.UTF8.GetCharCount(bytes.AsSpan(0, checked((int)offset)));
        Node Walk(ref Utf8JsonReader r)
        {
            var start = Position(r.TokenStartIndex);
            List<Property>? properties = r.TokenType == JsonTokenType.StartObject ? [] : null;
            if (r.TokenType == JsonTokenType.StartObject)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                while (r.Read() && r.TokenType != JsonTokenType.EndObject)
                {
                    if (r.TokenType != JsonTokenType.PropertyName) throw new JsonException();
                    var key = r.GetString()!; if (!names.Add(key)) throw new InvalidDataException(UiText.Get("OpenCode.Invalid"));
                    var keyStart = Position(r.TokenStartIndex); r.Read();
                    properties!.Add(new(key, keyStart, Walk(ref r)));
                }
            }
            else if (r.TokenType == JsonTokenType.StartArray)
                while (r.Read() && r.TokenType != JsonTokenType.EndArray) Walk(ref r);
            return new(start, Position(r.BytesConsumed), properties);
        }
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
        var root = Walk(ref reader); if (reader.Read()) throw new JsonException(); return root;
    }
    private static int Comma(string text, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (char.IsWhiteSpace(text[i])) continue;
            if (text[i] == ',' ) return i;
            if (text[i] == '/' && i + 1 < end && text[i + 1] == '/') { while (i < end && text[i] != '\n') i++; continue; }
            if (text[i] == '/' && i + 1 < end && text[i + 1] == '*') { i += 2; while (i + 1 < end && !(text[i] == '*' && text[i + 1] == '/')) i++; i++; continue; }
            return -1;
        }
        return -1;
    }
    private static string PreserveValueComments(string previous,string replacement)
    {
        var bytes=Encoding.UTF8.GetBytes(previous);
        var reader=new Utf8JsonReader(bytes,new JsonReaderOptions{AllowTrailingCommas=true,CommentHandling=JsonCommentHandling.Allow});
        var comments=new List<string>();
        while(reader.Read())if(reader.TokenType==JsonTokenType.Comment)
            comments.Add(Encoding.UTF8.GetString(bytes,checked((int)reader.TokenStartIndex),checked((int)(reader.BytesConsumed-reader.TokenStartIndex))));
        return comments.Count==0?replacement:string.Join("\n",comments)+"\n"+replacement;
    }
    internal static string Set(string text, IReadOnlyList<string> path, JsonNode? value)
    {
        if (string.IsNullOrWhiteSpace(text)) text = "{}";
        var parent = Parse(text);
        for (var i = 0; i < path.Count - 1; i++)
        {
            var child = parent.Properties?.SingleOrDefault(p => p.Name == path[i]);
            if (child == null)
            {
                if (value == null) return text;
                JsonNode nested = value.DeepClone();
                for (var j = path.Count - 1; j > i; j--) nested = new JsonObject { [path[j]] = nested };
                return Set(text, path.Take(i + 1).ToArray(), nested);
            }
            parent = child.Value;
            if (parent.Properties == null) throw new InvalidDataException(UiText.Get("OpenCode.Invalid"));
        }
        var properties = parent.Properties ?? throw new InvalidDataException(UiText.Get("OpenCode.Invalid"));
        var existing = properties.SingleOrDefault(p => p.Name == path[^1]);
        if (existing != null)
        {
            if (value != null)
            {
                var original=JsonNode.Parse(text[existing.Value.Start..existing.Value.End],documentOptions:Options);
                if(JsonNode.DeepEquals(original,value))return text;
                if(value is JsonObject desired && existing.Value.Properties is {} previous)
                {
                    foreach(var p in previous.Where(p=>!desired.ContainsKey(p.Name)))text=Set(text,path.Concat([p.Name]).ToArray(),null);
                    foreach(var p in desired)text=Set(text,path.Concat([p.Key]).ToArray(),p.Value);
                    return text;
                }
                var replacement=PreserveValueComments(text[existing.Value.Start..existing.Value.End],value.ToJsonString());
                return text.Remove(existing.Value.Start, existing.Value.End - existing.Value.Start).Insert(existing.Value.Start,replacement);
            }
            var index = properties.IndexOf(existing);
            var comma = Comma(text, existing.Value.End, parent.End - 1);
            if (comma >= 0) return text.Remove(comma, 1).Remove(existing.Start, existing.Value.End - existing.Start);
            if (index > 0)
            {
                comma = Comma(text, properties[index - 1].Value.End, existing.Start);
                var removed = text.Remove(existing.Start, existing.Value.End - existing.Start);
                return comma >= 0 ? removed.Remove(comma, 1) : removed;
            }
            return text.Remove(existing.Start, existing.Value.End - existing.Start);
        }
        if (value == null) return text;
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var trailing = properties.Count > 0 && Comma(text, properties[^1].Value.End, parent.End - 1) >= 0;
        var prefix = properties.Count > 0 && !trailing ? "," : "";
        return text.Insert(parent.End - 1, prefix + newline + "  " + JsonSerializer.Serialize(path[^1]) + ": " + value.ToJsonString() + (trailing ? "," : "") + newline);
    }
}
