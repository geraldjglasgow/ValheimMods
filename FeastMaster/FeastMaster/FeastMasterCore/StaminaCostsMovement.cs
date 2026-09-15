using HarmonyLib;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>
    /// Movement stamina costs. Each site reads a field of the player for its drain; the field is scaled in a
    /// prefix for the duration of the call and restored in a finalizer, so equipment and status effect modifiers
    /// keep working and settings hot reload: nothing is written permanently.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.CheckRun))]
    public static class RunCostPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, out ScaledFields __state)
        {
            float multiplier = Settings.RunCost.Value * CostRules.OutOfCombatFactor(__instance, Settings.OutOfCombatRunCost.Value);
            __state = CostRules.Scale(ref __instance.m_runStaminaDrain, multiplier);
        }

        [HarmonyFinalizer]
        public static void Finalizer(Player __instance, ScaledFields __state) => CostRules.Restore(ref __instance.m_runStaminaDrain, __state);
    }

    /// <summary>
    /// Jump: Character.Jump checks HaveStamina(m_jumpStaminaUsage) and then calls Player.OnJump, which drains
    /// from the same field; scaling it around Jump covers both, so a free jump is never refused for lack of stamina.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
    public static class JumpCostPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Character __instance, out ScaledFields __state)
        {
            __state = default;
            if (!(__instance is Player player))
                return;
            float multiplier = Settings.JumpCost.Value
                * CostRules.OutOfCombatFactor(player, Settings.OutOfCombatJumpCost.Value)
                * CostRules.SkillDiscountFactor(player, Skills.SkillType.Jump);
            __state = CostRules.Scale(ref __instance.m_jumpStaminaUsage, multiplier);
        }

        [HarmonyFinalizer]
        public static void Finalizer(Character __instance, ScaledFields __state) => CostRules.Restore(ref __instance.m_jumpStaminaUsage, __state);
    }

    /// <summary>Dodge: the game's helper returns the cost after equipment, status effects and its own skill discount.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetDodgeStaminaUse))]
    public static class DodgeCostPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, ref float __result)
        {
            __result *= Settings.DodgeCost.Value
                * CostRules.OutOfCombatFactor(__instance, Settings.OutOfCombatDodgeCost.Value)
                * CostRules.SkillDiscountFactor(__instance, Skills.SkillType.Dodge);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSneaking))]
    public static class SneakCostPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, out ScaledFields __state)
        {
            float multiplier = CostRules.SneakingIsFree(__instance)
                ? 0f
                : Settings.SneakCost.Value * CostRules.OutOfCombatFactor(__instance, Settings.OutOfCombatSneakCost.Value);
            __state = CostRules.Scale(ref __instance.m_sneakStaminaDrain, multiplier);
        }

        [HarmonyFinalizer]
        public static void Finalizer(Player __instance, ScaledFields __state) => CostRules.Restore(ref __instance.m_sneakStaminaDrain, __state);
    }

    /// <summary>Swim: the drain is a lerp between the min-skill and max-skill fields, both scaled.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSwimming))]
    public static class SwimCostPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, out ScaledFields __state)
        {
            __state = CostRules.Scale(ref __instance.m_swimStaminaDrainMinSkill, ref __instance.m_swimStaminaDrainMaxSkill, Settings.SwimCost.Value);
        }

        [HarmonyFinalizer]
        public static void Finalizer(Player __instance, ScaledFields __state)
        {
            CostRules.Restore(ref __instance.m_swimStaminaDrainMinSkill, ref __instance.m_swimStaminaDrainMaxSkill, __state);
        }
    }

    /// <summary>Encumbered: UpdateStats drains m_encumberedStaminaDrain per second while walking encumbered.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
    public static class EncumberedCostPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, out ScaledFields __state)
        {
            __state = CostRules.Scale(ref __instance.m_encumberedStaminaDrain, Mathf.Max(0f, Settings.EncumberedCost.Value));
        }

        [HarmonyFinalizer]
        public static void Finalizer(Player __instance, ScaledFields __state) => CostRules.Restore(ref __instance.m_encumberedStaminaDrain, __state);
    }
}
