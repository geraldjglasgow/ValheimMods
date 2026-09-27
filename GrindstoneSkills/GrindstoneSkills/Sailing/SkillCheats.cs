using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The console's raiseskill and resetskill (with devcommands) find a skill by its enum name, so "sailing" would be
    /// "Skill not found" and "all" would pass Sailing by. "sailing" is handled here the way the game handles its own
    /// skills, and "all" includes Sailing after the game's own run. Mainly for testing the lookout's level.
    /// </summary>
    public static class SkillCheats
    {
        [HarmonyPatch(typeof(Skills), nameof(Skills.CheatRaiseSkill))]
        private static class Raise
        {
            [HarmonyPrefix]
            private static bool Prefix(Skills __instance, string name, float value, bool showMessage)
            {
                if (!Is(name, SailingSkill.Name))
                    return true;
                RaiseBy(__instance, value, showMessage);
                return false;
            }

            [HarmonyPostfix]
            private static void Postfix(Skills __instance, string name, float value)
            {
                if (Is(name, "all"))
                    RaiseBy(__instance, value, false);
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.CheatResetSkill))]
        private static class Reset
        {
            [HarmonyPrefix]
            private static bool Prefix(Skills __instance, string name)
            {
                if (!Is(name, SailingSkill.Name))
                    return true;
                __instance.ResetSkill(SailingSkill.Type);
                Console.instance?.Print($"Skill {SailingSkill.Name} reset");
                return false;
            }

            [HarmonyPostfix]
            private static void Postfix(Skills __instance, string name)
            {
                if (Is(name, "all"))
                    __instance.ResetSkill(SailingSkill.Type);
            }
        }

        private static bool Is(string name, string word) => name != null && name.ToLowerInvariant() == word.ToLowerInvariant();

        /// <summary>The game's CheatRaiseSkill for one skill: add, clamp to 0..100, rebalance under a skill cap, report.</summary>
        private static void RaiseBy(Skills skills, float value, bool showMessage)
        {
            Skills.Skill skill = skills.GetSkill(SailingSkill.Type);
            skill.m_level = Mathf.Clamp(skill.m_level + value, 0f, SailingSkill.MaxLevel);
            if (skills.m_useSkillCap)
                skills.RebalanceSkills(SailingSkill.Type);
            if (!showMessage)
                return;
            skills.m_player.Message(MessageHud.MessageType.TopLeft, $"Skill increased {SailingSkill.Name}: {(int)skill.m_level}", 0, skill.m_info.m_icon);
            Console.instance?.Print($"Skill {SailingSkill.Name} = {skill.m_level}");
        }
    }
}
