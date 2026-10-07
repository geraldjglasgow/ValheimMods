using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Foraging, a skill of GrindstoneSkills' own (<see cref="CustomSkill"/>): the game has none (it trains Farming
    /// with wild plants). Its type number is the stable hash of <see cref="Keys.ForagingSkillName"/>. Levels live with
    /// the character like every skill (client-side), and every Foraging feature runs on the picker's client. The icon
    /// is <see cref="ForagingIcon"/>.
    /// </summary>
    public static class ForagingSkill
    {
        public const string Name = "Foraging";
        public const float MaxLevel = CustomSkill.MaxLevel;

        public static readonly CustomSkill Skill = new CustomSkill(Keys.ForagingSkillName, Name,
            "Picking wild berries, mushrooms, herbs and stones: extra yield, sweep picking.",
            Keys.ForagingLevel, ForagingIcon.Find);

        public static Skills.SkillType Type => Skill.Type;

        public static bool Active => ForagingSettings.Enabled.Value;

        /// <summary>A player's own Foraging level, on that player's client; 0 without a player.</summary>
        public static float Level(Player player) => player == null ? 0f : player.GetSkillLevel(Type);

        /// <summary>A perk's share at a level: <paramref name="percentAt100"/> percent at level 100, linear from 0.</summary>
        public static float Share(float percentAt100, float level) =>
            Mathf.Max(0f, percentAt100) / 100f * Mathf.Clamp01(level / MaxLevel);
    }
}
