using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using AIDialogueMod.Actions;
using AIDialogueMod.Config;
using AIDialogueMod.Personality;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

/// <summary>
/// Patches NMerchantRoom._Ready to inject a "Talk" button in the shop.
/// </summary>
[HarmonyPatch(typeof(NMerchantRoom), nameof(NMerchantRoom._Ready))]
public static class ShopUIPatch
{
    public static void Postfix(NMerchantRoom __instance)
    {
        try
        {
            var config = ModConfig.Load();

            var dialogueBtn = new DialogueButton(config.Language);
            __instance.AddChild(dialogueBtn);

            // Position at the top of the shop
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

    private static void OnDialoguePressed(ModConfig config)
    {
        try
        {
            if (!config.IsConfigured)
            {
                (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(new ConfigPanel(config));
                return;
            }

            string merchantName = config.Language == "en" ? "Merchant" : "商人";

            var manager = new DialogueManager(config);
            var panel = new DialoguePanel();
            (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(panel);
            panel.Initialize(merchantName, DialogueManager.MaxRounds);

            var executor = new ActionExecutor(manager.StolenCards);

            panel.OnPlayerSubmit += async (text) =>
            {
                panel.AddPlayerMessage(text);
                panel.SetInputEnabled(false);
                panel.ShowTypingIndicator();
                await manager.SendPlayerMessage(text, playerHp: 50, playerGold: 100);
            };

            panel.OnAbandon += () => { manager.AbandonDialogue(); executor.OnEventEnd(config.Language); panel.Close(); };
            manager.OnNpcMessage += (dialogue, emotion) => { panel.AddNpcMessage(dialogue, emotion); panel.SetRound(manager.CurrentRound); panel.SetInputEnabled(true); };
            manager.OnActionsExecuted += (actions) => { foreach (var n in executor.Execute(actions, config.Language)) panel.AddActionNotification(n); };
            manager.OnWaitingForAI += () => { panel.ShowTypingIndicator(); panel.SetInputEnabled(false); };
            manager.OnConversationEnded += () => { executor.OnEventEnd(config.Language); panel.SetInputEnabled(false); };

            _ = manager.StartDialogue(merchantName, CharacterType.Merchant, config.Language, 50, 80, 100, "Merchant Shop");
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] Shop OnDialoguePressed failed: {ex.Message}"); }
    }
}
