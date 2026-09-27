using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// The General section, and the order every settings group is bound in. Sections are numbered so the .cfg and
    /// configuration managers list them in this order. Gameplay settings are synced and lockable; the Display
    /// section is each player's own.
    /// </summary>
    public static class Settings
    {
        public const string General = "1 - General";

        public static ConfigEntry<bool> LockConfiguration { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            LockConfiguration = config.BindLocking(General, "Lock Configuration", true,
                "Server only. When on, every player uses the server's values for this file and cannot override them locally.");
            StarSettings.Initialize(config);
            OddsSettings.Initialize(config);
            KitchenSettings.Initialize(config);
            ExperienceSettings.Initialize(config);
            DeathSettings.Initialize(config);
            DisplaySettings.Initialize(config);
            SailingSettings.Initialize(config);
            LookoutSettings.Initialize(config);
            WoodcuttingSettings.Initialize(config);
            PickaxeSettings.Initialize(config);
            DefenseSettings.Initialize(config);
            HusbandrySettings.Initialize(config);
            ForagingSettings.Initialize(config);
            FishingSettings.Initialize(config);
            FarmingSettings.Initialize(config);
        }

        /// <summary>A range from 0 to the given maximum, for percent and multiplier settings.</summary>
        public static AcceptableValueRange<float> UpTo(float max) => new AcceptableValueRange<float>(0f, max);
    }
}
