using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Icons of our own for every skill of the game, <c>assets/skill_&lt;name&gt;.png</c>, painted as one set (the masters,
    /// prompts and review board are in <c>artwork/skill-icons-v2</c>). The game reads a skill's icon from its definition
    /// in the player's Skills (Skills.m_skills, SkillDef.m_icon; every Skill keeps a reference to its definition), so when
    /// a player's Skills wake those definitions get ours, and the skills panel and the level-up messages show them. The
    /// game's own icon stays when a file cannot be read, and on a machine without graphics (<see cref="EmbeddedIcon"/>).
    /// The mod's own skills (Sailing, Foraging, Defense, Husbandry) bring their icons through <see cref="CustomSkill"/>.
    /// </summary>
    public static class GameSkillIcons
    {
        private static readonly Dictionary<Skills.SkillType, string> Files = new Dictionary<Skills.SkillType, string>
        {
            { Skills.SkillType.Swords, "skill_swords" },
            { Skills.SkillType.Knives, "skill_knives" },
            { Skills.SkillType.Clubs, "skill_clubs" },
            { Skills.SkillType.Polearms, "skill_polearms" },
            { Skills.SkillType.Spears, "skill_spears" },
            { Skills.SkillType.Blocking, "skill_blocking" },
            { Skills.SkillType.Axes, "skill_axes" },
            { Skills.SkillType.Bows, "skill_bows" },
            { Skills.SkillType.ElementalMagic, "skill_elementalmagic" },
            { Skills.SkillType.BloodMagic, "skill_bloodmagic" },
            { Skills.SkillType.Unarmed, "skill_unarmed" },
            { Skills.SkillType.Pickaxes, "skill_pickaxes" },
            { Skills.SkillType.WoodCutting, "skill_woodcutting" },
            { Skills.SkillType.Crossbows, "skill_crossbows" },
            { Skills.SkillType.Jump, "skill_jump" },
            { Skills.SkillType.Sneak, "skill_sneak" },
            { Skills.SkillType.Run, "skill_run" },
            { Skills.SkillType.Swim, "skill_swim" },
            { Skills.SkillType.Fishing, "skill_fishing" },
            { Skills.SkillType.Cooking, "skill_cooking" },
            { Skills.SkillType.Farming, "skill_farming" },
            { Skills.SkillType.Crafting, "skill_crafting" },
            { Skills.SkillType.Dodge, "skill_dodge" },
            { Skills.SkillType.Ride, "skill_ride" },
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
