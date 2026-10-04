using HarmonyLib;
using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>After landing, the local player takes no damage of any kind for a few seconds. Every hit reaches a
    /// character through the owner's <c>Character.RPC_Damage</c> (verified in the decompiled assembly):
    /// <c>Character.Damage</c> sends it there, and fall damage (<c>UpdateGroundContact</c>, a drop over 4 m), drowning
    /// (<c>Player.OnSwimming</c> with no stamina), the wet and cold ticks (<c>SE_Wet</c>, <c>SE_Stats</c>) all call
    /// <c>Damage</c>. Burning, poison and smoke ticks call <c>Character.ApplyDamage</c> directly, which
    /// <c>RPC_Damage</c> also ends in. Both are stopped for the local player, which owns its own character; skipping
    /// <c>RPC_Damage</c> whole also drops the hit's push, stagger and status effect. Not gated on the settings: it only
    /// starts at the end of a jump, and finishing a jump safely comes first.</summary>
    public static class JumpProtection
    {
        private static float until;

        public static bool Active => Time.time < until;

        public static void Start(float seconds)
        {
            until = Mathf.Max(until, Time.time + Mathf.Max(0f, seconds));
        }

        public static void Clear() => until = 0f;

        internal static bool Shields(Character character) =>
            Active && character != null && character == Player.m_localPlayer;
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public static class JumpProtectionHitPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Character __instance) => !JumpProtection.Shields(__instance);
    }

    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class JumpProtectionDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Character __instance) => !JumpProtection.Shields(__instance);
    }
}
