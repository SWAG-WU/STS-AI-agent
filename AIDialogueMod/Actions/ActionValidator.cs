using System;
using System.Collections.Generic;
using System.Linq;
using AIDialogueMod.Personality;

namespace AIDialogueMod.Actions;

public static class ActionValidator
{
    private const int MaxActionsPerRound = 2;

    public static List<GameAction> Validate(
        List<GameAction> actions,
        List<PersonalityType> personalities,
        bool isConversationEnding,
        int playerHp = int.MaxValue,
        int playerGold = int.MaxValue)
    {
        var validated = new List<GameAction>();
        foreach (var action in actions)
        {
            if (validated.Count >= MaxActionsPerRound) break;
            var parsedType = action.ParseType();
            if (parsedType == null) continue;
            if (!PassesRestrictions(parsedType.Value, personalities, isConversationEnding)) continue;
            ClampValues(action, parsedType.Value, playerHp, playerGold);
            validated.Add(action);
        }
        return validated;
    }

    private static bool PassesRestrictions(ActionType type, List<PersonalityType> personalities, bool isConversationEnding)
    {
        switch (type)
        {
            case ActionType.SkipEvent: return isConversationEnding;
            case ActionType.GiveRelic: return personalities.Contains(PersonalityType.Generous);
            case ActionType.DestroyCard: return personalities.Contains(PersonalityType.Aggressive) || personalities.Contains(PersonalityType.Cunning);
            default: return true;
        }
    }

    private static void ClampValues(GameAction action, ActionType type, int playerHp, int playerGold)
    {
        switch (type)
        {
            case ActionType.ModifyPlayerHp:
                if (action.Value < 0 && playerHp != int.MaxValue)
                    action.Value = Math.Max(action.Value, -(playerHp - 1));
                break;
            case ActionType.ModifyPlayerGold:
                if (action.Value < 0 && playerGold != int.MaxValue)
                    action.Value = Math.Max(action.Value, -playerGold);
                break;
            case ActionType.ShopDiscount:
                action.Percentage = Math.Clamp(action.Percentage, 0, 100);
                break;
        }
    }
}
