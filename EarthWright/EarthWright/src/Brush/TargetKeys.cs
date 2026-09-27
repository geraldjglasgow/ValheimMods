using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>Section "10. Controls", the target height keys. Local: each player picks their own keys.</summary>
    public static class TargetKeys
    {
        public static ConfigEntry<KeyboardShortcut> Lock { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Up { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Down { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Feet { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Cycle { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Floor { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Lock = ControlSettings.Key(synced, "Lock Height Key", KeyCode.K,
                "Locks the current target height so every click levels to it; press again to release it.");
            Up = ControlSettings.Key(synced, "Height Up Key", KeyCode.PageUp,
                "Raises the locked target height by the height key step (hold to repeat, Shift for bigger steps). Locks the current height first when none is locked.");
            Down = ControlSettings.Key(synced, "Height Down Key", KeyCode.PageDown,
                "Lowers the locked target height by the height key step (hold to repeat, Shift for bigger steps). Locks the current height first when none is locked.");
            Feet = ControlSettings.Key(synced, "Back To Feet Key", KeyCode.End,
                "Releases any locked or exact height and levels to the ground under your feet again.");
            Cycle = ControlSettings.Key(synced, "Target Mode Key", KeyCode.Y,
                "Cycles where the target height comes from: your feet, the crosshair, or continuing the flat of earlier edits.");
            Floor = ControlSettings.Key(synced, "Copy Floor Height Key", KeyCode.Mouse2,
                "Copies the top height of the building piece under the crosshair and locks it as the target.");
        }
    }
}
