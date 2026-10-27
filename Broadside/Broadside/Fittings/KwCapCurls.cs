using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The two curled post caps, each a carved board swept from its foot up into a spiral and thinning toward the tip: the
    /// scroll (about 1.1 m), whose stem bows back a little and rolls outward over itself round a round eye, and the tail
    /// curl (about 1.5 m), taller and slimmer, which leans outward as it climbs and curls back over the post, for sterns.
    /// Each stem is bent by the cap's lean (<see cref="KwCapPen.Bend"/>), its curl moved whole with the stem's top, before
    /// the board is swept along it. Kit axes: x outward, y across, z up from the middle of the post's top.
    /// </summary>
    public static class KwCapCurls
    {
        /// <summary>The height of each one's foot, where its carving starts.</summary>
        public const float ScrollFoot = 0.08f, TailFoot = 0.12f;

        private const float ScrollRise = 0.78f, ScrollBow = 0.06f, ScrollRadius = 0.3f, EyeRadius = 0.05f;
        private const float TailRise = 1.3f, TailLean = 0.18f, TailRadius = 0.22f;
        private const int ScrollSteps = 8, TailSteps = 10;

        // How far a board starts down inside its foot, so it stands on the foot however it leans as it leaves it.
        private const float Sink = 0.03f;

        /// <summary>The scroll: a turn and a half rolling outward, shrinking to under half its radius each turn, round an eye across the ship.</summary>
        public static void Scroll(KwCapPen b)
        {
            KwCaps.Foot(b, ScrollFoot, 0.145f, 0.195f);
            float rise = b.Rise(ScrollRise, ScrollFoot);
            List<Vector3> stem = Stem(ScrollFoot, rise, s => -ScrollBow * Mathf.Sin(Mathf.PI * s), ScrollSteps);
            Vector3 top = stem[stem.Count - 1];
            KwCapBend bend = b.Bend(ScrollFoot, rise);
            List<Vector3> line = stem.ConvertAll(p => bend.Kit(p));
            line.AddRange(KwCapSweep.Spiral(top, ScrollRadius, 0.42f, 1.5f, 24, 1f).ConvertAll(p => bend.Kit(p, true)));
            KwCapSweep.Tube(b, line, t => Taper(t, new Vector2(0.12f, 0.17f), new Vector2(0.035f, 0.06f)), KwCapSweep.Bevelled);
            Vector3 eye = KwCapSweep.SpiralCentre(top, ScrollRadius, 1f), half = new Vector3(0f, 0.075f, 0f);
            b.Log(bend.Kit(eye - half, true), bend.Kit(eye + half, true), EyeRadius, 6);
        }

        /// <summary>The tail curl: a long stem leaning outward, then a turn and a quarter curling back over the post.</summary>
        public static void Tail(KwCapPen b)
        {
            KwCaps.Foot(b, TailFoot, 0.11f, 0.13f);
            float rise = b.Rise(TailRise, TailFoot);
            List<Vector3> stem = Stem(TailFoot, rise, s => TailLean * Mathf.Sin(Mathf.PI / 2f * s), TailSteps);
            KwCapBend bend = b.Bend(TailFoot, rise);
            List<Vector3> line = stem.ConvertAll(p => bend.Kit(p));
            line.AddRange(KwCapSweep.Spiral(stem[stem.Count - 1], TailRadius, 0.4f, 1.25f, 20, -1f).ConvertAll(p => bend.Kit(p, true)));
            KwCapSweep.Tube(b, line, t => Taper(t, new Vector2(0.1f, 0.12f), new Vector2(0.025f, 0.04f)), KwCapSweep.Bevelled);
        }

        // A stem from inside the foot whose top is at z0 up to z1 in steps, standing out from the post's middle by x(s) at
        // share s of the way.
        private static List<Vector3> Stem(float z0, float z1, Func<float, float> x, int steps)
        {
            List<Vector3> line = new List<Vector3>();
            for (int i = 0; i <= steps; i++)
            {
                float s = (float)i / steps;
                line.Add(new Vector3(x(s), 0f, Mathf.Lerp(z0 - Sink, z1, s)));
            }
            return line;
        }

        // From the board's size at the foot to the tip's (half depth, half width), thinning a little faster low down.
        private static Vector2 Taper(float t, Vector2 foot, Vector2 tip) => Vector2.Lerp(foot, tip, Mathf.Pow(t, 0.8f));
    }
}
