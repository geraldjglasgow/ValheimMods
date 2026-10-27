using System.Collections.Generic;
using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The shot arcs, on the captain's own screen only (nothing is sent): <see cref="KwBroadside.ArcsKey"/> at the helm of a
    /// launched Broadside ship with cannons aboard turns them on and off (<see cref="On"/>, kept for the session, not
    /// saved, also between helms). While on and steering, each cannon aboard a broadside would fire (manned ones too) shows
    /// its ball's flight if fired now (<see cref="KwShotPath"/>, the flight the called shot really flies: from the muzzle
    /// along the barrel as drawn, the ship's motion carried; a manned one's barrel follows its gunner's aim, where his
    /// shot goes) as a thin, faint white dotted arc ending where it would strike (no marker there: the rings the strike
    /// once had, green on a target and pale on the sea, were taken off at the user's word on 2026-10-26, "the little white
    /// lines, those are just fine"); a cannon not loaded (reloading or empty) dimmed. Worked out again every
    /// <see cref="Every"/> seconds and drawn under the ship (<see cref="KwShotArcLook"/>),
    /// so between two looks they ride with it. Hidden at once off the helm, without cannons or when turned off.
    /// </summary>
    public static class KwShotArcs
    {
        /// <summary>Seconds between two looks (20 a second).</summary>
        public const float Every = 0.05f;

        private static readonly List<Vector3> points = new List<Vector3>();
        private static readonly KwShotArcLook look = new KwShotArcLook();
        private static float next;

        /// <summary>Whether the arcs are on: the captain's toggle, for this session.</summary>
        public static bool On { get; set; }

        /// <summary>Each frame on the captain's game: the arcs worked out and drawn when due, or hidden.</summary>
        public static void Tick(Player? captain, Ship? ship, List<KwArm>? aboard)
        {
            if (!On || captain == null || ship == null || aboard == null || aboard.Count == 0)
            {
                look.Hide();
                return;
            }
            if (Time.time < next)
                return;
            next = Time.time + Every;
            look.Begin(ship.transform);
            foreach (KwArm arm in aboard)
            {
                if (arm != null && arm.Link.Valid && arm.Frame != null)
                    Draw(arm, captain);
            }
            look.End();
        }

        // One cannon's arc, to where it strikes, dimmed when it is not loaded.
        private static void Draw(KwArm arm, Player captain)
        {
            KwShot shot = KwArmTrigger.Of(arm.Def, arm.Link.Zdo.GetInt(arm.Link.LoadKind));
            KwStrike strike = KwShotPath.Fly(arm, shot, captain, points);
            WeaponState s = arm.State;
            points.Add(strike.Point);
            look.Arc(points, s.Loaded && !s.Busy);
        }
    }
}
