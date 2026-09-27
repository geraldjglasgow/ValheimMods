using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The bed setting of section "8. Homestead": it changes where players wake, so it is synced and locked; read at use time.</summary>
    public static class BedSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<bool> NearestBedRespawn { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            NearestBedRespawn = synced.Bind(Section, "Nearest Bed Respawn", true,
                "Every bed you own is a spawn bed: after death you wake in your bed nearest to where you died (the next nearest when that one is gone or no longer yours), and any of your beds lets you sleep. Off: only the last bed you claimed or set counts, as in the game.");
        }
    }
}
