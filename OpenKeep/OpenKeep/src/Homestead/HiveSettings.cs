using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The beehive keys of section "8. Homestead": both change what a hive gives, so both are synced and lockable.</summary>
    public static class HiveSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<float> HoneyPerDay { get; private set; }
        public static ConfigEntry<bool> HoneyPerPlayerOnline { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            HoneyPerDay = synced.Bind(Section, "Honey Per Day", 0f,
                "Honey each beehive makes per in-game day. 0 keeps the game's own rate, one honey every 20 minutes of game time (1.5 a day). A hive still holds at most 4 honey until it is emptied.",
                acceptableValues: new AcceptableValueRange<float>(0f, 1000f));
            HoneyPerPlayerOnline = synced.Bind(Section, "Honey Per Player Online", false,
                "Each beehive makes as much honey per in-game day as there are players on the server (1 in single player). Overrides Honey Per Day.");
        }
    }
}
