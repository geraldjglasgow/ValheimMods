using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The resting key of section "8. Homestead": it changes when players get the Rested buff, so it is synced and lockable.</summary>
    public static class RestSettings
    {
        public const string Section = HomesteadModule.Section;

        /// <summary>SE_Cozy.m_delay of the game's Resting status effect (read from the assets); the class default is 10.</summary>
        public const float GameDelay = 20f;

        public static ConfigEntry<float> RestedDelay { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            RestedDelay = synced.Bind(Section, "Rested Delay", 5f,
                "Seconds of resting (sitting near a fire, or near a fire under a roof) before the Rested buff arrives. The game's own delay is 20 s; set 20 to keep it, 0 gives Rested at once. "
                + "Only the wait changes: how long Rested lasts (longer with more comfort) and what counts as resting are the game's.",
                acceptableValues: new AcceptableValueRange<float>(0f, 60f));
        }
    }
}
