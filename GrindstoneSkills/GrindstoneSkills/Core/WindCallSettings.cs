using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 35: Wind Call, Sailing's level 25 active. A key pressed while steering a ship turns the wind to blow the way the
    /// player looks, for everyone aboard that ship, for a while. Synced, except the key, which is each player's own.
    /// </summary>
    public static class WindCallSettings
    {
        public const string Section = "35 - Wind Call";

        public static ConfigEntry<float> Level { get; private set; }
        public static ConfigEntry<float> Duration { get; private set; }
        public static ConfigEntry<float> Cooldown { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Key { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Level = config.Bind(Section, "Wind Call Level", 25f,
                "Sailing level that unlocks Wind Call. 0 gives it to everyone; above 100 turns it off.",
                acceptableValues: new AcceptableValueRange<float>(0f, 101f));
            Duration = config.Bind(Section, "Wind Call Duration", 60f,
                "Seconds the called wind blows before the weather's own wind returns.",
                acceptableValues: new AcceptableValueRange<float>(5f, 600f));
            Cooldown = config.Bind(Section, "Wind Call Cooldown", 180f,
                "Seconds before a player can call the wind again.", acceptableValues: Settings.UpTo(3600f));
            Key = config.Bind(Section, "Wind Call Key", new KeyboardShortcut(KeyCode.K),
                "Turns the wind to blow the way you look while you steer a ship. Each player's own.", synced: false);
        }
    }
}
