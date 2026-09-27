using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>How the stamina of one terrain swing is worked out (setting "Stamina Mode").</summary>
    public enum StaminaMode
    {
        /// <summary>The game's own cost.</summary>
        Vanilla = 0,
        /// <summary>No stamina at all.</summary>
        Off = 1,
        /// <summary>A set amount per swing.</summary>
        Fixed = 2,
        /// <summary>The game's cost times a factor, growing with the brush radius.</summary>
        Scaled = 3,
    }

    /// <summary>Who may switch free build on (setting "Allow Free Build").</summary>
    public enum FreeBuildAccess
    {
        Nobody = 0,
        Admins = 1,
        Everyone = 2,
    }

    /// <summary>
    /// Section "8. Costs", the tool side: stamina, tool wear, the cooldown and free build, plus the personal key and
    /// display switch. The gameplay keys are synced so the server decides the rules; the key and the HUD switch are
    /// each player's own. Values are read at use time, so edits and the server's push apply at once.
    /// </summary>
    public static class CostSettings
    {
        public static ConfigEntry<StaminaMode> Stamina { get; private set; }
        public static ConfigEntry<float> StaminaPerUse { get; private set; }
        public static ConfigEntry<float> StaminaFactor { get; private set; }
        public static ConfigEntry<float> StaminaRadiusExponent { get; private set; }
        public static ConfigEntry<Skills.SkillType> StaminaSkill { get; private set; }
        public static ConfigEntry<float> StaminaSkillReduction { get; private set; }
        public static ConfigEntry<bool> ToolWear { get; private set; }
        public static ConfigEntry<float> ToolWearFactor { get; private set; }
        public static ConfigEntry<float> ToolWearRadiusExponent { get; private set; }
        public static ConfigEntry<float> Cooldown { get; private set; }
        public static ConfigEntry<FreeBuildAccess> AllowFreeBuild { get; private set; }
        public static ConfigEntry<KeyboardShortcut> FreeBuildKey { get; private set; }
        public static ConfigEntry<bool> ShowCosts { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindStamina(synced);
            BindStaminaSkill(synced);
            BindWear(synced);
            BindUse(synced);
        }

        private static void BindStamina(SyncedConfiguration synced)
        {
            Stamina = synced.Bind(Sections.Costs, "Stamina Mode", StaminaMode.Vanilla,
                "Stamina a terrain entry of the hoe or cultivator uses per swing. Vanilla: the game's own cost. Off: none. Fixed: the amount in Stamina Per Use. Scaled: the game's cost times Stamina Factor, growing with the brush radius as Stamina Radius Exponent sets. A longer reach never lowers the cost.");
            StaminaPerUse = synced.Bind(Sections.Costs, "Stamina Per Use", 5f,
                "Stamina per terrain swing while Stamina Mode is Fixed.",
                acceptableValues: new AcceptableValueRange<float>(0f, 100f));
            StaminaFactor = synced.Bind(Sections.Costs, "Stamina Factor", 1f,
                "Scaled mode: the game's stamina cost is multiplied by this.",
                acceptableValues: new AcceptableValueRange<float>(0f, 10f));
            StaminaRadiusExponent = synced.Bind(Sections.Costs, "Stamina Radius Exponent", 1f,
                "Scaled mode: how strongly the stamina grows with the brush, as (brush radius / the entry's normal radius) to this power. 0: the radius does not matter; 1: grows with the radius; 2: grows with the area. A brush smaller than normal costs less.",
                acceptableValues: new AcceptableValueRange<float>(0f, 3f));
        }

        private static void BindStaminaSkill(SyncedConfiguration synced)
        {
            StaminaSkill = synced.Bind(Sections.Costs, "Stamina Skill", Skills.SkillType.None,
                "A skill that lowers the stamina of terrain work: at skill level 100 the cost is lowered by Stamina Skill Reduction percent, at lower levels proportionally less. None: no skill lowers it.");
            StaminaSkillReduction = synced.Bind(Sections.Costs, "Stamina Skill Reduction", 50f,
                "Percent the Stamina Skill takes off the terrain stamina cost at skill level 100.",
                acceptableValues: new AcceptableValueRange<float>(0f, 100f));
        }

        private static void BindWear(SyncedConfiguration synced)
        {
            ToolWear = synced.Bind(Sections.Costs, "Tool Wear", true,
                "Terrain swings wear the hoe and cultivator down as in the game. Off: terrain work never costs durability.");
            ToolWearFactor = synced.Bind(Sections.Costs, "Tool Wear Factor", 1f,
                "The game's durability loss per terrain swing is multiplied by this (0.5: half the wear, 2: double).",
                acceptableValues: new AcceptableValueRange<float>(0f, 10f));
            ToolWearRadiusExponent = synced.Bind(Sections.Costs, "Tool Wear Radius Exponent", 0f,
                "How the tool wear grows with the brush, as (brush radius / the entry's normal radius) to this power. 0: the radius does not matter; 1: grows with the radius; 2: grows with the area.",
                acceptableValues: new AcceptableValueRange<float>(0f, 3f));
        }

        private static void BindUse(SyncedConfiguration synced)
        {
            Cooldown = synced.Bind(Sections.Costs, "Cooldown", 0f,
                "Seconds a player waits between two terrain uses (brush swings and ramp, road or clearing work). Undo is never held back. 0: no cooldown.",
                acceptableValues: new AcceptableValueRange<float>(0f, 30f));
            AllowFreeBuild = synced.Bind(Sections.Costs, "Allow Free Build", FreeBuildAccess.Admins,
                "Who may switch free build on with the Free Build Key: Nobody, Admins (the server's admin list and the host) or Everyone. While free build is on, terrain work costs no stamina, tool wear, materials or volume stone and needs no crafting station.");
            FreeBuildKey = synced.Bind(Sections.Costs, "Free Build Key", new KeyboardShortcut(KeyCode.F7),
                "Switches free build on and off while a terrain tool is out, if the server allows it for you.", synced: false);
            ShowCosts = synced.Bind(Sections.Costs, "Show Costs", true,
                "Shows what one swing of the selected terrain entry costs at the current brush size next to the crosshair: have/need per item, red where you are short.",
                synced: false);
        }
    }
}
