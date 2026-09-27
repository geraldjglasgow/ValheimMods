using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 17: the perks of mining. Each grows linearly from nothing at Pickaxes level 0 to its value at level 100:
    /// extra ore and splash on the miner's level, on the rock's owner; wear on the swinging player's own level, on their
    /// client. Synced.
    /// </summary>
    public static class PickaxePerkSettings
    {
        public const string Section = PickaxeSettings.PerksSection;

        public static ConfigEntry<float> ExtraOreChance { get; private set; }
        public static ConfigEntry<float> WearReduction { get; private set; }
        public static ConfigEntry<float> SplashDamage { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            ExtraOreChance = config.Bind(Section, "Extra Ore Chance At 100", 30f,
                "Percent chance, for a level 100 miner, that a broken chunk of an ore deposit (or a single-piece deposit such as tin) rolls its drops once more. 0 turns it off.",
                acceptableValues: Settings.UpTo(100f));
            WearReduction = config.Bind(Section, "Pickaxe Wear Reduction At 100", 50f,
                "Percent less durability lost by a swing that hits rock, for a level 100 miner. 100 means no wear at all.",
                acceptableValues: Settings.UpTo(100f));
            SplashDamage = config.Bind(Section, "Splash Damage At 100", 15f,
                "Damage a level 100 miner's swing spreads from the first chunk it hits to the intact chunks touching that one, shared equally between them, once per swing however many chunks it hits. It grows every 10 levels: nothing below level 10, a tenth of this value from 10, half from 50, all of it at 100. It needs a hit that passed the rock's tool tier, gives no experience and never splashes again. 0 turns it off.",
                acceptableValues: Settings.UpTo(200f));
        }
    }
}
