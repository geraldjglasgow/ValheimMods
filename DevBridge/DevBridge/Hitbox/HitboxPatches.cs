using System;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Hitbox
{
    /// <summary>Where /hitbox looks: a melee swing as the game casts it, and a hit as it reaches the local player.
    /// Neither changes anything, and a failure in either is logged and swallowed so the game's own attack goes on.</summary>
    internal static class HitboxPatches
    {
        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        private static class Swing
        {
            private static void Prefix(Attack __instance)
            {
                Character attacker = __instance.m_character;
                if (!HitboxView.On || attacker == null || (attacker.IsPlayer() && !HitboxView.Players)) return;
                Safely(() => SwingShape.Show(__instance));
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class Hit
        {
            private static void Prefix(Character __instance, HitData hit)
            {
                if (!HitboxView.On || hit == null || __instance != Player.m_localPlayer) return;
                Safely(() => HitReport.Record(__instance, hit));
            }
        }

        private static void Safely(Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[DevBridge] hitbox: {error.GetType().Name}: {error.Message}");
            }
        }
    }
}
