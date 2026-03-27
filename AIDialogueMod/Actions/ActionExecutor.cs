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

    /// <summary>
    /// Registry mapping buff/debuff string IDs to a function that applies the power.
    /// Each entry is: (buffId) -> Action(creature, stacks) that calls PowerCmd.Apply.
    /// </summary>
    private static readonly Dictionary<string, Action<Creature, int>> PowerRegistry = new(StringComparer.OrdinalIgnoreCase)
    {
        // Buffs
        ["strength"]      = (c, s) => _ = PowerCmd.Apply<StrengthPower>(c, s, c, null, false),
        ["dexterity"]     = (c, s) => _ = PowerCmd.Apply<DexterityPower>(c, s, c, null, false),
        ["artifact"]      = (c, s) => _ = PowerCmd.Apply<ArtifactPower>(c, s, c, null, false),
        ["regen"]         = (c, s) => _ = PowerCmd.Apply<RegenPower>(c, s, c, null, false),
        ["thorns"]        = (c, s) => _ = PowerCmd.Apply<ThornsPower>(c, s, c, null, false),
        ["vigor"]         = (c, s) => _ = PowerCmd.Apply<VigorPower>(c, s, c, null, false),
        ["plating"]       = (c, s) => _ = PowerCmd.Apply<PlatingPower>(c, s, c, null, false),
        ["intangible"]    = (c, s) => _ = PowerCmd.Apply<IntangiblePower>(c, s, c, null, false),
        ["barricade"]     = (c, s) => _ = PowerCmd.Apply<BarricadePower>(c, s, c, null, false),
        ["ritual"]        = (c, s) => _ = PowerCmd.Apply<RitualPower>(c, s, c, null, false),
        ["rage"]          = (c, s) => _ = PowerCmd.Apply<RagePower>(c, s, c, null, false),
        ["enrage"]        = (c, s) => _ = PowerCmd.Apply<EnragePower>(c, s, c, null, false),
        ["focus"]         = (c, s) => _ = PowerCmd.Apply<FocusPower>(c, s, c, null, false),
        ["buffer"]        = (c, s) => _ = PowerCmd.Apply<BufferPower>(c, s, c, null, false),

        // Debuffs
        ["vulnerable"]    = (c, s) => _ = PowerCmd.Apply<VulnerablePower>(c, s, c, null, false),
        ["weak"]          = (c, s) => _ = PowerCmd.Apply<WeakPower>(c, s, c, null, false),
        ["frail"]         = (c, s) => _ = PowerCmd.Apply<FrailPower>(c, s, c, null, false),
        ["poison"]        = (c, s) => _ = PowerCmd.Apply<PoisonPower>(c, s, c, null, false),
        ["constrict"]     = (c, s) => _ = PowerCmd.Apply<ConstrictPower>(c, s, c, null, false),
        ["slow"]          = (c, s) => _ = PowerCmd.Apply<SlowPower>(c, s, c, null, false),
        ["no_draw"]       = (c, s) => _ = PowerCmd.Apply<NoDrawPower>(c, s, c, null, false),
        ["no_block"]      = (c, s) => _ = PowerCmd.Apply<NoBlockPower>(c, s, c, null, false),
        ["hex"]           = (c, s) => _ = PowerCmd.Apply<HexPower>(c, s, c, null, false),
        ["confused"]      = (c, s) => _ = PowerCmd.Apply<ConfusedPower>(c, s, c, null, false),
    };

    /// <summary>Chinese display names for known power IDs.</summary>
    private static readonly Dictionary<string, string> PowerNamesCN = new(StringComparer.OrdinalIgnoreCase)
    {
        ["strength"] = "力量", ["dexterity"] = "敏捷", ["artifact"] = "人工制品",
        ["regen"] = "再生", ["thorns"] = "荆棘", ["vigor"] = "活力",
        ["plating"] = "甲板", ["intangible"] = "无实体", ["barricade"] = "壁垒",
        ["ritual"] = "仪式", ["rage"] = "愤怒", ["enrage"] = "暴怒",
        ["focus"] = "集中", ["buffer"] = "缓冲",
        ["vulnerable"] = "易伤", ["weak"] = "虚弱", ["frail"] = "脆弱",
        ["poison"] = "中毒", ["constrict"] = "缠绕", ["slow"] = "减速",
        ["no_draw"] = "禁止抽牌", ["no_block"] = "禁止格挡", ["hex"] = "诅咒",
        ["confused"] = "混乱",
    };

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
            ActionType.AddPlayerBuff => ExecutePower(action, isPlayer: true, isBuff: true, isChinese),
            ActionType.AddPlayerDebuff => ExecutePower(action, isPlayer: true, isBuff: false, isChinese),
            ActionType.AddEnemyBuff => ExecutePower(action, isPlayer: false, isBuff: true, isChinese),
            ActionType.AddEnemyDebuff => ExecutePower(action, isPlayer: false, isBuff: false, isChinese),
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
                    if (loss > player.Gold) loss = player.Gold;
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

    /// <summary>
    /// Apply a buff or debuff using the game's native PowerCmd system.
    /// Maps buff_id/debuff_id strings to real Power types.
    /// </summary>
    private string ExecutePower(GameAction a, bool isPlayer, bool isBuff, bool zh)
    {
        string id = isBuff ? (a.BuffId ?? "unknown") : (a.DebuffId ?? "unknown");
        int stacks = Math.Max(1, a.Stacks);
        string targetLabel = isPlayer ? (zh ? "玩家" : "Player") : (zh ? "怪物" : "Enemy");
        string effectLabel = isBuff ? (zh ? "增益" : "buff") : (zh ? "减益" : "debuff");
        string displayName = PowerNamesCN.TryGetValue(id, out var cn) && zh ? cn : id;

        try
        {
            Creature? target = isPlayer ? GetPlayer()?.Creature : GetMainEnemy();
            if (target != null && PowerRegistry.TryGetValue(id, out var applyPower))
            {
                applyPower(target, stacks);
                Log.Warn($"[AIDialogueMod] Applied {id} x{stacks} to {(isPlayer ? "player" : "enemy")}");
            }
            else if (target == null)
            {
                Log.Warn($"[AIDialogueMod] Cannot apply {id}: target not found");
            }
            else
            {
                Log.Warn($"[AIDialogueMod] Unknown power ID: {id}, skipping application");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] ExecutePower failed for {id}: {ex.Message}");
        }

        return zh
            ? $"{targetLabel}获得{effectLabel}：{displayName} x{stacks}"
            : $"{targetLabel} gained {effectLabel}: {id} x{stacks}";
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
