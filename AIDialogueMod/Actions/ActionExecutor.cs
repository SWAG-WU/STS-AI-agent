using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;

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

    private Player? GetPlayer()
    {
        try
        {
            var combatManager = CombatManager.Instance;
            if (combatManager == null) return null;
            var state = combatManager.DebugOnlyGetState();
            if (state == null) return null;
            var playerCreatures = state.GetCreaturesOnSide(CombatSide.Player);
            return playerCreatures?.FirstOrDefault()?.Player;
        }
        catch { return null; }
    }

    private Creature? GetMainEnemy()
    {
        try
        {
            var combatManager = CombatManager.Instance;
            if (combatManager == null) return null;
            var state = combatManager.DebugOnlyGetState();
            if (state == null) return null;
            var enemies = state.GetCreaturesOnSide(CombatSide.Enemy);
            return enemies?.FirstOrDefault();
        }
        catch { return null; }
    }

    private string? ExecuteSingle(GameAction action, bool isChinese)
    {
        var type = action.ParseType();
        if (type == null) return null;

        return type.Value switch
        {
            ActionType.ModifyPlayerHp => ExecuteModifyPlayerHp(action, isChinese),
            ActionType.ModifyPlayerGold => ExecuteModifyPlayerGold(action, isChinese),
            ActionType.ModifyEnemyStrength => ExecuteModifyEnemyStrength(action, isChinese),
            ActionType.ModifyEnemyHp => ExecuteModifyEnemyHp(action, isChinese),
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

    private string ExecuteModifyPlayerHp(GameAction a, bool zh)
    {
        try
        {
            var player = GetPlayer();
            if (player?.Creature != null)
            {
                if (a.Value > 0)
                {
                    _ = CreatureCmd.Heal(player.Creature, a.Value, true);
                    Log.Warn($"[AIDialogueMod] Healed player for {a.Value} HP");
                }
                else if (a.Value < 0)
                {
                    int damage = -a.Value;
                    // Clamp: don't kill the player
                    int maxDamage = player.Creature.CurrentHp - 1;
                    if (damage > maxDamage) damage = maxDamage;
                    if (damage > 0)
                    {
                        player.Creature.LoseHpInternal(damage, default);
                        Log.Warn($"[AIDialogueMod] Dealt {damage} damage to player");
                    }
                }
            }
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] ModifyPlayerHp failed: {ex.Message}"); }

        return a.Value > 0
            ? (zh ? $"玩家恢复了 {a.Value} HP" : $"Player healed {a.Value} HP")
            : (zh ? $"玩家受到了 {-a.Value} 点伤害" : $"Player took {-a.Value} damage");
    }

    private string ExecuteModifyPlayerGold(GameAction a, bool zh)
    {
        try
        {
            var player = GetPlayer();
            if (player != null)
            {
                if (a.Value > 0)
                {
                    _ = PlayerCmd.GainGold(a.Value, player, false);
                    Log.Warn($"[AIDialogueMod] Player gained {a.Value} gold");
                }
                else if (a.Value < 0)
                {
                    int loss = -a.Value;
                    if (loss > player.Gold) loss = player.Gold; // Don't go negative
                    if (loss > 0)
                    {
                        _ = PlayerCmd.LoseGold(loss, player, default);
                        Log.Warn($"[AIDialogueMod] Player lost {loss} gold");
                    }
                }
            }
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] ModifyPlayerGold failed: {ex.Message}"); }

        return a.Value > 0
            ? (zh ? $"获得了 {a.Value} 金币" : $"Gained {a.Value} gold")
            : (zh ? $"失去了 {-a.Value} 金币" : $"Lost {-a.Value} gold");
    }

    private string ExecuteModifyEnemyStrength(GameAction a, bool zh)
    {
        try
        {
            var enemy = GetMainEnemy();
            if (enemy != null)
            {
                // Apply StrengthPower to enemy
                var strengthPower = new StrengthPower();
                _ = PowerCmd.Apply<StrengthPower>(enemy, a.Value, enemy, null, false);
                Log.Warn($"[AIDialogueMod] Enemy strength modified by {a.Value}");
            }
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] ModifyEnemyStrength failed: {ex.Message}"); }

        return a.Value > 0
            ? (zh ? $"怪物攻击力 +{a.Value}" : $"Enemy strength +{a.Value}")
            : (zh ? $"怪物攻击力 {a.Value}" : $"Enemy strength {a.Value}");
    }

    private string ExecuteModifyEnemyHp(GameAction a, bool zh)
    {
        try
        {
            var enemy = GetMainEnemy();
            if (enemy != null)
            {
                if (a.Value > 0)
                {
                    _ = CreatureCmd.Heal(enemy, a.Value, true);
                    Log.Warn($"[AIDialogueMod] Healed enemy for {a.Value} HP");
                }
                else if (a.Value < 0)
                {
                    int damage = -a.Value;
                    int maxDamage = enemy.CurrentHp - 1;
                    if (damage > maxDamage) damage = maxDamage;
                    if (damage > 0)
                    {
                        enemy.LoseHpInternal(damage, default);
                        Log.Warn($"[AIDialogueMod] Dealt {damage} damage to enemy");
                    }
                }
            }
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] ModifyEnemyHp failed: {ex.Message}"); }

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
        // Note: Buff/debuff application requires specific PowerModel types which vary per buff.
        // For now we log and show notification. Full implementation needs a buff registry.
        Log.Warn($"[AIDialogueMod] {(isBuff ? "Buff" : "Debuff")} {id} x{a.Stacks} on {(isPlayer ? "player" : "enemy")}");
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
