using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace AIDialogueMod;

[ModInitializer("Initialize")]
public static class Plugin
{
    private static Harmony? _harmony;

    public static void Initialize()
    {
        try
        {
            _harmony = new Harmony("com.modauthor.aidialogueMod");
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Log.Warn("[AIDialogueMod] Mod loaded successfully.");
        }
        catch (System.Exception ex)
        {
            Log.Warn($"[AIDialogueMod] Failed to initialize: {ex.Message}");
        }
    }
}
