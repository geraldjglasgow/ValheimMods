using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 20: how Defense is trained. A hit from a creature (from a player too, if PvP hits train) earns experience
    /// when you block it or when it hurts you, times the hit's size: the square root of its damage over Hit Size Damage,
    /// kept between the minimum and maximum size. Synced.
    /// </summary>
    public static class DefenseExperienceSettings
    {
        public const string Section = DefenseSettings.ExperienceSection;

        public static ConfigEntry<float> Multiplier { get; private set; }
        public static ConfigEntry<float> BlockExperience { get; private set; }
        public static ConfigEntry<float> ParryMultiplier { get; private set; }
        public static ConfigEntry<float> WeaponBlockShare { get; private set; }
        public static ConfigEntry<float> HitExperience { get; private set; }
        public static ConfigEntry<float> HitSizeDamage { get; private set; }
        public static ConfigEntry<float> MinHitSize { get; private set; }
        public static ConfigEntry<float> MaxHitSize { get; private set; }
        public static ConfigEntry<float> Cooldown { get; private set; }
        public static ConfigEntry<float> FirstBlockBonus { get; private set; }
        public static ConfigEntry<bool> PlayerHitsTrain { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Multiplier = config.Bind(Section, "Experience Multiplier", 1f,
                "Multiplies all Defense experience.", acceptableValues: Settings.UpTo(10f));
            BlockExperience = config.Bind(Section, "Block Experience", 1f,
                "Experience for blocking a size 1 hit with a shield.", acceptableValues: Settings.UpTo(20f));
            ParryMultiplier = config.Bind(Section, "Parry Multiplier", 2f,
                "A parry earns this many times a block's experience.", acceptableValues: Settings.UpTo(10f));
            WeaponBlockShare = config.Bind(Section, "Weapon Block Share", 50f,
                "Percent of a shield's experience for blocking or parrying with a weapon (no shield equipped).",
                acceptableValues: Settings.UpTo(100f));
            HitExperience = config.Bind(Section, "Hit Taken Experience", 0.5f,
                "Experience for a size 1 hit that hurts you without being blocked.", acceptableValues: Settings.UpTo(20f));
            BindSizes(config);
        }

        private static void BindSizes(SyncedConfiguration config)
        {
            HitSizeDamage = config.Bind(Section, "Hit Size Damage", 10f,
                "Damage of a size 1 hit, as it arrives, before armour and blocking. A hit of 4 times this damage is size 2.",
                acceptableValues: new AcceptableValueRange<float>(1f, 1000f));
            MinHitSize = config.Bind(Section, "Min Hit Size", 0.5f,
                "The smallest size a hit counts as.", acceptableValues: Settings.UpTo(10f));
            MaxHitSize = config.Bind(Section, "Max Hit Size", 4f,
                "The largest size a hit counts as.", acceptableValues: Settings.UpTo(20f));
            Cooldown = config.Bind(Section, "Experience Cooldown", 0.5f,
                "Seconds after a hit earned experience before the next one can, so a swarm of small creatures does not outpace one big one.",
                acceptableValues: Settings.UpTo(10f));
            FirstBlockBonus = config.Bind(Section, "First Block Bonus", 3f,
                "Experience multiplier for the first block against each kind of creature. 1 turns it off.",
                acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            PlayerHitsTrain = config.Bind(Section, "Player Hits Train", false,
                "When on, hits from other players (PvP) train Defense like hits from creatures.");
        }
    }
}
