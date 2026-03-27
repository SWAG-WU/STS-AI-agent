using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;

namespace AIDialogueMod.Actions;

public class ActionExecutor
{
    private readonly StolenCardManager _stolenCardManager;

    public ActionExecutor(StolenCardManager stolenCardManager)
    {
        _stolenCardManager = stolenCardManager;
    }

    public List<string> Execute(List<GameAction> actions, string language)
    {
        var notifications = new List<string>();
        bool isChinese = language != "en";
        foreach (var action in actions)
        {
            try
            {
                string? notification = ExecuteSingle(action, isChinese);
                if (notification != null) notifications.Add(notification);
            }
            catch (Exception ex)
            {
                Log.Warn($"[AIDialogueMod] Action execution failed for {action.Type}: {ex.Message}");
            }
        }
        return notifications;
    }

    private string? ExecuteSingle(GameAction action, bool isChinese)
    {
        var type = action.ParseType();
        if (type == null) return null;

        return type.Value switch
        {
            ActionType.ModifyPlayerHp => FormatHp(action, isChinese),
            ActionType.ModifyPlayerGold => FormatGold(action, isChinese),
            ActionType.ModifyEnemyStrength => FormatEnemyStrength(action, isChinese),
            ActionType.ModifyEnemyHp => FormatEnemyHp(action, isChinese),
            ActionType.AddPlayerBuff => FormatBuff(action, true, true, isChinese),
            ActionType.AddPlayerDebuff => FormatBuff(action, true, false, isChinese),
            ActionType.AddEnemyBuff => FormatBuff(action, false, true, isChinese),
            ActionType.AddEnemyDebuff => FormatBuff(action, false, false, isChinese),
            ActionType.GiveCard => FormatGiveCard(action, isChinese),
            ActionType.DestroyCard => FormatDestroyCard(action, isChinese),
            ActionType.StealCard => FormatStealCard(action, isChinese),
            ActionType.ReturnCard => FormatReturnCard(action, isChinese),
            ActionType.GiveRelic => FormatGiveRelic(action, isChinese),
            ActionType.SkipEvent => isChinese ? "事件被跳过！" : "Event skipped!",
            ActionType.ShopDiscount => isChinese ? $"商店全场 {action.Percentage}% 折扣！" : $"Shop {action.Percentage}% discount!",
            ActionType.NoAction => null,
            _ => null,
        };
    }

    private string FormatHp(GameAction a, bool zh)
    {
        Log.Warn($"[AIDialogueMod] ModifyPlayerHp: {a.Value}");
        return a.Value > 0
            ? (zh ? $"玩家恢复了 {a.Value} HP" : $"Player healed {a.Value} HP")
            : (zh ? $"玩家受到了 {-a.Value} 点伤害" : $"Player took {-a.Value} damage");
    }

    private string FormatGold(GameAction a, bool zh)
    {
        Log.Warn($"[AIDialogueMod] ModifyPlayerGold: {a.Value}");
        return a.Value > 0
            ? (zh ? $"获得了 {a.Value} 金币" : $"Gained {a.Value} gold")
            : (zh ? $"失去了 {-a.Value} 金币" : $"Lost {-a.Value} gold");
    }

    private string FormatEnemyStrength(GameAction a, bool zh)
    {
        Log.Warn($"[AIDialogueMod] ModifyEnemyStrength: {a.Value}");
        return a.Value > 0
            ? (zh ? $"怪物攻击力 +{a.Value}" : $"Enemy strength +{a.Value}")
            : (zh ? $"怪物攻击力 {a.Value}" : $"Enemy strength {a.Value}");
    }

    private string FormatEnemyHp(GameAction a, bool zh)
    {
        Log.Warn($"[AIDialogueMod] ModifyEnemyHp: {a.Value}");
        return a.Value > 0
            ? (zh ? $"怪物恢复了 {a.Value} HP" : $"Enemy healed {a.Value} HP")
            : (zh ? $"怪物受到了 {-a.Value} 点伤害" : $"Enemy took {-a.Value} damage");
    }

    private string FormatBuff(GameAction a, bool isPlayer, bool isBuff, bool zh)
    {
        string id = isBuff ? (a.BuffId ?? "unknown") : (a.DebuffId ?? "unknown");
        string target = isPlayer ? (zh ? "玩家" : "Player") : (zh ? "怪物" : "Enemy");
        string effect = isBuff ? (zh ? "增益" : "buff") : (zh ? "减益" : "debuff");
        string dur = FormatDuration(a.Duration, zh);
        return zh ? $"{target}获得{effect}：{id} x{a.Stacks}（{dur}）" : $"{target} gained {effect}: {id} x{a.Stacks} ({dur})";
    }

    private string FormatGiveCard(GameAction a, bool zh) => zh ? $"获得卡牌：{a.CardId ?? "unknown"}" : $"Received card: {a.CardId ?? "unknown"}";
    private string FormatDestroyCard(GameAction a, bool zh) => zh ? $"卡牌被永久销毁：{a.CardId ?? "random"}" : $"Card permanently destroyed: {a.CardId ?? "random"}";

    private string FormatStealCard(GameAction a, bool zh)
    {
        string cardId = a.CardId ?? "random";
        _stolenCardManager.StealCard(cardId);
        return zh ? $"卡牌被暂时夺取：{cardId}（事件结束后归还）" : $"Card temporarily stolen: {cardId} (returned after event)";
    }

    private string FormatReturnCard(GameAction a, bool zh)
    {
        string cardId = a.CardId ?? "";
        _stolenCardManager.ReturnCard(cardId);
        return zh ? $"卡牌已归还：{cardId}" : $"Card returned: {cardId}";
    }

    private string FormatGiveRelic(GameAction a, bool zh) => zh ? $"获得遗物：{a.RelicId ?? "unknown"}" : $"Received relic: {a.RelicId ?? "unknown"}";

    private static string FormatDuration(string duration, bool zh)
    {
        if (duration == "combat") return zh ? "本场战斗" : "this combat";
        if (duration == "permanent") return zh ? "永久" : "permanent";
        if (duration.StartsWith("turns:") && int.TryParse(duration[6..], out int turns))
            return zh ? $"{turns}回合" : $"{turns} turns";
        return duration;
    }

    public List<string> OnEventEnd(string language)
    {
        var returned = _stolenCardManager.ClearAndReturnAll();
        var notifications = new List<string>();
        bool zh = language != "en";
        foreach (var cardId in returned)
            notifications.Add(zh ? $"被偷的卡牌已归还：{cardId}" : $"Stolen card returned: {cardId}");
        return notifications;
    }
}
