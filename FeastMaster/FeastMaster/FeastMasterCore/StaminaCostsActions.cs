using HarmonyLib;

namespace FeastMaster
{
    /// <summary>Block: normal and perfect block drains are fields of the Humanoid, scaled around BlockAttack.</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    public static class BlockCostPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Humanoid __instance, out ScaledFields __state)
        {
            __state = default;
            if (!(__instance is Player player))
                return;
            float multiplier = Settings.BlockCost.Value * CostRules.SkillDiscountFactor(player, Skills.SkillType.Blocking);
            __state = CostRules.Scale(ref __instance.m_blockStaminaDrain, ref __instance.m_perfectBlockStaminaDrain, multiplier);
        }

        [HarmonyFinalizer]
        public static void Finalizer(Humanoid __instance, ScaledFields __state)
        {
            CostRules.Restore(ref __instance.m_blockStaminaDrain, ref __instance.m_perfectBlockStaminaDrain, __state);
        }
    }

    /// <summary>
    /// Attack: the game's helper returns the cost after equipment, status effects and the weapon skill. Attacks of
    /// home items (hammer, hoe, cultivator swings) take the Tool Cost instead, like their placement.
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
    public static class AttackCostPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Attack __instance, ref float __result)
        {
            if (!(__instance.m_character is Player))
                return;
            __result *= __instance.m_isHomeItem ? Settings.ToolCost.Value : Settings.AttackCost.Value;
        }
    }

    /// <summary>Tool: placement with the hammer, hoe and cultivator.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetBuildStamina))]
    public static class ToolCostPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result) => __result *= Settings.ToolCost.Value;
    }

    /// <summary>
    /// Fishing: FishingFloat.FixedUpdate drains the hooked cost per second while a fish is on the line and the
    /// pull cost per second while reeling in. The pull cost also includes the fish's own stamina use, scaled in
    /// <see cref="FishStaminaUsePatch"/>.
    /// </summary>
    [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.FixedUpdate))]
    public static class FishingCostPatch
    {
        [HarmonyPrefix]
        public static void Prefix(FishingFloat __instance, out ScaledFields __state)
        {
            __state = new ScaledFields
            {
                Set = true,
                First = __instance.m_hookedStaminaPerSec,
                Second = __instance.m_hookedStaminaPerSecMaxSkill,
                Third = __instance.m_pullStaminaUse,
            };
            __instance.m_hookedStaminaPerSec *= Settings.FishingHookedCost.Value;
            __instance.m_hookedStaminaPerSecMaxSkill *= Settings.FishingHookedCost.Value;
            __instance.m_pullStaminaUse *= Settings.FishingPullCost.Value;
        }

        [HarmonyFinalizer]
        public static void Finalizer(FishingFloat __instance, ScaledFields __state)
        {
            if (!__state.Set)
                return;
            __instance.m_hookedStaminaPerSec = __state.First;
            __instance.m_hookedStaminaPerSecMaxSkill = __state.Second;
            __instance.m_pullStaminaUse = __state.Third;
        }
    }

    /// <summary>The fish's contribution to the pull cost (only used by FishingFloat's pull drain).</summary>
    [HarmonyPatch(typeof(Fish), nameof(Fish.GetStaminaUse))]
    public static class FishStaminaUsePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result) => __result *= Settings.FishingPullCost.Value;
    }

    /// <summary>Harpoon: the status effect on the harpooned creature drains the puller's stamina from m_staminaDrain.</summary>
    [HarmonyPatch(typeof(SE_Harpooned), nameof(SE_Harpooned.UpdateStatusEffect))]
    public static class HarpoonCostPatch
    {
        [HarmonyPrefix]
        public static void Prefix(SE_Harpooned __instance, out ScaledFields __state)
        {
            __state = CostRules.Scale(ref __instance.m_staminaDrain, Settings.HarpoonCost.Value);
        }

        [HarmonyFinalizer]
        public static void Finalizer(SE_Harpooned __instance, ScaledFields __state) => CostRules.Restore(ref __instance.m_staminaDrain, __state);
    }
}
