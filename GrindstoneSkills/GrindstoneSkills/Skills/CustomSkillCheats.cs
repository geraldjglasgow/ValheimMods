using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The console's raiseskill and resetskill (with devcommands) find a skill by its enum name, so "sailing" would be
    /// "Skill not found" and "all" would pass the mod's own skills by. Each <see cref="CustomSkill"/>'s name is handled
    /// here the way the game handles its own skills, and "all" includes every one after the game's own run.
    /// </summary>
    public static class CustomSkillCheats
    {
        [HarmonyPatch(typeof(Skills), nameof(Skills.CheatRaiseSkill))]
        private static class Raise
        {
            [HarmonyPrefix]
            private static bool Prefix(Skills __instance, string name, float value, bool showMessage)
            {
                CustomSkill skill = CustomSkills.Find(name);
                if (skill == null)
                    return true;
                RaiseBy(__instance, skill, value, showMessage);
                return false;
            }

            [HarmonyPostfix]
            private static void Postfix(Skills __instance, string name, float value)
            {
                if (!IsAll(name))
                    return;
                foreach (CustomSkill skill in CustomSkills.All)
                    RaiseBy(__instance, skill, value, false);
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.CheatResetSkill))]
        private static class Reset
        {
            [HarmonyPrefix]
            private static bool Prefix(Skills __instance, string name)
            {
                CustomSkill skill = CustomSkills.Find(name);
                if (skill == null)
                    return true;
                __instance.ResetSkill(skill.Type);
                Console.instance?.Print($"Skill {skill.Name} reset");
                return false;
            }

            [HarmonyPostfix]
            private static void Postfix(Skills __instance, string name)
            {
                if (!IsAll(name))
                    return;
                foreach (CustomSkill skill in CustomSkills.All)
                    __instance.ResetSkill(skill.Type);
            }
        }

        private static bool IsAll(string name) => name != null && name.ToLowerInvariant() == "all";

        /// <summary>The game's CheatRaiseSkill for one skill: add, clamp to 0..100, rebalance under a skill cap, report.</summary>
        private static void RaiseBy(Skills skills, CustomSkill custom, float value, bool showMessage)
        {
            Skills.Skill skill = skills.GetSkill(custom.Type);
            skill.m_level = Mathf.Clamp(skill.m_level + value, 0f, CustomSkill.MaxLevel);
            if (skills.m_useSkillCap)
                skills.RebalanceSkills(custom.Type);
            if (!showMessage)
                return;
            skills.m_player.Message(MessageHud.MessageType.TopLeft, $"Skill increased {custom.Name}: {(int)skill.m_level}", 0, skill.m_info?.m_icon);
            Console.instance?.Print($"Skill {custom.Name} = {skill.m_level}");
        }
    }
}
