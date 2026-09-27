using HarmonyLib;
using PlateColumn;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrindstoneSkills
{
    /// <summary>
    /// Farming's icon in the skills panel and in "skill increased" messages: <c>assets/skill_farming.png</c> embedded in the
    /// DLL (64x64, like the game's skill icons). The game reads a skill's icon from its definition in the player's Skills
    /// (Skills.m_skills, SkillDef.m_icon; every Skill keeps a reference to its definition), so when a player's Skills wake
    /// the Farming definition gets it. The game's own icon stays when the file cannot be read, and on a machine without
    /// graphics (a dedicated server), which never shows icons. The embedded file is decoded once.
    /// </summary>
    public static class FarmingIcon
    {
        public const string Resource = "GrindstoneSkills.assets.skill_farming.png";

        private static Sprite embedded;
        private static bool tried;

        public static Sprite Find()
        {
            if (tried)
                return embedded;
            tried = true;
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                embedded = EmbeddedSprite.Load(typeof(FarmingIcon).Assembly, Resource, "skill_farming");
            return embedded;
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.Awake))]
        private static class Waking
        {
            [HarmonyPostfix]
            private static void Postfix(Skills __instance) => HookGuard.Run("Farming icon", () => Apply(__instance));
        }

        private static void Apply(Skills skills)
        {
            Sprite icon = Find();
            if (icon == null || skills.m_skills == null)
                return;
            foreach (Skills.SkillDef definition in skills.m_skills)
            {
                if (definition != null && definition.m_skill == FarmSkill.Skill)
                    definition.m_icon = icon;
            }
        }
    }
}
