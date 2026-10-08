using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>The skills window's orders, as the sort dropdown lists them.</summary>
    public enum SkillSort
    {
        Default,
        Name,
        Level
    }

    /// <summary>
    /// Lists the skills window's skills in the chosen order (<see cref="SortBar"/>): the game's own (the default), A to Z
    /// by name in the player's language, or highest level first. The choice is kept in the game's preferences, so it
    /// stays from one opening, and one game, to the next. Read from the game code (2026-10-07): <c>SkillsDialog.Setup</c>
    /// lays its entries out in the order of <c>Skills.GetSkillList</c>, a new list built from the skill dictionary in its
    /// own order. That list is sorted only while the window is being set up, so the game places, numbers and
    /// gamepad-steps its entries in the sorted order itself and nothing else that reads the list changes.
    /// <see cref="BookPane"/> sorts its own copy the same way, so its index of an entry matches the window's. Local only;
    /// nothing is sent.
    /// </summary>
    public static class SkillOrder
    {
        private const string PrefKey = "GrindstoneSkills_SkillSort";
        private static bool listing;
        private static SkillSort? current;

        /// <summary>The order chosen last, read from the game's preferences the first time.</summary>
        public static SkillSort Current
        {
            get
            {
                current ??= Read();
                return current.Value;
            }
        }

        /// <summary>Chooses an order and saves it with the game's preferences.</summary>
        public static void Choose(SkillSort sort)
        {
            current = sort;
            PlatformPrefs.SetInt(PrefKey, (int)sort);
            PlatformPrefs.Save();
        }

        private static SkillSort Read()
        {
            int saved = PlatformPrefs.GetInt(PrefKey, (int)SkillSort.Default);
            return Enum.IsDefined(typeof(SkillSort), saved) ? (SkillSort)saved : SkillSort.Default;
        }

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

        /// <summary>Sorts the skills in the chosen order, in place; the game's own order leaves them as they are.</summary>
        public static void Arrange(List<Skills.Skill> skills)
        {
            if (skills == null || skills.Count < 2 || Current == SkillSort.Default)
                return;
            List<Skills.Skill> sorted = Sorted(skills).ToList();
            skills.Clear();
            skills.AddRange(sorted);
        }

        private static IEnumerable<Skills.Skill> Sorted(List<Skills.Skill> skills)
        {
            if (Current == SkillSort.Level)
                return skills.OrderByDescending(skill => skill.m_level).ThenBy(NameOf, StringComparer.CurrentCultureIgnoreCase);
            return skills.OrderBy(NameOf, StringComparer.CurrentCultureIgnoreCase);
        }

        /// <summary>The name the window shows for a skill, made the way the game makes it.</summary>
        private static string NameOf(Skills.Skill skill) =>
            Localization.instance.Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower());
    }
}
