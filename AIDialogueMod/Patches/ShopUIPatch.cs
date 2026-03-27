using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using AIDialogueMod.Actions;
using AIDialogueMod.Config;
using AIDialogueMod.Personality;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

[HarmonyPatch(typeof(NMerchantRoom), nameof(NMerchantRoom._Ready))]
public static class ShopUIPatch
{
    public static void Postfix(NMerchantRoom __instance)
    {
        try
        {
            DialogueSessionCache.ClearAll();

            var config = ModConfig.Load();
            var dialogueBtn = new DialogueButton(config.Language);
            __instance.AddChild(dialogueBtn);

            dialogueBtn.AnchorLeft = 0.5f;
            dialogueBtn.AnchorTop = 0f;
            dialogueBtn.AnchorRight = 0.5f;
            dialogueBtn.AnchorBottom = 0f;
            dialogueBtn.OffsetLeft = -80f;
            dialogueBtn.OffsetTop = 10f;
            dialogueBtn.OffsetRight = 80f;
            dialogueBtn.OffsetBottom = 50f;

            dialogueBtn.Pressed += () => OnDialoguePressed(config);
            Log.Warn("[AIDialogueMod] Shop dialogue button injected.");
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] ShopUIPatch failed: {ex.Message}"); }
    }

    private static Player? GetPlayer()
    {
        try
        {
            var cm = CombatManager.Instance;
            if (cm == null) return null;
            var state = cm.DebugOnlyGetState();
            if (state == null) return null;
            return state.GetCreaturesOnSide(CombatSide.Player)?.FirstOrDefault()?.Player;
        }
        catch { return null; }
    }

    private static void OnDialoguePressed(ModConfig config)
    {
        try
        {
            if (DialogueSessionCache.IsPanelOpen) return;

            if (!config.IsConfigured)
            {
                (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(new ConfigPanel(config));
                return;
            }

            string merchantName = config.Language == "en" ? "Merchant" : "商人";
            var player = GetPlayer();
            int playerHp = player?.Creature?.CurrentHp ?? 50;
            int playerMaxHp = player?.Creature?.MaxHp ?? 80;
            int playerGold = player?.Gold ?? 100;

            string sessionKey = "shop_merchant";
            var (manager, isNew) = DialogueSessionCache.GetOrCreate(sessionKey, config);

            manager.ClearEventHandlers();

            var panel = new DialoguePanel();
            (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(panel);
            panel.Initialize(merchantName, DialogueManager.MaxRounds);

            var executor = new ActionExecutor(manager.StolenCards);
            DialogueSessionCache.MarkPanelOpen();

            panel.OnPlayerSubmit += async (text) =>
            {
                panel.AddPlayerMessage(text);
                panel.SetInputEnabled(false);
                panel.ShowTypingIndicator();
                var p = GetPlayer();
                await manager.SendPlayerMessage(text, p?.Creature?.CurrentHp ?? playerHp, p?.Gold ?? playerGold);
            };

            panel.OnAbandon += () =>
            {
                manager.AbandonDialogue();
                executor.OnEventEnd(config.Language);
                DialogueSessionCache.MarkPanelClosed();
                panel.Close();
            };

            manager.OnNpcMessage += (dialogue, emotion) =>
            {
                panel.AddNpcMessage(dialogue, emotion);
                panel.SetRound(manager.CurrentRound);
                panel.SetInputEnabled(true);
            };
            manager.OnReplayPlayerMessage += (text) => panel.AddPlayerMessage(text);
            manager.OnActionsExecuted += (actions) =>
            {
                foreach (var n in executor.Execute(actions, config.Language))
                    panel.AddActionNotification(n);
            };
            manager.OnWaitingForAI += () => { panel.ShowTypingIndicator(); panel.SetInputEnabled(false); };
            manager.OnConversationEnded += () =>
            {
                executor.OnEventEnd(config.Language);
                panel.SetInputEnabled(false);
                DialogueSessionCache.MarkPanelClosed();
            };

            if (isNew)
                _ = manager.StartDialogue(merchantName, CharacterType.Merchant, config.Language, playerHp, playerMaxHp, playerGold, "Merchant Shop");
            else
            {
                panel.SetRound(manager.CurrentRound);
                manager.ResumeDialogue();
            }
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] Shop OnDialoguePressed failed: {ex.Message}"); }
    }
}
