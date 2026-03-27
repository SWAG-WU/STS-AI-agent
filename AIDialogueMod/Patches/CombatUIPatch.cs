using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using AIDialogueMod.Config;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

// TODO: Replace HarmonyPatch target after inspecting sts2.dll
// [HarmonyPatch(typeof(TARGET_COMBAT_UI_CLASS), "TARGET_METHOD")]
public static class CombatUIPatch
{
    public static void Postfix(object __instance)
    {
        try
        {
            var config = ModConfig.Load();
            if (!config.IsConfigured) return;
            if (__instance is not Node uiRoot) return;

            var dialogueBtn = new DialogueButton(config.Language);
            dialogueBtn.Pressed += () => OnDialoguePressed(config);
            // TODO: Add button to correct parent node
            Log.Warn("[AIDialogueMod] Combat dialogue button injected.");
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] CombatUIPatch failed: {ex.Message}");
        }
    }

    private static void OnDialoguePressed(ModConfig config)
    {
        try
        {
            if (!config.IsConfigured)
            {
                var sceneTree = Engine.GetMainLoop() as SceneTree;
                sceneTree?.Root.AddChild(new ConfigPanel(config));
                return;
            }

            var manager = new DialogueManager(config);
            var panel = new DialoguePanel();
            (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(panel);
            panel.Initialize("Unknown Enemy", DialogueManager.MaxRounds);

            var executor = new Actions.ActionExecutor(manager.StolenCards);

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

            _ = manager.StartDialogue("Unknown Enemy", Personality.CharacterType.Normal, config.Language, 50, 80, 100, "HP 60");
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] OnDialoguePressed failed: {ex.Message}"); }
    }
}
