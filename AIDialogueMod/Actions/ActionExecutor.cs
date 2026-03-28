using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Rewards;

namespace AIDialogueMod.Actions;

public class ActionExecutor
{
    private readonly StolenCardManager _stolenCardManager;

    /// <summary>Card deletion/modification count per game session (destroy, steal, transform).</summary>
    private static int _cardModificationCount = 0;
    private const int MaxCardModifications = 5;

    public static int CardModificationCount => _cardModificationCount;
    public static int CardModificationsRemaining => MaxCardModifications - _cardModificationCount;

    /// <summary>Reset the card modification counter (call at new run start).</summary>
    public static void ResetCardModificationCount() => _cardModificationCount = 0;

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
        // Method 1: RunManager.DebugOnlyGetState() (works everywhere — events, shops, map, combat)
        try
        {
            var runState = RunManager.Instance?.DebugOnlyGetState();
            if (runState != null)
                return runState.Players?.FirstOrDefault();
        }
        catch { }

        // Method 2: CombatManager fallback (combat-only)
        try
        {
            var state = CombatManager.Instance?.DebugOnlyGetState();
            if (state != null)
            {
                var player = state.GetCreaturesOnSide(CombatSide.Player)?.FirstOrDefault()?.Player;
                if (player != null) return player;
            }
        }
        catch { }

