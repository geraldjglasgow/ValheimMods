using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sections 36 to 43: one switch for each of the game's skills GrindstoneSkills deepens on top of the game's own
    /// effects (Run, Jump, Sneak, Swim, Dodge, Ride, Crafting and the eleven weapon skills). Every number is a constant
    /// in its module and named in the switch's description. Off, the skill plays exactly as in the game; levels are kept
    /// either way. Synced and lockable.
    /// </summary>
    public static class GameSkillSettings
    {
        public static ConfigEntry<bool> Running { get; private set; }
        public static ConfigEntry<bool> Jumping { get; private set; }
        public static ConfigEntry<bool> Sneaking { get; private set; }
        public static ConfigEntry<bool> Swimming { get; private set; }
        public static ConfigEntry<bool> Dodging { get; private set; }
        public static ConfigEntry<bool> Riding { get; private set; }
        public static ConfigEntry<bool> Crafting { get; private set; }
        public static ConfigEntry<bool> WeaponMastery { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindMovement(config);
            BindCombat(config);
            Crafting = config.Bind("42 - Crafting", "Crafting Enabled", true,
                "Maker's Mark (weapons, armour, shields and tools you craft or upgrade get up to 25% more durability at level 100, kept on the item), Sturdy Build (pieces you place get up to 25% more health, kept on the piece), Thrifty (up to a 10% chance that a craft at the workbench, forge or another Crafting station gives one material back) and Journeyman at 75 (the workbench and forge count one level higher for you). Off, crafting and building are exactly the game's; items and pieces made earlier keep theirs.");
            WeaponMastery = config.Bind("43 - Weapon Mastery", "Weapon Mastery Enabled", true,
                "Weapon Switch (a weapon skill below your best weapon skill trains 50% faster) and one milestone per weapon skill at level 50: Swords Edge (+25% damage to a staggered foe), Knives Bleed (hits add poison worth 20% of the hit), Clubs Crush (+30% stagger), Polearms Sweep (+30% knockback), Spears Long Throw (thrown spears +25% damage), Axes Rend (each hit on the same foe within 3 s adds 5%, up to 25%), Unarmed Brawler (+50% damage), Bows Recover (25% chance a shot uses no arrow), Crossbows Steady Reload (sprinting, jumping and dodging pause a reload instead of restarting it), Elemental Magic Attunement (+25% eitr regeneration holding a staff) and Blood Magic Life Tap (spells cost 25% less health). Blood magic shields also train their caster when they absorb damage.");
        }

        private static void BindMovement(SyncedConfiguration config)
        {
            Running = config.Bind("36 - Running", "Running Enabled", true,
                "Pack Mule (up to +50 carry weight at level 100), Burden at 50 (half the stamina drain while over-encumbered) and Second Wind at 75 (running out of stamina while running gives 30 back, once every 3 minutes). Off, running is exactly the game's.");
            Jumping = config.Bind("37 - Jumping", "Jumping Enabled", true,
                "Light Feet (jumps cost up to 50% less stamina at level 100), Soft Landing (falls are safe up to 4 m higher, and fall damage is up to 50% less) and Roll at 50 (landing from a harmful fall while moving forward rolls: no damage up to twice the safe height, half above it). Off, jumping and falling are exactly the game's.");
            Sneaking = config.Bind("38 - Sneaking", "Sneaking Enabled", true,
                "Prowl (crouched walking up to 50% faster at level 100), Soft Steps (up to 50% less noise while crouched), Assassin (sneak attacks up to 50% stronger, and 5 Sneak experience each) and Vanish at 75 (crouch for 3 s with no enemy within 8 m and creatures hunting you within 40 m lose track of you, once a minute). Off, sneaking is exactly the game's.");
            Swimming = config.Bind("39 - Swimming", "Swimming Enabled", true,
                "Quick Dry (Wet ends up to 50% sooner at level 100), Long Breath (out of stamina in water, drowning starts up to 3 s later and hurts up to 50% less) and Salvage (floating items are picked up from up to twice as far while swimming). Swim speed and swim stamina stay the game's. Off, swimming is exactly the game's.");
            Riding = config.Bind("41 - Riding", "Riding Enabled", true,
                "Gallop (a mount you ride runs up to 15% faster at level 100, on top of the game's 25%) and Steady Seat (a mount takes up to 25% less damage while you ride it). Off, riding is exactly the game's.");
        }

        private static void BindCombat(SyncedConfiguration config)
        {
            Dodging = config.Bind("40 - Dodging", "Dodging Enabled", true,
                "Long Roll (a roll's invulnerability lasts up to 30% longer at level 100), Counter at 50 (dodging through a hit makes the next melee attack you start within 2 s deal 25% more) and Heavy Roll at 75 (you can dodge while over-encumbered, for twice the stamina). Off, dodging is exactly the game's.");
        }
    }
}
