using System;
using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Section "1. Brush", skill cap keys: an optional cap on the brush radius that grows with one of the player's
    /// skills. Synced, because it limits what every player can do to the shared world.
    /// </summary>
    public static class SkillCapSettings
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> Skill { get; private set; }
        public static ConfigEntry<float> StartLevel { get; private set; }
        public static ConfigEntry<float> FullLevel { get; private set; }
        public static ConfigEntry<float> FullRadius { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Sections.Brush, "Skill Cap", false,
                "When on, the largest brush radius grows with a skill: below the start level entries keep the game's own size, and the cap grows to the full radius at the full level. The cap never goes below an entry's own size.");
            Skill = synced.Bind(Sections.Brush, "Skill Cap Skill", "Crafting",
                "The skill the cap follows, by its English name in the game's code (Crafting, WoodCutting, Pickaxes, Run, ...).");
            StartLevel = synced.Bind(Sections.Brush, "Skill Cap Start Level", 25f,
                "Below this skill level the brush keeps each entry's own size.", acceptableValues: new AcceptableValueRange<float>(0f, 100f));
            FullLevel = synced.Bind(Sections.Brush, "Skill Cap Full Level", 60f,
                "At this skill level the cap reaches the full radius.", acceptableValues: new AcceptableValueRange<float>(1f, 100f));
            FullRadius = synced.Bind(Sections.Brush, "Skill Cap Radius", 6f,
                "The largest radius (metres) the skill cap allows at the full level.", acceptableValues: new AcceptableValueRange<float>(0.5f, 100f));
        }
    }

    /// <summary>
    /// The skill cap itself: the largest radius the local player's skill allows for an entry whose own (vanilla) size
    /// is <c>ownRadius</c>. Runs on the player's machine; the skill level is the player's own.
    /// </summary>
    public static class SkillCap
    {
        private static string parsedName;
        private static Skills.SkillType parsedSkill = Skills.SkillType.Crafting;

        /// <summary>float.MaxValue when the cap is off or there is no player.</summary>
        public static float Cap(float ownRadius)
        {
            Player player = Player.m_localPlayer;
            if (SkillCapSettings.Enabled == null || !SkillCapSettings.Enabled.Value || player == null)
                return float.MaxValue;
            float level = player.GetSkillLevel(SkillType());
            float start = SkillCapSettings.StartLevel.Value;
            float full = Mathf.Max(start + 0.01f, SkillCapSettings.FullLevel.Value);
            float share = Mathf.Clamp01((level - start) / (full - start));
            float grown = Mathf.Lerp(ownRadius, SkillCapSettings.FullRadius.Value, share);
            return Mathf.Max(ownRadius, grown);
        }

        private static Skills.SkillType SkillType()
        {
            string name = SkillCapSettings.Skill.Value;
            if (name == parsedName)
                return parsedSkill;
            parsedName = name;
            if (!Enum.TryParse(name?.Trim(), true, out parsedSkill))
            {
                parsedSkill = Skills.SkillType.Crafting;
                Plugin.Log.LogWarning($"EarthWright: unknown skill '{name}' for the skill cap, using Crafting.");
            }
            return parsedSkill;
        }
    }
}
