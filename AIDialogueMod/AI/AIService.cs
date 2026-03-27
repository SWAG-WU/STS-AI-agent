using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AIDialogueMod.Config;

namespace AIDialogueMod.AI;

public class AIService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly ModConfig _config;

    public AIService(ModConfig config)
    {
        _config = config;
    }

    public async Task<string> SendMessageAsync(string systemPrompt, List<ChatMessage> conversationHistory)
    {
        try
        {
            return _config.Provider switch
            {
                "claude" => await SendClaudeAsync(systemPrompt, conversationHistory),
                "gpt" => await SendOpenAIAsync(systemPrompt, conversationHistory),
                _ => await SendOpenAIAsync(systemPrompt, conversationHistory),
            };
        }
        catch (TaskCanceledException) { return ""; }
        catch (Exception) { return ""; }
    }

    private async Task<string> SendClaudeAsync(string systemPrompt, List<ChatMessage> messages)
    {
        var body = new { model = string.IsNullOrEmpty(_config.Model) ? "claude-sonnet-4-20250514" : _config.Model, max_tokens = 512, system = systemPrompt, messages };
        var request = new HttpRequestMessage(HttpMethod.Post, _config.GetEffectiveApiUrl())
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", _config.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        var response = await Http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
    }

    private async Task<string> SendOpenAIAsync(string systemPrompt, List<ChatMessage> messages)
    {
        var allMessages = new List<object> { new { role = "system", content = systemPrompt } };
        foreach (var msg in messages)
            allMessages.Add(new { role = msg.Role, content = msg.Content });
        var body = new { model = string.IsNullOrEmpty(_config.Model) ? "gpt-4o" : _config.Model, max_tokens = 512, messages = allMessages };
        var request = new HttpRequestMessage(HttpMethod.Post, _config.GetEffectiveApiUrl())
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
        var response = await Http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}
