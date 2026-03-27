using AIDialogueMod.AI;
using AIDialogueMod.Personality;
using System.Collections.Generic;
using Xunit;

namespace AIDialogueMod.Tests.AI;

public class PromptBuilderTests
{
    [Fact]
    public void System_prompt_contains_character_name()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt("熔岩巨兽", CharacterType.Elite,
            new List<PersonalityType> { PersonalityType.Aggressive, PersonalityType.Greedy },
            "zh", 50, 80, 100, "HP 120, 攻击力 18");
        Assert.Contains("熔岩巨兽", prompt);
    }

    [Fact]
    public void System_prompt_contains_personality_descriptions()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt("Lava Beast", CharacterType.Elite,
            new List<PersonalityType> { PersonalityType.Aggressive, PersonalityType.Greedy },
            "en", 50, 80, 100, "HP 120");
        Assert.Contains("Aggressive", prompt);
        Assert.Contains("Greedy", prompt);
        Assert.Contains("fiery temper", prompt);
    }

    [Fact]
    public void System_prompt_contains_player_state()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt("Test", CharacterType.Normal,
            new List<PersonalityType> { PersonalityType.Calm, PersonalityType.Kind },
            "zh", 50, 80, 100, "HP 60");
        Assert.Contains("50/80", prompt);
        Assert.Contains("100", prompt);
    }

    [Fact]
    public void System_prompt_contains_json_format_instruction()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt("Test", CharacterType.Normal,
            new List<PersonalityType> { PersonalityType.Calm, PersonalityType.Kind },
            "zh", 50, 80, 100, "HP 60");
        Assert.Contains("dialogue", prompt);
        Assert.Contains("actions", prompt);
        Assert.Contains("end_conversation", prompt);
    }

    [Fact]
    public void System_prompt_contains_action_list()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt("Test", CharacterType.Normal,
            new List<PersonalityType> { PersonalityType.Calm, PersonalityType.Kind },
            "zh", 50, 80, 100, "HP 60");
        Assert.Contains("modify_player_hp", prompt);
        Assert.Contains("skip_event", prompt);
        Assert.Contains("steal_card", prompt);
    }

    [Fact]
    public void English_prompt_uses_english_language_tag()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt("Test", CharacterType.Normal,
            new List<PersonalityType> { PersonalityType.Calm, PersonalityType.Kind },
            "en", 50, 80, 100, "HP 60");
        Assert.Contains("English", prompt);
    }

    [Fact]
    public void BuildUserMessage_wraps_player_input()
    {
        var builder = new PromptBuilder();
        string msg = builder.BuildUserMessage("大哥饶命！", 2, 5);
        Assert.Contains("大哥饶命！", msg);
        Assert.Contains("2", msg);
        Assert.Contains("5", msg);
    }
}
