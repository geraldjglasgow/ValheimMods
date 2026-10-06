using HarmonyLib;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>
    /// Stamina Regen Multiplier, Low Stamina Regen Bonus and Eitr Regen Multiplier scale the player's base regen
    /// fields for the duration of UpdateStats, the way the costs scale their drain fields: nothing is written
    /// permanently, so the settings hot reload. Asked by <see cref="UpdateStatsPatch"/>, the one UpdateStats patch.
    /// </summary>
    public static class RegenBasicsRule
    {
        public static bool Rules() => Customized.Any(Settings.StaminaRegenMultiplier, Settings.LowStaminaRegenBonus, Settings.EitrRegenMultiplier);

        public static ScaledGroup Scale(Player player)
        {
            return new ScaledGroup
            {
                First = CostRules.Scale(ref player.m_staminaRegen, Mathf.Max(0f, Settings.StaminaRegenMultiplier.Value)),
                Second = CostRules.Scale(ref player.m_staminaRegenTimeMultiplier, Mathf.Max(0f, Settings.LowStaminaRegenBonus.Value)),
                Third = CostRules.Scale(ref player.m_eiterRegen, Mathf.Max(0f, Settings.EitrRegenMultiplier.Value)),
            };
        }

        public static void Restore(Player player, ScaledGroup saved)
        {
            CostRules.Restore(ref player.m_staminaRegen, saved.First);
            CostRules.Restore(ref player.m_staminaRegenTimeMultiplier, saved.Second);
            CostRules.Restore(ref player.m_eiterRegen, saved.Third);
        }
    }

    /// <summary>Stamina Regen Delay: RPC_UseStamina sets the regen timer from m_staminaRegenDelay.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.RPC_UseStamina))]
    public static class StaminaRegenDelayPatch
    {
        public static bool Prepare() => Customized.Any(Settings.StaminaRegenDelay);

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
        public static bool Prepare() => Customized.Any(Settings.EitrRegenDelay);

        [HarmonyPrefix]
        public static void Prefix(Player __instance, out ScaledFields __state)
        {
            __state = CostRules.Swap(ref __instance.m_eitrRegenDelay, Mathf.Max(0f, Settings.EitrRegenDelay.Value));
        }

        [HarmonyFinalizer]
        public static void Finalizer(Player __instance, ScaledFields __state) => CostRules.Restore(ref __instance.m_eitrRegenDelay, __state);
    }
}
