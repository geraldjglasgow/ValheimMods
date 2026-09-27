using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stamina refund and axe wear (section 12), the perks of a swing that hits wood (<see cref="SwingPerks"/>). Each
    /// grows linearly from nothing at Woodcutting level 0 to its value at level 100, on the swinging player's own level.
    /// Synced.
    /// </summary>
    public static class SwingPerkSettings
    {
        public const string Section = WoodcuttingSettings.ChoppingSection;

        public static ConfigEntry<float> StaminaRefund { get; private set; }
        public static ConfigEntry<float> WearReduction { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            StaminaRefund = config.Bind(Section, "Stamina Refund At 100", 30f,
                "Percent of a swing's stamina given back when the swing hits wood, for a level 100 woodcutter. 0 turns it off.",
                acceptableValues: Settings.UpTo(100f));
            WearReduction = config.Bind(Section, "Axe Wear Reduction At 100", 50f,
                "Percent less durability lost by a swing that hits wood, for a level 100 woodcutter. 100 means no wear at all.",
                acceptableValues: Settings.UpTo(100f));
        }
    }
}
