using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Icons of our own for the game's skills this mod deepens: Cooking (<c>assets/skill_cooking.png</c>), Farming
    /// (<c>assets/skill_farming.png</c>), Fishing (<c>assets/skill_fishing.png</c>), Woodcutting
    /// (<c>assets/skill_woodcutting.png</c>) and Pickaxes (<c>assets/skill_pickaxes.png</c>). The game reads a skill's icon from its definition in the player's Skills
    /// (Skills.m_skills, SkillDef.m_icon; every Skill keeps a reference to its definition), so when a player's Skills
    /// wake those definitions get ours, and the skills panel and the level-up messages show them. The game's own icon
    /// stays when a file cannot be read, and on a machine without graphics (<see cref="EmbeddedIcon"/>). The mod's own
    /// skills (Sailing, Foraging, Defense, Husbandry) bring their icons through <see cref="CustomSkill"/>.
    /// </summary>
    public static class GameSkillIcons
    {
        private static readonly Dictionary<Skills.SkillType, string> Files = new Dictionary<Skills.SkillType, string>
        {
            { Skills.SkillType.Cooking, "skill_cooking" },
            { Skills.SkillType.Farming, "skill_farming" },
            { Skills.SkillType.Fishing, "skill_fishing" },
            { Skills.SkillType.WoodCutting, "skill_woodcutting" },
            { Skills.SkillType.Pickaxes, "skill_pickaxes" },
        };

        [HarmonyPatch(typeof(Skills), nameof(Skills.Awake))]
        private static class Waking
        {
            [HarmonyPostfix]
            private static void Postfix(Skills __instance) => HookGuard.Run("skill icons", () => Apply(__instance));
        }

        private static void Apply(Skills skills)
        {
            if (skills.m_skills == null)
                return;
            foreach (Skills.SkillDef definition in skills.m_skills)
            {
                if (definition == null || !Files.TryGetValue(definition.m_skill, out string file))
                    continue;
                Sprite icon = EmbeddedIcon.Load(file);
                if (icon != null)
                    definition.m_icon = icon;
            }
        }
    }
}
