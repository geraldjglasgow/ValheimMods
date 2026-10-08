using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Lists the skills window's skills by name, A to Z in the player's language.
    /// Read from the game code (2026-10-07): <c>SkillsDialog.Setup</c> lays its entries out in the order of
    /// <c>Skills.GetSkillList</c>, a new list built from the skill dictionary in its own order.
    /// That list is sorted only while the window is being set up, so the game places, numbers and gamepad-steps its
    /// entries in the sorted order itself and nothing else that reads the list changes. <see cref="BookPane"/> sorts
    /// its own copy the same way, so its index of an entry matches the window's. Local only; nothing is sent.
    /// </summary>
    public static class SkillOrder
    {
        private static bool listing;

        [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
        private static class Listing
        {
            [HarmonyPrefix]
            private static void Prefix() => listing = true;

            [HarmonyFinalizer]
            private static void Finalizer() => listing = false;
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillList))]
        private static class Ordered
        {
            [HarmonyPostfix]
            private static void Postfix(List<Skills.Skill> __result)
            {
                if (listing)
                    HookGuard.Run("skill order", Arrange, __result);
            }
        }

        /// <summary>Sorts the skills by their shown name, in place.</summary>
        public static void Arrange(List<Skills.Skill> skills)
        {
            if (skills == null || skills.Count < 2)
                return;
            List<Skills.Skill> sorted = skills.OrderBy(NameOf, StringComparer.CurrentCultureIgnoreCase).ToList();
            skills.Clear();
            skills.AddRange(sorted);
        }

        /// <summary>The name the window shows for a skill, made the way the game makes it.</summary>
        private static string NameOf(Skills.Skill skill) =>
            Localization.instance.Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower());
    }
}
