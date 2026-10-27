using System.Collections.Generic;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cannons aboard counted for the reload lines (<see cref="KwReloadRows"/>), a side at a time, from each cannon's
    /// ZDO alone (the MannedWeapons library's <see cref="WeaponState"/>: its load, its reload step and that step's start,
    /// its last shot's moment, on the network clock), so every peer counts them alike whoever owns them and nothing is
    /// sent: loaded (ready), reloading (its step under way, or fired and resting before the reload the captain's or its
    /// gunner's game begins as the rest ends, <see cref="ReloadCycle.Rest"/>), or empty (nothing loaded and no reload under
    /// way or due). A manned cannon counts as any other: the broadside fires it too. A cannon is port when it was placed
    /// facing left of the ship's heading (its rest lay, where a broadside fires it), else starboard.
    /// </summary>
    public static class KwReloadCount
    {
        /// <summary>
        /// Seconds past the rest after a shot that a cannon with no reload step yet still counts as reloading, for a peer
        /// that reads another's cannon (the step's start in its ZDO on its way); then it counts empty.
        /// </summary>
        public const float Grace = 1f;

        /// <summary>The cannons aboard by side: port those placed facing left of the ship's heading, starboard the rest.</summary>
        public static void Count(Transform ship, List<KwArm> aboard, out KwSideCount port, out KwSideCount starboard)
        {
            (port, starboard) = (default, default);
            foreach (KwArm arm in aboard)
            {
                if (arm == null || arm.Frame == null || !arm.Link.Valid)
                    continue;
                if (Port(ship, arm))
                    port.Add(Left(arm.State, arm.Design.Reload));
                else
                    starboard.Add(Left(arm.State, arm.Design.Reload));
            }
        }

        /// <summary>Whether a cannon aboard is on the port side: placed facing left of the ship's heading (its rest lay, where a broadside fires it).</summary>
        public static bool Port(Transform ship, KwArm arm) => Vector3.Dot(arm.transform.forward, ship.right) < 0f;

        /// <summary>Seconds until the cannon is loaded: 0 when it is, less than 0 when it is empty with no reload under way or due.</summary>
        public static float Left(in WeaponState s, ReloadCycle reload)
        {
            float[] steps = reload.Steps;
            if (s.Loaded)
                return 0f;
            if (s.Busy)
                return steps[s.Step - 1] - s.StepTime + After(steps, s.Step);
            if (s.Step == 0 && s.ShotAge < reload.Rest + Grace)
                return Mathf.Max(0f, reload.Rest - s.ShotAge) + After(steps, s.Stage);
            return -1f;
        }

        // Seconds the steps after the first `done` take.
        private static float After(float[] steps, int done)
        {
            float sum = 0f;
            for (int i = done; i < steps.Length; i++)
                sum += steps[i];
            return sum;
        }
    }

    /// <summary>One side's cannons: how many, loaded, reloading and empty, and how long until the last reload is done.</summary>
    public struct KwSideCount
    {
        public int All, Ready, Reloading, Empty;

        /// <summary>Tenths of a second, rounded up, until the last reload under way or due on this side is done; 0 with none.</summary>
        public int Tenths;

        /// <summary>Everything the line shows as one number: when it is the same, the line is too.</summary>
        public long Shown => (((((long)All * 256 + Ready) * 256) + Reloading) * 256 + Empty) * 100000L + Tenths;

        /// <summary>A cannon counted by its seconds left (<see cref="KwReloadCount.Left"/>).</summary>
        public void Add(float left)
        {
            All++;
            if (left < 0f)
                Empty++;
            else if (left == 0f)
                Ready++;
            else
                (Reloading, Tenths) = (Reloading + 1, Mathf.Max(Tenths, Mathf.CeilToInt(left * 10f)));
        }
    }
}
