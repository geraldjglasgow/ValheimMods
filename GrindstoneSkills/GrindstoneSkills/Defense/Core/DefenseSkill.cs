using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Defense, a skill of GrindstoneSkills' own (<see cref="CustomSkill"/>): the game's Blocking skill makes a shield
    /// stronger, Defense makes the player tougher. Its type number is the stable hash of <see cref="Keys.DefenseSkillName"/>.
    /// Everything it does to the defending player runs on that player's own client, which owns their character and so
    /// handles their damage, food, blocking and stamina; the level is also published to the player's ZDO under
    /// <see cref="Keys.DefenseLevel"/>, where an ally's client reads it for Shield Wall. The icon is <see cref="DefenseIcon"/>.
    /// </summary>
    public static class DefenseSkill
    {
        public const string Name = "Defense";
        public const float MaxLevel = CustomSkill.MaxLevel;

        public static readonly CustomSkill Skill = new CustomSkill(Keys.DefenseSkillName, Name,
            "More health, more health from food and less damage taken; regeneration out of combat, poise, a wider parry window and cheaper blocks and dodges. Trained by blocking and by taking hits.",
            Keys.DefenseLevel, DefenseIcon.Find);

        public static Skills.SkillType Type => Skill.Type;

        /// <summary>The master switch. Off, every Defense feature stands aside and fights play as vanilla.</summary>
        public static bool Active => DefenseSettings.Enabled.Value;

        /// <summary>The local player's Defense level, 0 when there is no local player.</summary>
        public static float Local() => Skill.Local();

        /// <summary>Any player's Defense level: the local player's own skill, anyone else's from their ZDO.</summary>
        public static float Of(Player player) => Skill.Of(player);

        public static bool IsLocal(Character character) => character != null && character == Player.m_localPlayer;

        /// <summary>A level as a share of level 100, clamped to 0..1: the scale every perk grows on.</summary>
        public static float Factor(float level) => Mathf.Clamp01(level / MaxLevel);

        /// <summary>A perk's share at a level: <paramref name="percentAt100"/> percent at level 100, linear from 0.</summary>
        public static float Share(float percentAt100, float level) => Mathf.Max(0f, percentAt100) / 100f * Factor(level);

        /// <summary>The local player's share of a perk; 0 while Defense is off.</summary>
        public static float LocalShare(float percentAt100) => Active ? Share(percentAt100, Local()) : 0f;

        /// <summary>Whether a level reached a milestone setting; a milestone above 100 is off.</summary>
        public static bool Reached(float level, float milestone) => milestone <= MaxLevel && level >= milestone;

        /// <summary>Whether Defense is on and the local player reached the milestone.</summary>
        public static bool LocalReached(float milestone) => Active && Reached(Local(), milestone);
    }
}
