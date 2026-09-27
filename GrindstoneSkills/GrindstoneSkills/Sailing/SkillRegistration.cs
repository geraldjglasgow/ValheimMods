using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Makes Sailing a skill the game knows. A Skills component looks every skill up in its m_skills list
    /// (GetSkillDef) and would add a skill with no definition, which breaks the level-up message; so Skills.Awake adds
    /// Sailing's definition to every Skills (the Player prefab's list is copied per instance). Skills.Load keeps only
    /// types the game's enum defines (IsSkillValid), so a saved Sailing level would be dropped without the second
    /// patch. The name reaches the game's localization whenever a language is set up; the icon is the Karve's, read
    /// once the prefabs are loaded. The death penalty, the skills panel and the level-up message then work unchanged.
    /// </summary>
    public static class SkillRegistration
    {
        private static readonly string[] IconShips = { "Karve", "VikingShip", "Raft" };

        /// <summary>Names the skill in a language the game set up before the plugin loaded. Reads the instance
        /// field, since Localization.instance would create the localization before the platform knows the locale.</summary>
        public static void Initialize()
        {
            Localization localization = Localization.m_instance;
            if (localization != null)
                AddName.Postfix(localization);
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.Awake))]
        private static class AddDefinition
        {
            [HarmonyPostfix]
            private static void Postfix(Skills __instance)
            {
                if (!__instance.m_skills.Exists(def => def != null && def.m_skill == SailingSkill.Type))
                    __instance.m_skills.Add(SailingSkill.Definition);
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.IsSkillValid))]
        private static class KeepSavedLevel
        {
            [HarmonyPostfix]
            private static void Postfix(Skills.SkillType type, ref bool __result) => __result |= type == SailingSkill.Type;
        }

        [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
        private static class AddName
        {
            [HarmonyPostfix]
            internal static void Postfix(Localization __instance)
            {
                __instance.AddWord(SailingSkill.LocalizationKey, SailingSkill.Name);
                __instance.m_cache.EvictAll();
            }
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class FindIcon
        {
            [HarmonyPostfix]
            private static void Postfix(ZNetScene __instance)
            {
                if (SailingSkill.Definition.m_icon == null)
                    SailingSkill.Definition.m_icon = ShipIcon(__instance);
            }
        }

        private static Sprite ShipIcon(ZNetScene scene)
        {
            foreach (string name in IconShips)
            {
                GameObject prefab = scene.GetPrefab(name);
                Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
                if (piece != null && piece.m_icon != null)
                    return piece.m_icon;
            }
            return null;
        }
    }
}
