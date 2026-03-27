using AIDialogueMod.AI;
using Xunit;

namespace AIDialogueMod.Tests.AI;

public class ResponseParserTests
{
    [Fact]
    public void Parses_valid_json_response()
    {
        string json = "{\"dialogue\": \"Hello\", \"emotion\": \"threatening\", \"actions\": [{\"type\": \"modify_enemy_strength\", \"value\": 3}], \"end_conversation\": false}";
        var result = ResponseParser.Parse(json);
        Assert.Equal("Hello", result.Dialogue);
        Assert.Equal("threatening", result.Emotion);
        Assert.Single(result.Actions);
        Assert.Equal("modify_enemy_strength", result.Actions[0].Type);
        Assert.Equal(3, result.Actions[0].Value);
        Assert.False(result.EndConversation);
    }

    [Fact]
    public void Extracts_json_from_surrounding_text()
    {
        string messy = "Sure, here's my response:\n{\"dialogue\": \"Hello!\", \"emotion\": \"friendly\", \"actions\": [], \"end_conversation\": false}\nThat's my reply.";
        var result = ResponseParser.Parse(messy);
        Assert.Equal("Hello!", result.Dialogue);
    }

    [Fact]
    public void Returns_fallback_on_completely_invalid_input()
    {
        string garbage = "This is not JSON at all";
        var result = ResponseParser.Parse(garbage);
        Assert.Equal("This is not JSON at all", result.Dialogue);
        Assert.Empty(result.Actions);
        Assert.False(result.EndConversation);
    }

    [Fact]
    public void Returns_fallback_on_empty_input()
    {
        var result = ResponseParser.Parse("");
        Assert.NotNull(result);
        Assert.Empty(result.Actions);
    }

    [Fact]
    public void Handles_missing_actions_field()
    {
        string json = "{\"dialogue\": \"Hi\", \"emotion\": \"neutral\"}";
        var result = ResponseParser.Parse(json);
        Assert.Equal("Hi", result.Dialogue);
        Assert.Empty(result.Actions);
    }

    [Fact]
    public void Handles_end_conversation_true()
    {
        string json = "{\"dialogue\": \"Goodbye!\", \"emotion\": \"calm\", \"actions\": [{\"type\": \"skip_event\"}], \"end_conversation\": true}";
        var result = ResponseParser.Parse(json);
        Assert.True(result.EndConversation);
        Assert.Equal("skip_event", result.Actions[0].Type);
    }

    [Fact]
    public void Extracts_json_with_nested_braces()
    {
        string text = "Response: {\"dialogue\": \"I'll give you power!\", \"emotion\": \"kind\", \"actions\": [{\"type\": \"no_action\"}], \"end_conversation\": false}";
        var result = ResponseParser.Parse(text);
        Assert.Contains("power", result.Dialogue);
    }
}
