using HarmonyLib;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>
    /// Stamina Regen Multiplier, Low Stamina Regen Bonus and Eitr Regen Multiplier scale the player's base regen
    /// fields for the duration of UpdateStats, the way the costs scale their drain fields: nothing is written
    /// permanently, so the settings hot reload.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
    public static class RegenBasicsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, out ScaledFields __state)
        {
            __state = new ScaledFields
            {
                Set = true,
                First = __instance.m_staminaRegen,
                Second = __instance.m_staminaRegenTimeMultiplier,
                Third = __instance.m_eiterRegen,
            };
            __instance.m_staminaRegen *= Mathf.Max(0f, Settings.StaminaRegenMultiplier.Value);
            __instance.m_staminaRegenTimeMultiplier *= Mathf.Max(0f, Settings.LowStaminaRegenBonus.Value);
            __instance.m_eiterRegen *= Mathf.Max(0f, Settings.EitrRegenMultiplier.Value);
        }

        [HarmonyFinalizer]
        public static void Finalizer(Player __instance, ScaledFields __state)
        {
            if (!__state.Set)
                return;
            __instance.m_staminaRegen = __state.First;
            __instance.m_staminaRegenTimeMultiplier = __state.Second;
            __instance.m_eiterRegen = __state.Third;
        }
    }

    /// <summary>Stamina Regen Delay: RPC_UseStamina sets the regen timer from m_staminaRegenDelay.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.RPC_UseStamina))]
    public static class StaminaRegenDelayPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, out ScaledFields __state)
        {
            __state = CostRules.Swap(ref __instance.m_staminaRegenDelay, Mathf.Max(0f, Settings.StaminaRegenDelay.Value));
        }

        [HarmonyFinalizer]
        public static void Finalizer(Player __instance, ScaledFields __state) => CostRules.Restore(ref __instance.m_staminaRegenDelay, __state);
    }

    /// <summary>Eitr Regen Delay: RPC_UseEitr sets the eitr regen timer from m_eitrRegenDelay.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.RPC_UseEitr))]
    public static class EitrRegenDelayPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, out ScaledFields __state)
        {
            __state = CostRules.Swap(ref __instance.m_eitrRegenDelay, Mathf.Max(0f, Settings.EitrRegenDelay.Value));
        }

        [HarmonyFinalizer]
        public static void Finalizer(Player __instance, ScaledFields __state) => CostRules.Restore(ref __instance.m_eitrRegenDelay, __state);
    }
}
