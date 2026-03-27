using System.Collections.Generic;

namespace AIDialogueMod;

/// <summary>
/// Caches DialogueManager instances per encounter so re-opening dialogue
/// preserves conversation history and personality.
/// Cleared when entering a new room/encounter.
/// </summary>
public static class DialogueSessionCache
{
    private static readonly Dictionary<string, DialogueManager> _sessions = new();
    private static bool _panelOpen;

    /// <summary>Whether a dialogue panel is currently open.</summary>
    public static bool IsPanelOpen => _panelOpen;

    /// <summary>Get or create a DialogueManager for the given encounter key.</summary>
    public static (DialogueManager manager, bool isNew) GetOrCreate(string key, Config.ModConfig config)
    {
        if (_sessions.TryGetValue(key, out var existing))
            return (existing, false);

        var manager = new DialogueManager(config);
        _sessions[key] = manager;
        return (manager, true);
    }

    public static void MarkPanelOpen() => _panelOpen = true;
    public static void MarkPanelClosed() => _panelOpen = false;

    /// <summary>Call when entering a new room to clear stale sessions.</summary>
    public static void ClearAll()
    {
        _sessions.Clear();
        _panelOpen = false;
    }
}
