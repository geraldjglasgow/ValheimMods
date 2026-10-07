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
        public static ConfigEntry<bool> QuickRespawn { get; private set; }
        public static ConfigEntry<bool> StandUpOnRespawn { get; private set; }
        public static ConfigEntry<bool> BedsOnMap { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            NearestBedRespawn = synced.Bind(Section, "Nearest Bed Respawn", true,
                "Every bed you own is a spawn bed: after death you wake in your bed nearest to where you died (the next nearest when that one is gone or no longer yours), and any of your beds lets you sleep. With two beds or more, death opens the map for 30 s: click a bed to wake there. Off: only the last bed you claimed or set counts, as in the game.");
            QuickRespawn = synced.Bind(Section, "Quick Respawn", true,
                "The closer to where you died you wake, the sooner you wake: 1 s beside the bed, the game's full 18 s from 1000 m (on the map), in proportion between; measured to the world start without a bed. A bed picked on the map wakes you as soon as its land has loaded. Off: the game's wait.");
            StandUpOnRespawn = synced.Bind(Section, "Stand Up On Respawn", true,
                "After a death you wake standing and can move at once, instead of the game's getting-up animation. Logging in keeps the game's.");
            BedsOnMap = synced.Bind(Section, "Beds On Map", true,
                "With Nearest Bed Respawn, every bed you own shows on your map with the game's bed icon in yellow, not only the last one you slept in.", false);
        }
    }
}
