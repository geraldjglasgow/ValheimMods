using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 21: Defense's milestones, each unlocked at a level: Riposte (25), Shield Wall (50), Hardened (75) and Last
    /// Stand (100). A milestone level above 100 turns it off; 0 gives it to everyone. Synced.
    /// </summary>
    public static class DefenseMilestoneSettings
    {
        public const string Section = DefenseSettings.MilestonesSection;

        public static ConfigEntry<float> RiposteLevel { get; private set; }
        public static ConfigEntry<float> RiposteWindow { get; private set; }
        public static ConfigEntry<float> RiposteDamage { get; private set; }
        public static ConfigEntry<float> ShieldWallLevel { get; private set; }
        public static ConfigEntry<float> ShieldWallRadius { get; private set; }
        public static ConfigEntry<float> ShieldWallReduction { get; private set; }
        public static ConfigEntry<float> HardenedLevel { get; private set; }
        public static ConfigEntry<float> HardenedPerStack { get; private set; }
        public static ConfigEntry<int> HardenedMaxStacks { get; private set; }
        public static ConfigEntry<float> HardenedDuration { get; private set; }
        public static ConfigEntry<float> LastStandLevel { get; private set; }
        public static ConfigEntry<float> LastStandCooldown { get; private set; }
        public static ConfigEntry<float> LastStandInvulnerability { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindRiposte(config);
            BindShieldWall(config);
            BindHardened(config);
            BindLastStand(config);
        }

        private static AcceptableValueRange<float> Levels => new AcceptableValueRange<float>(0f, 101f);

        private static void BindRiposte(SyncedConfiguration config)
        {
            RiposteLevel = config.Bind(Section, "Riposte Level", 25f,
                "Defense level that unlocks Riposte: a parry powers up your next melee attack.", acceptableValues: Levels);
            RiposteWindow = config.Bind(Section, "Riposte Window", 2f,
                "Seconds after a parry in which an attack you start is a riposte.", acceptableValues: new AcceptableValueRange<float>(0.5f, 10f));
            RiposteDamage = config.Bind(Section, "Riposte Damage", 25f,
                "Percent more damage for a riposte's melee hits, which also stagger the creatures they hit (not bosses, nor creatures the game never staggers).",
                acceptableValues: Settings.UpTo(300f));
        }

        private static void BindShieldWall(SyncedConfiguration config)
        {
            ShieldWallLevel = config.Bind(Section, "Shield Wall Level", 50f,
                "Defense level that unlocks Shield Wall: while you block with a shield, players close behind you take less damage.",
                acceptableValues: Levels);
            ShieldWallRadius = config.Bind(Section, "Shield Wall Radius", 4f,
                "How far behind the blocker, in metres, a player is sheltered.", acceptableValues: new AcceptableValueRange<float>(1f, 15f));
            ShieldWallReduction = config.Bind(Section, "Shield Wall Reduction", 10f,
                "Percent less damage for a sheltered player. Several blockers do not add up.", acceptableValues: Settings.UpTo(90f));
        }

        private static void BindHardened(SyncedConfiguration config)
        {
            HardenedLevel = config.Bind(Section, "Hardened Level", 75f,
                "Defense level that unlocks Hardened: each hit that hurts you makes you take less damage for a while.",
                acceptableValues: Levels);
            HardenedPerStack = config.Bind(Section, "Hardened Per Stack", 3f,
                "Percent less damage per stack.", acceptableValues: Settings.UpTo(30f));
            HardenedMaxStacks = config.Bind(Section, "Hardened Max Stacks", 5,
                "The most stacks you can have.", acceptableValues: new AcceptableValueRange<int>(1, 20));
            HardenedDuration = config.Bind(Section, "Hardened Duration", 8f,
                "Seconds the stacks last after the last hit that added one.", acceptableValues: new AcceptableValueRange<float>(1f, 60f));
        }

        private static void BindLastStand(SyncedConfiguration config)
        {
            LastStandLevel = config.Bind(Section, "Last Stand Level", 100f,
                "Defense level that unlocks Last Stand: a blow that would kill you leaves you at 1 health instead.",
                acceptableValues: Levels);
            LastStandCooldown = config.Bind(Section, "Last Stand Cooldown", 600f,
                "Seconds before Last Stand can save you again.", acceptableValues: new AcceptableValueRange<float>(10f, 7200f));
            LastStandInvulnerability = config.Bind(Section, "Last Stand Invulnerability", 2f,
                "Seconds you take no damage at all after Last Stand saved you.", acceptableValues: Settings.UpTo(10f));
        }
    }
}
