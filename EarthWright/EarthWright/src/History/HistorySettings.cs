using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// Section "7. Undo". The history belongs to the player who made the changes and lives on their own machine, so
    /// every setting here is personal (not synced). Undo and redo themselves are ordinary terrain edits: the server's
    /// protection, zones and limits still decide whether they may happen.
    /// </summary>
    public static class HistorySettings
    {
        public static ConfigEntry<int> Size { get; private set; }
        public static ConfigEntry<float> GroupWindow { get; private set; }
        public static ConfigEntry<KeyboardShortcut> UndoKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> RedoKey { get; private set; }
        public static ConfigEntry<float> SnapshotRadius { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Size = synced.Bind(Sections.History, "History Size", 15,
                "How many of your own terrain changes the undo key can take back, newest first. The history is kept on your machine only and is cleared when you log out or change world, not when you die.",
                synced: false, acceptableValues: new AcceptableValueRange<int>(1, 50));
            GroupWindow = synced.Bind(Sections.History, "Group Window", 0.3f,
                "Changes made within this many seconds of each other are undone together as one step. A stroke dragged with the mouse button held is always one step.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0f, 5f));
            UndoKey = synced.Bind(Sections.History, "Undo Key", new KeyboardShortcut(KeyCode.Z, KeyCode.LeftControl),
                "Takes back your last terrain change while a terrain tool is out. While a ramp or road is being placed it removes the last point first. Costs are not refunded.",
                synced: false);
            RedoKey = synced.Bind(Sections.History, "Redo Key", new KeyboardShortcut(KeyCode.Y, KeyCode.LeftControl),
                "Brings back the change you last undid, while a terrain tool is out.", synced: false);
            SnapshotRadius = synced.Bind(Sections.History, "Snapshot Radius", 32f,
                "Radius in metres of the area around you that the console command 'ew snapshot save <name>' records when no radius is given.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(4f, 128f));
        }
    }
}
