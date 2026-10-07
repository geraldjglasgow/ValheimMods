using System;

namespace GrindstoneSkills.Api
{
    /// <summary>
    /// Skill levels for other mods, read by reflection (Hearthhold does): plain types only. A skill is named by its
    /// panel name, ignoring case: one of GrindstoneSkills' own ("Foraging", "Husbandry", "Sailing", "Defense") or one of
    /// the game's (a Skills.SkillType name such as "Cooking", "Farming", "Fishing"). Main thread only. Later versions
    /// only add endpoints.
    /// </summary>
    public static class SkillsApi
    {
        public static int GetApiVersion() => 1;

        /// <summary>The local player's level in the skill, 0 to 100; 0 for an unknown skill or without a local player.</summary>
        public static float GetLocalLevel(string skill) => GetLevel(Player.m_localPlayer, skill);

        /// <summary>
        /// A player's level in the skill, 0 to 100. Any player's level in GrindstoneSkills' own skills (each client
        /// publishes its own to the player's ZDO once a second); the game's skills only for the local player, 0 for others.
        /// </summary>
        public static float GetLevel(Player player, string skill)
        {
            if (player == null || string.IsNullOrEmpty(skill))
                return 0f;
            CustomSkill own = CustomSkills.Find(skill);
            if (own != null)
                return own.Of(player);
            if (player != Player.m_localPlayer || !Enum.TryParse(skill, true, out Skills.SkillType type))
                return 0f;
            return player.GetSkillLevel(type);
        }
    }
}
