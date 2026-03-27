using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace AIDialogueMod.Patches;

/// <summary>
/// Patches NRestSiteRoom._Ready to ensure NO dialogue button is injected at rest sites.
/// This is a Prefix patch that does nothing — it exists to explicitly document that
/// rest sites are excluded. The actual exclusion is passive: we simply don't inject anything here.
///
/// If future code tries to generically inject buttons into all rooms, this class
/// serves as the filter point.
/// </summary>
[HarmonyPatch(typeof(NRestSiteRoom), nameof(NRestSiteRoom._Ready))]
public static class RestSiteFilter
{
    public static void Postfix(NRestSiteRoom __instance)
    {
        // Explicitly do nothing — rest sites are excluded from AI dialogue.
        // This patch exists as a safeguard and documentation.
        Log.Warn("[AIDialogueMod] RestSiteFilter: rest site detected, dialogue button NOT injected.");
    }

    /// <summary>
    /// Utility method to check if a room type string indicates a rest site.
    /// </summary>
    public static bool IsRestSite(string roomType)
    {
        return roomType.Contains("rest", System.StringComparison.OrdinalIgnoreCase)
            || roomType.Contains("campfire", System.StringComparison.OrdinalIgnoreCase);
    }
}
