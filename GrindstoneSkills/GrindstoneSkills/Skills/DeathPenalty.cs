using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Skill loss on death, for every skill. Player.OnDeath calls Skills.OnDeath on the dying player's own client,
    /// unless the "no skill drain" buff from a recent death is active or the world resets skills on death (then
    /// Skills.Clear runs instead, untouched here). Skills.OnDeath calls LowerAllSkills(m_DeathLowerFactor *
    /// Game.m_skillReductionRate), which takes that share off every level and zeroes every skill's progress toward
    /// its next level (m_accumulator). The game's factor is 0.05.
    /// For that one call the factor becomes "Skill Loss On Death" (the world's death penalty modifier still scales
    /// it), and with "Lose Progress On Death" off every skill's progress is put back after LowerAllSkills. With no
    /// loss and progress kept nothing is lowered, and the game's "skills lowered" message is not shown either.
    /// </summary>
    public static class DeathPenalty
    {
        private static bool keepProgress;

        [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
        private static class OnDeath
        {
            [HarmonyPrefix]
            private static bool Prefix(Skills __instance, out float __state)
            {
                __state = __instance.m_DeathLowerFactor;
                float loss = Mathf.Clamp(DeathSettings.SkillLoss.Value, 0f, 100f) / 100f;
                keepProgress = !DeathSettings.LoseProgress.Value;
                if (loss <= 0f && keepProgress)
                    return false;
                __instance.m_DeathLowerFactor = loss;
                return true;
            }

            [HarmonyFinalizer]
            private static void Finalizer(Skills __instance, float __state)
            {
                __instance.m_DeathLowerFactor = __state;
                keepProgress = false;
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.LowerAllSkills))]
        private static class LowerAll
        {
            [HarmonyPrefix]
            private static void Prefix(Skills __instance, out Dictionary<Skills.SkillType, float> __state) =>
                __state = keepProgress ? Progress(__instance) : null;

            [HarmonyPostfix]
            private static void Postfix(Skills __instance, Dictionary<Skills.SkillType, float> __state)
            {
                if (__state == null)
                    return;
                foreach (KeyValuePair<Skills.SkillType, float> saved in __state)
                    if (__instance.m_skillData.TryGetValue(saved.Key, out Skills.Skill skill))
                        skill.m_accumulator = saved.Value;
            }
        }

        /// <summary>Every skill's progress toward its next level.</summary>
        private static Dictionary<Skills.SkillType, float> Progress(Skills skills)
        {
            Dictionary<Skills.SkillType, float> progress = new Dictionary<Skills.SkillType, float>();
            foreach (KeyValuePair<Skills.SkillType, Skills.Skill> skill in skills.m_skillData)
                progress[skill.Key] = skill.Value.m_accumulator;
            return progress;
        }
    }
}
