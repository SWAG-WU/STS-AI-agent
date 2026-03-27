using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using AIDialogueMod.Actions;
using AIDialogueMod.Config;
using AIDialogueMod.Personality;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

/// <summary>
/// Patches NCombatUi.Activate to inject a "Talk" button next to the End Turn button.
/// </summary>
[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
public static class CombatUIPatch
{
    public static void Postfix(NCombatUi __instance, CombatState state)
    {
        try
        {
            var config = ModConfig.Load();

            var dialogueBtn = new DialogueButton(config.Language);
            __instance.AddChild(dialogueBtn);

            // Position near the end-turn button area (bottom-right)
            dialogueBtn.AnchorLeft = 1f;
            dialogueBtn.AnchorTop = 1f;
            dialogueBtn.AnchorRight = 1f;
            dialogueBtn.AnchorBottom = 1f;
            dialogueBtn.OffsetLeft = -320f;
            dialogueBtn.OffsetTop = -80f;
            dialogueBtn.OffsetRight = -160f;
            dialogueBtn.OffsetBottom = -20f;

            dialogueBtn.Pressed += () => OnDialoguePressed(config, state);
            Log.Warn("[AIDialogueMod] Combat dialogue button injected.");
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] CombatUIPatch failed: {ex.Message}");
        }
    }

    private static void OnDialoguePressed(ModConfig config, CombatState state)
    {
        try
        {
            if (!config.IsConfigured)
            {
                var sceneTree = Engine.GetMainLoop() as SceneTree;
                sceneTree?.Root.AddChild(new ConfigPanel(config));
                return;
            }

            // Get player and enemy info from CombatState
            var players = state.GetCreaturesOnSide(CombatSide.Player);
            var enemies = state.GetCreaturesOnSide(CombatSide.Enemy);
            var player = players?.FirstOrDefault()?.Player;
            var mainEnemy = enemies?.FirstOrDefault();

            int playerHp = player?.Creature?.CurrentHp ?? 50;
            int playerMaxHp = player?.Creature?.MaxHp ?? 80;
            int playerGold = player?.Gold ?? 0;
            string enemyName = mainEnemy?.Name ?? "Unknown Enemy";
            string enemyInfo = mainEnemy != null ? $"HP {mainEnemy.CurrentHp}/{mainEnemy.MaxHp}" : "Unknown";

            // Determine character type
            CharacterType charType = CharacterType.Normal;
            // TODO: Detect elite/boss from encounter model if available

            var manager = new DialogueManager(config);
            var panel = new DialoguePanel();
            (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(panel);
            panel.Initialize(enemyName, DialogueManager.MaxRounds);

            var executor = new ActionExecutor(manager.StolenCards);

            panel.OnPlayerSubmit += async (text) =>
            {
                panel.AddPlayerMessage(text);
                panel.SetInputEnabled(false);
                panel.ShowTypingIndicator();
                int currentHp = player?.Creature?.CurrentHp ?? playerHp;
                int currentGold = player?.Gold ?? playerGold;
                await manager.SendPlayerMessage(text, currentHp, currentGold);
            };

            panel.OnAbandon += () => { manager.AbandonDialogue(); executor.OnEventEnd(config.Language); panel.Close(); };
            manager.OnNpcMessage += (dialogue, emotion) => { panel.AddNpcMessage(dialogue, emotion); panel.SetRound(manager.CurrentRound); panel.SetInputEnabled(true); };
            manager.OnActionsExecuted += (actions) => { foreach (var n in executor.Execute(actions, config.Language)) panel.AddActionNotification(n); };
            manager.OnWaitingForAI += () => { panel.ShowTypingIndicator(); panel.SetInputEnabled(false); };
            manager.OnConversationEnded += () => { executor.OnEventEnd(config.Language); panel.SetInputEnabled(false); };

            _ = manager.StartDialogue(enemyName, charType, config.Language, playerHp, playerMaxHp, playerGold, enemyInfo);
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] OnDialoguePressed failed: {ex.Message}"); }
    }
}
