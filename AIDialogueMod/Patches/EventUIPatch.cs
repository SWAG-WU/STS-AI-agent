using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using AIDialogueMod.Config;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

// [HarmonyPatch(typeof(TARGET_EVENT_UI_CLASS), "TARGET_METHOD")]
public static class EventUIPatch
{
    public static void Postfix(object __instance)
    {
        try
        {
            var config = ModConfig.Load();
            if (!config.IsConfigured) return;
            if (__instance is not Node uiRoot) return;
            var dialogueBtn = new DialogueButton(config.Language);
            dialogueBtn.Text = config.Language == "en" ? "Try talking..." : "尝试对话...";
            // TODO: Add to event option list, wire up with CharacterType.EventNpc
            Log.Warn("[AIDialogueMod] Event dialogue option injected.");
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] EventUIPatch failed: {ex.Message}"); }
    }
}
