namespace AIDialogueMod.Patches;

public static class RestSiteFilter
{
    public static bool IsRestSite(string eventId)
    {
        return eventId.Contains("rest", System.StringComparison.OrdinalIgnoreCase)
            || eventId.Contains("campfire", System.StringComparison.OrdinalIgnoreCase);
    }
}
