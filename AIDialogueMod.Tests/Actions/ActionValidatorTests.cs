using AIDialogueMod.Actions;
using AIDialogueMod.AI;
using AIDialogueMod.Personality;
using System.Collections.Generic;
using Xunit;

namespace AIDialogueMod.Tests.Actions;

public class ActionValidatorTests
{
    private readonly List<PersonalityType> _normalPersonalities = new()
    {
        PersonalityType.Calm, PersonalityType.Greedy
    };

    [Fact]
    public void Unknown_action_type_is_filtered_out()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "unknown_action" },
            new() { Type = "no_action" }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Single(result);
        Assert.Equal("no_action", result[0].Type);
    }

    [Fact]
    public void Max_two_actions_per_round()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "modify_player_hp", Value = -5 },
            new() { Type = "modify_player_gold", Value = -10 },
            new() { Type = "modify_enemy_strength", Value = 3 },
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Skip_event_blocked_when_conversation_not_ending()
    {
        var actions = new List<GameAction> { new() { Type = "skip_event" } };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Empty(result);
    }

    [Fact]
    public void Skip_event_allowed_when_conversation_ending()
    {
        var actions = new List<GameAction> { new() { Type = "skip_event" } };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: true);
        Assert.Single(result);
    }

    [Fact]
    public void Give_relic_blocked_without_generous_personality()
    {
        var actions = new List<GameAction> { new() { Type = "give_relic", RelicId = "some_relic" } };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Empty(result);
    }

    [Fact]
    public void Give_relic_allowed_with_generous_personality()
    {
        var personalities = new List<PersonalityType> { PersonalityType.Generous, PersonalityType.Calm };
        var actions = new List<GameAction> { new() { Type = "give_relic", RelicId = "some_relic" } };
        var result = ActionValidator.Validate(actions, personalities, isConversationEnding: false);
        Assert.Single(result);
    }

    [Fact]
    public void Destroy_card_blocked_without_aggressive_or_cunning()
    {
        var actions = new List<GameAction> { new() { Type = "destroy_card", CardId = "strike" } };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Empty(result);
    }

    [Fact]
    public void Destroy_card_allowed_with_aggressive_personality()
    {
        var personalities = new List<PersonalityType> { PersonalityType.Aggressive, PersonalityType.Greedy };
        var actions = new List<GameAction> { new() { Type = "destroy_card", CardId = "strike" } };
        var result = ActionValidator.Validate(actions, personalities, isConversationEnding: false);
        Assert.Single(result);
    }

    [Fact]
    public void Hp_modification_clamped_to_minimum_1()
    {
        var actions = new List<GameAction> { new() { Type = "modify_player_hp", Value = -999 } };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false, playerHp: 10);
        Assert.Single(result);
        Assert.Equal(-9, result[0].Value);
    }

    [Fact]
    public void Gold_modification_clamped_to_zero()
    {
        var actions = new List<GameAction> { new() { Type = "modify_player_gold", Value = -500 } };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false, playerGold: 100);
        Assert.Single(result);
        Assert.Equal(-100, result[0].Value);
    }

    [Fact]
    public void No_action_always_passes()
    {
        var actions = new List<GameAction> { new() { Type = "no_action" } };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Single(result);
    }
}
