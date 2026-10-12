using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The local player's level in one of the game's skills, for the modules that deepen them (sections 36 to 43). A
    /// player's skills live only on their own client, so every perk built on them runs there, or reads a level the game
    /// or the module sent along (a hit, an RPC, a ZDO). Levels include food and mead bonuses, as the game's own effects do.
    /// </summary>
    public static class LocalSkill
    {
        /// <summary>Whether the character is this client's own player.</summary>
        public static bool IsLocal(Character character) =>
            character != null && ReferenceEquals(character, Player.m_localPlayer);

        /// <summary>The local player's level, bonuses included; 0 without a local player.</summary>
        public static float Level(Skills.SkillType skill)
        {
            Player player = Player.m_localPlayer;
            return player != null ? player.GetSkillLevel(skill) : 0f;
        }

        /// <summary>The local player's level as a share of 100, 0..1: the game's skill factor.</summary>
        public static float Factor(Skills.SkillType skill) => Mathf.Clamp01(Level(skill) / 100f);

        /// <summary>Whether the local player has reached a milestone level.</summary>
        public static bool Reached(Skills.SkillType skill, float level) => Level(skill) >= level;

        /// <summary>A perk worth <paramref name="atHundred"/> at level 100, linear from nothing at level 0.</summary>
        public static float Scaled(Skills.SkillType skill, float atHundred) => atHundred * Factor(skill);
    }
}
