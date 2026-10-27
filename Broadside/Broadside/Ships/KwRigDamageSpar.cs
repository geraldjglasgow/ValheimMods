using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A damaged spar's mesh (<see cref="KwRigDamageLooks"/>), in its piece's kit axes as <see cref="KwRound.Taper"/> draws a
    /// whole one (the same sides, ways across, UVs and two faces): rings along it, close together through each notch, each
    /// ring's points on the notch's side brought in to the notch's chord (<see cref="KwRigSection.ChordAt"/>), a little
    /// ragged, so the notch is a scoop out of the wood the shot came from; a spar broken through drawn only to the break,
    /// its end splintered (<see cref="Ragged"/>). Every peer that draws makes the same from the marks.
    /// </summary>
    public static class KwRigDamageSpar
    {
        private const int Through = 9;
        private const float Splinter = 0.09f, Least = 0.12f;

        /// <summary>The spar from one place along it to another, notched; its ends splintered where asked (a break).</summary>
        public static Mesh Notched(KwRigSpar spar, List<KwRigMark> notches, float from, float to, bool raggedFrom, bool raggedTo, int seed)
        {
            MeshBuilder b = new MeshBuilder(Vector3.zero);
            Build(b, spar, notches, (from, to), (raggedFrom, raggedTo), seed);
            return b.ToMesh("KW_spar_damaged");
        }

        /// <summary>The notched spar's faces into a builder (<see cref="Notched"/>), from and to along it, ragged at either end.</summary>
        public static void Build(MeshBuilder b, KwRigSpar spar, List<KwRigMark> notches, (float From, float To) span, (bool From, bool To) ragged, int seed)
        {
            (float from, float to) = span;
            (bool raggedFrom, bool raggedTo) = ragged;
            List<float> rings = Rings(spar, notches, from, to);
            List<Vector3[]> points = rings.ConvertAll(t => Ring(spar, notches, t));
            if (raggedFrom)
                points[0] = Ragged(spar, points[0], rings[0], -1f, seed);
            if (raggedTo)
                points[points.Count - 1] = Ragged(spar, points[points.Count - 1], rings[rings.Count - 1], 1f, seed + 1);
            for (int k = 0; k + 1 < rings.Count; k++)
                Band(b, points[k], points[k + 1], rings[k], rings[k + 1]);
            Cap(b, points[0], spar.At(rings[0]));
            Cap(b, points[points.Count - 1], spar.At(rings[rings.Count - 1]));
        }

        // The places along it a ring goes: its ends, its middle for a yard, and close together through every notch.
        private static List<float> Rings(KwRigSpar spar, List<KwRigMark> notches, float from, float to)
        {
            List<float> at = new List<float> { from, to };
            if (spar.RMid > 0f)
                at.Add(spar.Length / 2f);
            foreach (KwRigMark notch in notches)
            {
                for (int k = 0; k < Through; k++)
                    at.Add(notch.A - notch.D + 2f * notch.D * k / (Through - 1));
            }
            at = at.FindAll(t => t >= from - 1e-4f && t <= to + 1e-4f);
            at.Sort();
            List<float> rings = new List<float>();
            foreach (float t in at)
            {
                if (rings.Count == 0 || t - rings[rings.Count - 1] > 0.002f)
                    rings.Add(t);
            }
            return rings;
        }

        /// <summary>A ring's points at a place along the spar, each brought in to the deepest notch's chord there.</summary>
        public static Vector3[] Ring(KwRigSpar spar, List<KwRigMark> notches, float t)
        {
            Vector3[] ring = new Vector3[spar.Sides];
            Vector3 centre = spar.At(t);
            float r = spar.Radius(Mathf.Clamp(t, 0f, spar.Length));
            for (int i = 0; i < spar.Sides; i++)
            {
                float angle = i * 360f / spar.Sides;
                ring[i] = centre + spar.Out(angle) * (r * Depth(notches, t, angle, i));
            }
            return ring;
        }

        // How far out a ring's point at an angle stays (a share of the radius): in to the chord of each notch it faces.
        private static float Depth(List<KwRigMark> notches, float t, float angle, int side)
        {
            float out1 = 1f;
            foreach (KwRigMark notch in notches)
            {
                if (Mathf.Abs(t - notch.A) >= notch.D)
                    continue;
                float facing = Mathf.Cos((angle - notch.B) * Mathf.Deg2Rad);
                float chord = KwRigSection.ChordAt(KwRigSection.Chord(notch.C), t - notch.A, notch.D);
                chord += 0.08f * (KwWearNoise.Hash(side, notch.Seed + Mathf.RoundToInt(t * 50f)) - 0.5f) * (1f - chord);
                if (facing > 0.01f && facing > chord)
                    out1 = Mathf.Min(out1, Mathf.Max(Least, chord / facing));
            }
            return out1;
        }

        // An end ring splintered: each point moved along the spar by its own amount, out past the end (way) or back into it.
        private static Vector3[] Ragged(KwRigSpar spar, Vector3[] ring, float t, float way, int seed)
        {
            Vector3[] made = new Vector3[ring.Length];
            for (int i = 0; i < ring.Length; i++)
            {
                float jag = Splinter * (KwWearNoise.Hash(i, seed) - (i % 2 == 0 ? 0.15f : 0.75f));
                made[i] = ring[i] + spar.Axis * (way * jag);
            }
            return made;
        }

        // The side quads between two rings, both faces, UVs as KwRound's (along half a unit a metre, round 0 to 1).
        private static void Band(MeshBuilder b, Vector3[] a, Vector3[] c, float ta, float tc)
        {
            int n = a.Length;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                float s0 = (float)i / n, s1 = (float)(i + 1) / n;
                b.Polygon(new List<Vector3> { a[i], c[i], c[j], a[j] },
                    new List<Vector2> { new Vector2(ta * 0.5f, s0), new Vector2(tc * 0.5f, s0), new Vector2(tc * 0.5f, s1), new Vector2(ta * 0.5f, s1) });
            }
        }

        // An end closed: a fan from its middle (splintered ends make it a rough point).
        private static void Cap(MeshBuilder b, Vector3[] ring, Vector3 middle)
        {
            List<Vector3> rim = new List<Vector3>(ring);
            List<Vector2> uv = rim.ConvertAll(p => new Vector2(0.02f * (p - middle).x, 0.02f * (p - middle).y));
            b.Polygon(rim, uv);
        }
    }
}
