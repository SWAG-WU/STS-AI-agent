using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using AIDialogueMod.Config;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

// [HarmonyPatch(typeof(TARGET_SHOP_UI_CLASS), "TARGET_METHOD")]
public static class ShopUIPatch
{
    public static void Postfix(object __instance)
    {
        try
        {
            var config = ModConfig.Load();
            if (!config.IsConfigured) return;
            if (__instance is not Node uiRoot) return;
            var dialogueBtn = new DialogueButton(config.Language);
            // TODO: Add to correct position in shop UI, wire up with CharacterType.Merchant
            Log.Warn("[AIDialogueMod] Shop dialogue button injected.");
        }
        catch (Exception ex) { Log.Warn($"[AIDialogueMod] ShopUIPatch failed: {ex.Message}"); }
    }
}