        return null;
    }

    private Creature? GetMainEnemy()
    {
        try
        {
            var state = CombatManager.Instance?.DebugOnlyGetState();
            if (state != null)
                return state.GetCreaturesOnSide(CombatSide.Enemy)?.FirstOrDefault();
        }
        catch { }
        return null;
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
            ActionType.GiveCard => ExecuteGiveCard(action, isChinese),
            ActionType.DestroyCard => ExecuteDestroyCard(action, isChinese),
            ActionType.StealCard => ExecuteStealCard(action, isChinese),
            ActionType.ReturnCard => ExecuteReturnCard(action, isChinese),
            ActionType.TransformCard => ExecuteTransformCard(action, isChinese),
            ActionType.GiveRelic => ExecuteGiveRelic(action, isChinese),
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
                    player.Gold += a.Value;
                    Log.Warn($"[AIDialogueMod] Player gained {a.Value} gold (now {player.Gold})");
                }
                else if (a.Value < 0)
                {
                    int loss = Math.Min(-a.Value, player.Gold);
                    if (loss > 0)
                    {
                        player.Gold -= loss;
                        Log.Warn($"[AIDialogueMod] Player lost {loss} gold (now {player.Gold})");
                    }
                }
            }
            else
            {
                Log.Warn("[AIDialogueMod] ModifyPlayerGold: player not found");
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

    private string ExecuteGiveCard(GameAction a, bool zh)
    {
        try
        {
            var player = GetPlayer();
            if (player == null)
            {
                Log.Warn("[AIDialogueMod] GiveCard: player is null");
                return zh ? "获得卡牌失败" : "Failed to give card";
            }

            // Wish mode: player specified a card by name → search and give directly
            if (!string.IsNullOrEmpty(a.CardId))
                return ExecuteWishCard(a.CardId, player, zh);

            // Event mode: show native card reward UI (3 cards to choose from)
            return ExecuteCardReward(player, zh);
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] GiveCard failed: {ex.Message}");
            return zh ? "获得卡牌失败" : "Failed to give card";
        }
    }

    /// <summary>Approach B: Event-style — show the game's native card selection UI.</summary>
    private string ExecuteCardReward(Player player, bool zh)
    {
        try
        {
            var cardPool = player.Character?.CardPool;
            if (cardPool == null)
            {
                Log.Warn("[AIDialogueMod] ExecuteCardReward: card pool is null, trying ModelDb fallback");
                return ExecuteDirectCardGive(player, zh);
            }

            var options = new CardCreationOptions(
                new[] { cardPool },
                CardCreationSource.Other,
                CardRarityOddsType.RegularEncounter,
                null);

            var cardReward = new CardReward(options, 3, player);
            _ = RewardsCmd.OfferCustom(player, new List<Reward> { cardReward });
            Log.Warn("[AIDialogueMod] Offered card reward via native UI");
            return zh ? "请选择一张卡牌..." : "Choose a card...";
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] ExecuteCardReward failed: {ex.Message}, falling back to direct give");
            return ExecuteDirectCardGive(player, zh);
        }
    }

    /// <summary>Approach C: Wish mode — player specifies a card name, find it and give directly.</summary>
    private string ExecuteWishCard(string cardName, Player player, bool zh)
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            Log.Warn("[AIDialogueMod] ExecuteWishCard: runState is null");
            return zh ? $"获得卡牌失败" : "Failed to give card";
        }

        try
        {
            // Search all cards by title (display name) or internal ID
            CardModel? found = null;
            var allCards = ModelDb.AllCards;
            if (allCards != null)
            {
                foreach (var card in allCards)
                {
                    string? title = card.Title?.ToString();
                    string? entry = card.Id?.Entry;

                    if (string.Equals(title, cardName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entry, cardName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = card;
                        break;
                    }
                }
            }

            if (found == null)
            {
                Log.Warn($"[AIDialogueMod] ExecuteWishCard: card '{cardName}' not found");
                return zh ? $"未找到卡牌：{cardName}" : $"Card not found: {cardName}";
            }

            var mutableCard = runState.CreateCard(found, player);
            runState.AddCard(mutableCard, player);
            string name = mutableCard.Title?.ToString() ?? mutableCard.Id?.ToString() ?? cardName;
            Log.Warn($"[AIDialogueMod] WishCard gave: {name}");
            return zh ? $"许愿成功，获得卡牌：{name}" : $"Wish granted, received card: {name}";
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] ExecuteWishCard failed: {ex.Message}");
            return zh ? $"获得卡牌失败" : "Failed to give card";
        }
    }

    /// <summary>Fallback: directly pick a random card from ModelDb and add to deck.</summary>
    private string ExecuteDirectCardGive(Player player, bool zh)
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            Log.Warn("[AIDialogueMod] ExecuteDirectCardGive: runState is null");
            return zh ? "获得卡牌失败" : "Failed to give card";
        }

        try
        {
            // Try player's card pool first, then ModelDb
            var cardPool = player.Character?.CardPool;
            List<CardModel>? candidates = null;

            if (cardPool != null)
                candidates = cardPool.AllCards?.ToList();

            if (candidates == null || candidates.Count == 0)
                candidates = ModelDb.AllCards?.ToList();

            if (candidates != null && candidates.Count > 0)
            {
                var rng = new Random();
                var chosen = candidates[rng.Next(candidates.Count)];
                var mutableCard = runState.CreateCard(chosen, player);
                runState.AddCard(mutableCard, player);
                string name = mutableCard.Title?.ToString() ?? mutableCard.Id?.ToString() ?? "???";
                Log.Warn($"[AIDialogueMod] DirectCardGive: {name}");
                return zh ? $"获得卡牌：{name}" : $"Received card: {name}";
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] ExecuteDirectCardGive failed: {ex.Message}");
        }
        return zh ? "获得卡牌失败" : "Failed to give card";
    }
    private string ExecuteDestroyCard(GameAction a, bool zh)
    {
        if (_cardModificationCount >= MaxCardModifications)
            return zh ? "（**你的删改次数已达上限！**）" : "(**Your card modification limit has been reached!**)";

        try
        {
            var player = GetPlayer();
            if (player == null)
            {
                Log.Warn("[AIDialogueMod] DestroyCard: player is null");
                return zh ? "销毁卡牌失败" : "Failed to destroy card";
            }

            // If specific card requested, find and remove it directly
            if (!string.IsNullOrEmpty(a.CardId))
            {
                var card = FindCardInDeck(player, a.CardId);
                if (card != null)
                {
                    string name = card.Title?.ToString() ?? card.Id?.ToString() ?? a.CardId;
                    player.Deck.RemoveInternal(card, false);
                    RunManager.Instance?.DebugOnlyGetState()?.RemoveCard(card);
                    _cardModificationCount++;
                    Log.Warn($"[AIDialogueMod] Destroyed card: {name} (mod #{_cardModificationCount})");
                    return zh ? $"卡牌被永久销毁：{name}" : $"Card permanently destroyed: {name}";
                }
                Log.Warn($"[AIDialogueMod] DestroyCard: card '{a.CardId}' not found in deck");
            }

            // No specific card — show native card removal UI
            var reward = new CardRemovalReward(player);
            _ = RewardsCmd.OfferCustom(player, new List<Reward> { reward });
            _cardModificationCount++;
            Log.Warn($"[AIDialogueMod] Offered card removal reward via native UI (mod #{_cardModificationCount})");
            return zh ? "请选择一张卡牌销毁..." : "Choose a card to destroy...";
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] DestroyCard failed: {ex.Message}");
            return zh ? "销毁卡牌失败" : "Failed to destroy card";
        }
    }

    private string ExecuteStealCard(GameAction a, bool zh)
    {
        if (_cardModificationCount >= MaxCardModifications)
            return zh ? "（**你的删改次数已达上限！**）" : "(**Your card modification limit has been reached!**)";

        try
        {
            var player = GetPlayer();
            var runState = RunManager.Instance?.DebugOnlyGetState();
            if (player == null || runState == null)
            {
                Log.Warn("[AIDialogueMod] StealCard: player or runState is null");
                return zh ? "夺取卡牌失败" : "Failed to steal card";
            }

            CardModel? card = null;

            // Try to find specific card by name
            if (!string.IsNullOrEmpty(a.CardId))
                card = FindCardInDeck(player, a.CardId);

            // If not found, pick a random card from deck
            if (card == null)
            {
                var deck = player.Deck.Cards;
                if (deck.Count > 0)
                    card = deck[new Random().Next(deck.Count)];
            }

            if (card != null)
            {
                string name = card.Title?.ToString() ?? card.Id?.ToString() ?? "???";
                player.Deck.RemoveInternal(card, false);
                runState.RemoveCard(card);
                _stolenCardManager.StealCard(card);
                _cardModificationCount++;
                Log.Warn($"[AIDialogueMod] Stole card: {name} (mod #{_cardModificationCount})");
                return zh ? $"卡牌被夺取：{name}（事件结束后归还）" : $"Card stolen: {name} (returned after event)";
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] StealCard failed: {ex.Message}");
        }
        return zh ? "夺取卡牌失败" : "Failed to steal card";
    }

    private string ExecuteReturnCard(GameAction a, bool zh)
    {
        try
        {
            var player = GetPlayer();
            var runState = RunManager.Instance?.DebugOnlyGetState();
            if (player == null || runState == null)
            {
                Log.Warn("[AIDialogueMod] ReturnCard: player or runState is null");
                return zh ? "归还卡牌失败" : "Failed to return card";
            }

            var stolen = _stolenCardManager.GetStolenCards();
            if (stolen.Count > 0)
            {
                var card = stolen[0];
                string name = card.Title?.ToString() ?? card.Id?.ToString() ?? "???";
                runState.AddCard(card, player);
                player.Deck.AddInternal(card, -1, false);
                _stolenCardManager.RemoveCard(card);
                Log.Warn($"[AIDialogueMod] Returned stolen card: {name}");
                return zh ? $"卡牌已归还：{name}" : $"Card returned: {name}";
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] ReturnCard failed: {ex.Message}");
        }
        return zh ? "没有需要归还的卡牌" : "No cards to return";
    }

    private string ExecuteTransformCard(GameAction a, bool zh)
    {
        if (_cardModificationCount >= MaxCardModifications)
            return zh ? "（**你的删改次数已达上限！**）" : "(**Your card modification limit has been reached!**)";

        try
        {
            var player = GetPlayer();
            if (player == null)
            {
                Log.Warn("[AIDialogueMod] TransformCard: player is null");
                return zh ? "变换卡牌失败" : "Failed to transform card";
            }

            // If specific card given, find it; otherwise pick from deck
            CardModel? card = null;
            if (!string.IsNullOrEmpty(a.CardId))
                card = FindCardInDeck(player, a.CardId);

            if (card != null)
            {
                // Use game's native transform command (shows UI + animation)
                _ = CardCmd.TransformToRandom(card, null, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.EventLayout);
                string name = card.Title?.ToString() ?? card.Id?.ToString() ?? "???";
                Log.Warn($"[AIDialogueMod] Transforming card: {name}");
                return zh ? $"卡牌正在变换：{name}" : $"Transforming card: {name}";
            }

            // No specific card — show deck transform selection screen
            Log.Warn("[AIDialogueMod] TransformCard: no card specified, falling back to random deck transform");
            return ExecuteCardReward(player, zh);
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] TransformCard failed: {ex.Message}");
            return zh ? "变换卡牌失败" : "Failed to transform card";
        }
    }

    /// <summary>Find a card in the player's deck by title or internal ID.</summary>
    private CardModel? FindCardInDeck(Player player, string cardName)
    {
        foreach (var card in player.Deck.Cards)
        {
            string? title = card.Title?.ToString();
            string? entry = card.Id?.Entry;
            if (string.Equals(title, cardName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry, cardName, StringComparison.OrdinalIgnoreCase))
                return card;
        }
        return null;
    }

    private string ExecuteGiveRelic(GameAction a, bool zh)
    {
        string fallbackName = a.RelicId ?? (zh ? "随机遗物" : "random relic");
        try
        {
            var player = GetPlayer();
            if (player != null)
            {
                // Use the game's own factory method — it handles grab bag population and RNG internally
                var relic = RelicFactory.PullNextRelicFromFront(player);
                if (relic != null)
                {
                    _ = RelicCmd.Obtain(relic, player, -1);
                    string relicName = relic.Title?.ToString() ?? relic.Id?.ToString() ?? fallbackName;
                    Log.Warn($"[AIDialogueMod] Gave relic to player: {relicName}");
                    return zh ? $"获得遗物：{relicName}" : $"Received relic: {relicName}";
                }
                Log.Warn("[AIDialogueMod] RelicFactory.PullNextRelicFromFront returned null");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] GiveRelic failed: {ex.Message}");
        }
        return zh ? $"获得遗物：{fallbackName}" : $"Received relic: {fallbackName}";
    }

    public List<string> OnEventEnd(string language)
    {
        var returned = _stolenCardManager.ClearAndReturnAll();
        var notifications = new List<string>();
        bool zh = language != "en";

        var player = GetPlayer();
        var runState = RunManager.Instance?.DebugOnlyGetState();

        foreach (var card in returned)
        {
            string name = card.Title?.ToString() ?? card.Id?.ToString() ?? "???";
            if (player != null && runState != null)
            {
                try
                {
                    runState.AddCard(card, player);
                    player.Deck.AddInternal(card, -1, false);
                    Log.Warn($"[AIDialogueMod] Returned stolen card on event end: {name}");
                }
                catch (Exception ex)
                {
                    Log.Warn($"[AIDialogueMod] Failed to return card {name}: {ex.Message}");
                }
            }
            notifications.Add(zh ? $"被偷的卡牌已归还：{name}" : $"Stolen card returned: {name}");
        }
        return notifications;
    }
}
