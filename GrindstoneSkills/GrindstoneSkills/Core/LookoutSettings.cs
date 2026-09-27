using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 9: the lookout, Sailing's milestone. A key pressed aboard a ship sends a pulse out from it, and everyone
    /// aboard sees the name tags of the enemies it reached. Synced, except the key, which is each player's own.
    /// </summary>
    public static class LookoutSettings
    {
        public const string Section = "9 - Lookout";

        public static ConfigEntry<float> Level { get; private set; }
        public static ConfigEntry<float> Radius { get; private set; }
        public static ConfigEntry<float> Duration { get; private set; }
        public static ConfigEntry<float> Cooldown { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Key { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Level = config.Bind(Section, "Lookout Level", 50f,
                "Sailing level that unlocks the lookout. 0 gives it to everyone; above 100 turns it off.",
                acceptableValues: new AcceptableValueRange<float>(0f, 101f));
            Radius = config.Bind(Section, "Lookout Radius", 100f,
                "How far from the ship the pulse reaches, in metres.", acceptableValues: new AcceptableValueRange<float>(10f, 300f));
            Duration = config.Bind(Section, "Lookout Duration", 30f,
                "Seconds the enemies the pulse reached keep their name tags.", acceptableValues: new AcceptableValueRange<float>(1f, 300f));
            Cooldown = config.Bind(Section, "Lookout Cooldown", 60f,
                "Seconds before a player can send the next pulse.", acceptableValues: Settings.UpTo(3600f));
            Key = config.Bind(Section, "Lookout Key", new KeyboardShortcut(KeyCode.O),
                "Sends the lookout pulse while you are aboard a ship. Each player's own.", synced: false);
        }
    }
}
