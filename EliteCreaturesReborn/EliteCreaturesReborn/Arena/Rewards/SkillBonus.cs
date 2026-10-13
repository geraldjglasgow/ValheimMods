using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// The skill XP bonus of a trial's reward (features/arena.md section 3, "Rewards"), on the player's own machine: each
    /// skill the player raised during the trial (<see cref="SkillLedger"/>) is raised again by its gain times the reward's
    /// factor, through the game's own <c>Skills.RaiseSkill</c>, so every mod that watches or scales raises (GrindstoneSkills'
    /// rates and Catch Up among them) sees the bonus as it saw the gain. The game drops whatever a single raise earns past
    /// the next level, so the bonus is raised in steps as large as the trial's own raises of that skill were on average,
    /// at most <see cref="MostRaises"/> of them per skill (larger steps past that).
    /// </summary>
    internal static class SkillBonus
    {
        private const int MostRaises = 2000;

        private static readonly List<KeyValuePair<Skills.SkillType, Vector2>> Owed =
            new List<KeyValuePair<Skills.SkillType, Vector2>>();

        /// <summary>Raises the local player's skills by the bonus for <paramref name="trial"/>; then nothing is owed.</summary>
        public static void Pay(Player player, long trial, float factor)
        {
            Skills? skills = player.GetSkills();
            if (skills == null || factor <= 0f)
            {
                SkillLedger.Forget();
                return;
            }
            Owed.Clear();
            Owed.AddRange(SkillLedger.For(trial)); // copied: the raises below must not change what is being walked
            SkillLedger.Forget();
            foreach (KeyValuePair<Skills.SkillType, Vector2> gain in Owed)
            {
                Raise(skills, gain.Key, gain.Value, factor);
            }
            Owed.Clear();
        }

        // The gain (x: the factors added up, y: the raises) again, times the factor, in steps of the trial's average raise.
        private static void Raise(Skills skills, Skills.SkillType skill, Vector2 gain, float factor)
        {
            float bonus = gain.x * factor;
            if (bonus <= 0f)
            {
                return;
            }
            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1f, gain.y) * factor), 1, MostRaises);
            float step = bonus / steps;
            for (int i = 0; i < steps; i++)
            {
                skills.RaiseSkill(skill, step);
            }
        }
    }
}
