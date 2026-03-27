using System.Collections.Generic;
using System.Text.Json.Serialization;
using AIDialogueMod.Actions;

namespace AIDialogueMod.AI;

public class AIResponse
{
    [JsonPropertyName("dialogue")]
    public string Dialogue { get; set; } = "";

    [JsonPropertyName("emotion")]
    public string Emotion { get; set; } = "neutral";

    [JsonPropertyName("actions")]
    public List<GameAction> Actions { get; set; } = new();

    [JsonPropertyName("end_conversation")]
    public bool EndConversation { get; set; } = false;
}
