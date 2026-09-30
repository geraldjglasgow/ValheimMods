using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The bed settings of section "8. Homestead", read at use time. Where players wake and how long they wait change
    /// the game, so those are synced and locked; showing the beds on the map is each player's own choice.
    /// </summary>
    public static class BedSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<bool> NearestBedRespawn { get; private set; }
        public static ConfigEntry<float> BedChoiceSeconds { get; private set; }
        public static ConfigEntry<bool> QuickRespawn { get; private set; }
        public static ConfigEntry<float> QuickRespawnRange { get; private set; }
        public static ConfigEntry<float> QuickRespawnSeconds { get; private set; }
        public static ConfigEntry<bool> StandUpOnRespawn { get; private set; }
        public static ConfigEntry<bool> BedsOnMap { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            NearestBedRespawn = synced.Bind(Section, "Nearest Bed Respawn", true,
                "Every bed you own is a spawn bed: after death you wake in your bed nearest to where you died (the next nearest when that one is gone or no longer yours), and any of your beds lets you sleep. Off: only the last bed you claimed or set counts, as in the game.");
            BedChoiceSeconds = synced.Bind(Section, "Bed Choice Seconds", 30f,
                "With Nearest Bed Respawn and two or more beds, death opens the map with your beds on it and a countdown in its upper left corner: click a bed to wake there. With no click before the countdown ends you wake in the bed nearest to where you died. 0: no map, always the nearest.",
                acceptableValues: new AcceptableValueRange<float>(0f, 60f));
            BindQuick(synced);
            BedsOnMap = synced.Bind(Section, "Beds On Map", true,
                "With Nearest Bed Respawn, every bed you own shows on your map with the game's bed icon in yellow, not only the last one you slept in.", false);
        }

        private static void BindQuick(SyncedConfiguration synced)
        {
            QuickRespawn = synced.Bind(Section, "Quick Respawn", true,
                "The closer to where you died you wake, the sooner you wake: the whole wait after death (the game's 10 s, then 8 s of loading) shrinks with the distance to the bed, or to the world start without one. Off: the game's wait.");
            QuickRespawnRange = synced.Bind(Section, "Quick Respawn Range", 1000f,
                "Metres (on the map) between where you died and the bed at which the game's full wait applies; closer is quicker, in proportion.",
                acceptableValues: new AcceptableValueRange<float>(10f, 20000f));
            QuickRespawnSeconds = synced.Bind(Section, "Quick Respawn Seconds", 1f,
                "Seconds from death to waking when you died right beside the bed. A far area can still take longer to load.",
                acceptableValues: new AcceptableValueRange<float>(0f, 18f));
            StandUpOnRespawn = synced.Bind(Section, "Stand Up On Respawn", true,
                "After a death you wake standing and can move at once, instead of the game's getting-up animation. Logging in keeps the game's.");
        }
    }
}
