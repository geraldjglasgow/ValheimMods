using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Hotkeys
{
    /// <summary>
    /// The mouse wheel for a mod: while one of the mod's claims holds (a modifier held with the right tool in hand), the mod
    /// reads the wheel in notches and the rest of the game reads 0, so the camera does not zoom and nothing else scrolls.
    /// <see cref="Install"/> once per mod: one postfix on <c>ZInput.GetMouseScrollWheel</c>, which every system of the game
    /// reads. A claim is asked on every read of the wheel, so it must be cheap. Small wheel deltas (touchpads) add up to the
    /// game's own one-notch threshold, as the game does for piece rotation. EarthWright's brush has the same rule in its own
    /// <c>Brush/ScrollInput</c>, written before this.
    /// </summary>
    public static class Wheel
    {
        /// <summary>The wheel amount of one notch (the game's piece rotation threshold, <c>Player.m_scrollAmountThreshold</c>).</summary>
        private const float NotchThreshold = 0.1f;

        private static readonly List<Func<bool>> claims = new List<Func<bool>>();
        private static bool installed;
        private static bool bypass;
        private static float pending;

        public static void Install(Harmony harmony)
        {
            if (installed)
            {
                return;
            }
            installed = true;
            harmony.Patch(AccessTools.Method(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel)),
                postfix: new HarmonyMethod(typeof(Wheel), nameof(HideFromGame)));
        }

        /// <summary>While <paramref name="claim"/> holds, the wheel is the mod's. A claim that throws counts as not holding.</summary>
        public static void Claim(Func<bool> claim) => claims.Add(claim);

        /// <summary>One of the mod's claims holds now.</summary>
        public static bool Claimed
        {
            get
            {
                foreach (Func<bool> claim in claims)
                {
                    if (Holds(claim))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>The wheel this frame as the game reads it, past the mod's own block. Positive is away from the player.</summary>
        public static float Raw()
        {
            bypass = true;
            try
            {
                return ZInput.GetMouseScrollWheel();
            }
            finally
            {
                bypass = false;
            }
        }

        /// <summary>+1 (away from the player), -1 or 0 notches this frame; 0 while no claim holds. Call once a frame.</summary>
        public static int Notches()
        {
            if (!Claimed)
            {
                pending = 0f;
                return 0;
            }
            pending += Raw();
            if (Mathf.Abs(pending) < NotchThreshold)
            {
                return 0;
            }
            int step = pending > 0f ? 1 : -1;
            pending = 0f;
            return step;
        }

        private static bool Holds(Func<bool> claim)
        {
            try
            {
                return claim();
            }
            catch
            {
                return false;
            }
        }

        private static void HideFromGame(ref float __result)
        {
            if (__result != 0f && !bypass && Claimed)
            {
                __result = 0f;
            }
        }
    }
}
