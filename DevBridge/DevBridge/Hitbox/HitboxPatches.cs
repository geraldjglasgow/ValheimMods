using System;
using DevBridge.Events;
using HarmonyLib;

namespace DevBridge.Hitbox
{
    /// <summary>Where /hitbox looks: a melee swing as the game casts it, and a hit as it reaches the local player.
    /// Neither changes anything, and a failure in either is logged and swallowed so the game's own attack goes on.
    /// While /hitbox is off each leaves at once; the work is a static lambda with its arguments passed in, so no call
    /// allocates a closure. The hit has no patch of its own: CharacterPatches' one RPC_Damage prefix calls Arrived. A
    /// failure is logged once (Publish.WarnOnce), so a shape that throws on every swing cannot flood the log.</summary>
    internal static class HitboxPatches
    {
        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        private static class Swing
        {
            private static void Prefix(Attack __instance)
            {
                Character attacker = __instance.m_character;
                if (!HitboxView.On || attacker == null || (attacker.IsPlayer() && !HitboxView.Players)) return;
                Safely(static attack => SwingShape.Show(attack), __instance);
            }
        }

        /// <summary>A hit reaching a character, from CharacterPatches' RPC_Damage prefix while /hitbox is on.</summary>
        internal static void Arrived(Character target, HitData hit)
        {
            if (target != Player.m_localPlayer) return;
            Safely(static (player, h) => HitReport.Record(player, h), target, hit);
        }

        private static void Safely<T>(Action<T> action, T arg)
        {
            try
            {
                action(arg);
            }
            catch (Exception error)
            {
                Warn(error);
            }
        }

        private static void Safely<T1, T2>(Action<T1, T2> action, T1 first, T2 second)
        {
            try
            {
                action(first, second);
            }
            catch (Exception error)
            {
                Warn(error);
            }
        }

        private static void Warn(Exception error) => Publish.WarnOnce("hitbox", error);
    }
}
