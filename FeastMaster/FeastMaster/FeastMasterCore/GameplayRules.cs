using HarmonyLib;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>Eat Again At: the game allows re-eating below half of the food's duration.</summary>
    [HarmonyPatch(typeof(Player.Food), nameof(Player.Food.CanEatAgain))]
    public static class EatAgainPatch
    {
        private const float GameFraction = 0.5f;

        [HarmonyPostfix]
        public static void Postfix(Player.Food __instance, ref bool __result)
        {
            float fraction = Mathf.Clamp01(FeastMasterData.EatAgainAt.Value);
            if (Mathf.Approximately(fraction, GameFraction))
                return;
            __result = __instance.m_time < __instance.m_item.m_shared.m_foodBurnTime * fraction;
        }
    }

    /// <summary>Skill gain multipliers: Skills.RaiseSkill(type, factor) is the single entry for skill experience.</summary>
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    public static class SkillGainPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Skills.SkillType skillType, ref float factor)
        {
            factor *= Mathf.Max(0f, Multiplier(skillType));
        }

        private static float Multiplier(Skills.SkillType skill)
        {
            switch (skill)
            {
                case Skills.SkillType.Run: return Settings.RunSkillGain.Value;
                case Skills.SkillType.Jump: return Settings.JumpSkillGain.Value;
                case Skills.SkillType.Sneak: return Settings.SneakSkillGain.Value;
                case Skills.SkillType.Swim: return Settings.SwimSkillGain.Value;
                case Skills.SkillType.Fishing: return Settings.FishingSkillGain.Value;
                default: return 1f;
            }
        }
    }

    /// <summary>
    /// Drowning Damage: Player.OnSwimming builds a HitType.Drowning hit of Ceil(maxHealth / 20) each second without
    /// stamina and passes it to Character.Damage; the damage is scaled there for players.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    public static class DrowningDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null || hit.m_hitType != HitData.HitType.Drowning || !(__instance is Player))
                return;
            hit.m_damage.m_damage *= Mathf.Max(0f, Settings.DrowningDamage.Value);
        }
    }
}
