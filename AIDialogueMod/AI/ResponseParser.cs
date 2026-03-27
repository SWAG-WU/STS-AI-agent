using System;
using System.Text.Json;

namespace AIDialogueMod.AI;

public static class ResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static AIResponse Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new AIResponse { Dialogue = "" };

        var result = TryDeserialize(raw.Trim());
        if (result != null) return result;

        string? extracted = ExtractJsonObject(raw);
        if (extracted != null)
        {
            result = TryDeserialize(extracted);
            if (result != null) return result;
        }

        return new AIResponse { Dialogue = raw.Trim(), Emotion = "neutral" };
    }

    private static AIResponse? TryDeserialize(string json)
    {
        try
        {
            var response = JsonSerializer.Deserialize<AIResponse>(json, JsonOptions);
            if (response != null && !string.IsNullOrEmpty(response.Dialogue))
                return response;
        }
        catch { }
        return null;
    }

    private static string? ExtractJsonObject(string text)
    {
        int start = text.IndexOf('{');
        if (start < 0) return null;

        int depth = 0;
        bool inString = false;
        bool escaped = false;

        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (escaped) { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (c == '{') depth++;
            else if (c == '}') depth--;
            if (depth == 0) return text.Substring(start, i - start + 1);
        }
        return null;
    }
}
