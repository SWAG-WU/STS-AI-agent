using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIDialogueMod.AI;

public static class ResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    // Strip markdown code fences: ```json ... ``` or ``` ... ```
    private static readonly Regex CodeFenceRegex = new(
        @"```(?:json)?\s*([\s\S]*?)\s*```",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static AIResponse Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new AIResponse { Dialogue = "" };

        // Step 1: Strip markdown code fences if present
        string cleaned = StripCodeFences(raw);

        // Step 2: Try direct JSON parse
        var result = TryDeserialize(cleaned.Trim());
        if (result != null) return result;

        // Step 3: Try extracting JSON object from text
        string? extracted = ExtractJsonObject(cleaned);
        if (extracted != null)
        {
            result = TryDeserialize(extracted);
            if (result != null) return result;
        }

        // Step 4: Try extracting just the dialogue field via regex
        var dialogueMatch = Regex.Match(cleaned, @"""dialogue""\s*:\s*""((?:[^""\\]|\\.)*)""");
        if (dialogueMatch.Success)
        {
            return new AIResponse
            {
                Dialogue = UnescapeJsonString(dialogueMatch.Groups[1].Value),
                Emotion = "neutral"
            };
        }

        // Step 5: Fallback - show raw text as dialogue (should rarely happen now)
        return new AIResponse { Dialogue = raw.Trim(), Emotion = "neutral" };
    }

    private static string StripCodeFences(string text)
    {
        var match = CodeFenceRegex.Match(text);
        return match.Success ? match.Groups[1].Value : text;
    }

    private static string UnescapeJsonString(string s)
    {
        return s.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\\\", "\\");
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
