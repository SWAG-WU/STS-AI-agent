using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIDialogueMod.Config;

public class ModConfig
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AIDialogueMod"
    );
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("apiUrl")]
    public string ApiUrl { get; set; } = "";

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "claude";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "zh";

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public string GetEffectiveApiUrl()
    {
        if (!string.IsNullOrWhiteSpace(ApiUrl))
            return ApiUrl;

        return Provider switch
        {
            "claude" => "https://api.anthropic.com/v1/messages",
            "gpt" => "https://api.openai.com/v1/chat/completions",
            _ => ApiUrl
        };
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static ModConfig FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ModConfig>(json, JsonOptions) ?? new ModConfig();
        }
        catch
        {
            return new ModConfig();
        }
    }

    public static ModConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                return FromJson(json);
            }
        }
        catch { }
        return new ModConfig();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigPath, ToJson());
        }
        catch { }
    }
}
