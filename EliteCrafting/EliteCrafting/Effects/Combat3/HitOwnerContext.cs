using System;
using HarmonyLib;

namespace EliteCrafting.Effects.Combat3
{
    /// <summary>
    /// What the target's owner applies for the attacker of the hit it is resolving (Character.RPC_Damage returns at once
    /// on any other peer). The owner cannot see the attacker's gear, so it reads the attacker's player ZDO, which the
    /// attacker's own client writes: Lingering Wounds' total (<c>ecf_dot</c>, <see cref="PlayerStats"/>, on rebuild) and
    /// the penetration of the weapon it last swung (<c>ecf_pen</c>, <see cref="Penetration"/>, at the swing's start), each
    /// clamped to the running rules. A creature, or a player without the keys, reads 0. Set for the duration of the
    /// RPC only; a hit resolved inside another (Bramblehide's returned damage on a creature this peer owns) keeps its
    /// own values and restores the outer ones. Works on a dedicated server: it needs no local player.
    /// </summary>
    internal static class HitOwnerContext
    {
        public struct Saved
        {
            public float DotScale;
            public float ResistBypass;
        }

        /// <summary>How much longer the attacker's burning, poison and frost last (1 = as the game has it).</summary>
        public static float DotScale { get; private set; } = 1f;

        /// <summary>The share of each resistance the attacker's hit bypasses (0 = none).</summary>
        public static float ResistBypass { get; private set; }

        public static Saved Enter(Character target, HitData hit)
        {
            Saved outer = new Saved { DotScale = DotScale, ResistBypass = ResistBypass };
            bool owner = ItemEffects.Enabled && target.m_nview != null && target.m_nview.IsValid() && target.m_nview.IsOwner();
            bool fromPlayer = owner && !hit.m_attacker.IsNone();
            DotScale = fromPlayer ? 1f + PlayerStats.OfAttacker(hit, PlayerStats.Dot) : 1f;
            ResistBypass = fromPlayer ? Penetration.OfAttacker(hit) : 0f;
            return outer;
        }

        public static void Exit(Saved outer)
        {
            DotScale = Math.Max(1f, outer.DotScale);
            ResistBypass = Math.Max(0f, outer.ResistBypass);
        }

        public static void Reset()
        {
            DotScale = 1f;
            ResistBypass = 0f;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class HitOwnerContextPatch
    {
        private static void Prefix(Character __instance, HitData hit, out HitOwnerContext.Saved __state) =>
            __state = HitOwnerContext.Enter(__instance, hit);

        private static void Postfix(HitOwnerContext.Saved __state) => HitOwnerContext.Exit(__state);

        private static Exception? Finalizer(Exception? __exception)
        {
            if (__exception != null)
            {
                HitOwnerContext.Reset();
            }
            return __exception;
        }
    }
}
