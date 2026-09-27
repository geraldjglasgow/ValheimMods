using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The game's own Farming skill (Skills.SkillType.Farming, 106): the cultivator's piece table raises it on every
    /// placement and every crop pickable on every pick. Levels are client-side like every skill; a planter's level
    /// reaches other machines inside the plant's ZDO (<see cref="PlantKeys"/>), a picker's never leaves their client.
    /// </summary>
    public static class FarmSkill
    {
        public const Skills.SkillType Skill = Skills.SkillType.Farming;
        public const float MaxLevel = 100f;

        /// <summary>The master switch. Off, every Farming feature stands aside and the game plays as vanilla.</summary>
        public static bool Active => FarmingSettings.Enabled.Value;

        /// <summary>A player's own Farming level, on that player's client; 0 without a player.</summary>
        public static float Level(Player player) => player == null ? 0f : player.GetSkillLevel(Skill);

        /// <summary>The local player's Farming level, 0 when there is no local player.</summary>
        public static float Local() => Level(Player.m_localPlayer);

        /// <summary>A level as a share of level 100, clamped to 0..1: the scale every perk grows on.</summary>
        public static float Factor(float level) => Mathf.Clamp01(level / MaxLevel);

        /// <summary>A perk's share at a level: <paramref name="percentAt100"/> percent at level 100, linear from 0.</summary>
        public static float Share(float percentAt100, float level) => Mathf.Max(0f, percentAt100) / 100f * Factor(level);

        /// <summary>Whether a "Level" perk is reached: at or above its level (101 turns it off).</summary>
        public static bool Reached(int perkLevel, float level) => perkLevel <= MaxLevel && level >= perkLevel;
    }
}
