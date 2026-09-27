using System;
using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Makes GrindstoneSkills' own skills (<see cref="CustomSkill"/>) skills the game knows. Read from the game code
    /// 2026-09-27:
    /// <list type="bullet">
    /// <item>A Skills component looks every skill up in its m_skills list (GetSkillDef) and would add a skill with no
    /// definition, which breaks the level-up message; so Skills.Awake adds each definition to every Skills (the Player
    /// prefab's list is copied per instance).</item>
    /// <item>Skills.Load keeps only types the game's enum defines (IsSkillValid), so a saved level would be dropped
    /// without the second patch.</item>
    /// <item>The skills panel and the level-up message name a skill "$skill_" + the type's name in lower case; the
    /// word reaches the game's localization whenever a language is set up.</item>
    /// <item>Icons are looked up when ZNetScene wakes, once the prefabs are loaded.</item>
    /// </list>
    /// The death penalty, the skills panel, the world's skill-gain modifier and the level-up message then work
    /// unchanged. The console's raiseskill and resetskill are <see cref="CustomSkillCheats"/>.
    /// </summary>
    public static class CustomSkills
    {
        private static readonly List<CustomSkill> skills = new List<CustomSkill>();

        public static IReadOnlyList<CustomSkill> All => skills;

        /// <summary>
        /// Registers the mod's own skills, in the plugin's Awake. Also names them in a language the game set up before
        /// the plugin loaded; that reads the instance field, since Localization.instance would create the
        /// localization before the platform knows the locale.
        /// </summary>
        public static void Initialize(params CustomSkill[] own)
        {
            foreach (CustomSkill skill in own)
            {
                if (skill != null && Find(skill.Type) == null)
                    skills.Add(skill);
            }
            Localization localization = Localization.m_instance;
            if (localization != null)
                AddNames.Postfix(localization);
        }

        public static CustomSkill Find(Skills.SkillType type) => skills.Find(skill => skill.Type == type);

        /// <summary>The skill with this name, ignoring case; null when none has it.</summary>
        public static CustomSkill Find(string name) =>
            skills.Find(skill => string.Equals(skill.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>A player's level in one of the mod's own skills (<see cref="CustomSkill.Of"/>); 0 for any other type.</summary>
        public static float LevelOf(Player player, Skills.SkillType type) => Find(type)?.Of(player) ?? 0f;

        [HarmonyPatch(typeof(Skills), nameof(Skills.Awake))]
        private static class AddDefinitions
        {
            [HarmonyPostfix]
            private static void Postfix(Skills __instance)
            {
                foreach (CustomSkill skill in skills)
                {
                    if (!__instance.m_skills.Exists(def => def != null && def.m_skill == skill.Type))
                        __instance.m_skills.Add(skill.Definition);
                }
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.IsSkillValid))]
        private static class KeepSavedLevels
        {
            [HarmonyPostfix]
            private static void Postfix(Skills.SkillType type, ref bool __result) => __result |= Find(type) != null;
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
        private static class AddNames
        {
            [HarmonyPostfix]
            internal static void Postfix(Localization __instance)
            {
                foreach (CustomSkill skill in skills)
                    __instance.AddWord(skill.LocalizationKey, skill.Name);
                __instance.m_cache.EvictAll();
            }
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class FindIcons
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                foreach (CustomSkill skill in skills)
                    skill.FindIcon();
            }
        }
    }
}
