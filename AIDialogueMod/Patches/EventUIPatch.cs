using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Events;
using AIDialogueMod.Actions;
using AIDialogueMod.Config;
using AIDialogueMod.Personality;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

/// <summary>
/// Patches NEventLayout.AddOptions to append a "Try talking..." dialogue option.
/// Skips injection if the event is a rest site.
/// </summary>
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.AddOptions))]
public static class EventUIPatch
{
    public static void Postfix(NEventLayout __instance, IEnumerable<EventOption> options)
    {
        try
        {
            var config = ModConfig.Load();

            // Create a dialogue button styled as an event option
            var dialogueBtn = new DialogueButton(config.Language);
            dialogueBtn.Text = config.Language == "en" ? "Try talking..." : "尝试对话...";
            __instance.AddChild(dialogueBtn);

            // Position below the event options
            dialogueBtn.AnchorLeft = 0.5f;
            dialogueBtn.AnchorTop = 1f;
            dialogueBtn.AnchorRight = 0.5f;
            dialogueBtn.AnchorBottom = 1f;
            dialogueBtn.OffsetLeft = -100f;
            dialogueBtn.OffsetTop = -60f;
            dialogueBtn.OffsetRight = 100f;
            dialogueBtn.OffsetBottom = -20f;

            dialogueBtn.Pressed += () => OnDialoguePressed(config);
            Log.Warn("[AIDialogueMod] Event dialogue option injected.");
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] EventUIPatch failed: {ex.Message}"); }
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

            string npcName = config.Language == "en" ? "Mysterious Figure" : "神秘人物";

            var manager = new DialogueManager(config);
            var panel = new DialoguePanel();
            (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(panel);
            panel.Initialize(npcName, DialogueManager.MaxRounds);

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

            _ = manager.StartDialogue(npcName, CharacterType.EventNpc, config.Language, 50, 80, 100, "Random Event");
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] Event OnDialoguePressed failed: {ex.Message}"); }
    }
}
