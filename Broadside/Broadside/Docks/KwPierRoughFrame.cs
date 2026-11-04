using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The rough wood dock's frame under and round its deck (<see cref="KwPierRough"/>): joists along under the boards, each
    /// its own width and a little off its line, cut into runs at slightly different heights; a cap beam across at each
    /// station (none at a joined start), its own width, a little askew, its ends past the sides by odd amounts (flush where
    /// another dock or a ladder meets that side); a crooked pole piling pair at each station (<see cref="KwPierRoughPole"/>,
    /// some lashed to the cap with rope); and an uneven curb along each side, pieces of odd lengths, heights and widths with
    /// small gaps between, left out where the side is open. The curbs' pieces are drawn from a seed of their own, so a
    /// branch or a ladder built later cuts them without moving anything else. The same framing heights as the neat docks
    /// (<see cref="KwPierKind"/>), so the colliders fit. Plain geometry.
    /// </summary>
    public static class KwPierRoughFrame
    {
        private const float JoistSpacing = 0.9f, JoistShift = 0.03f, JoistStep = 0.006f, CapOverLeast = 0.02f, CapOverMost = 0.18f, CapSkew = 0.015f;
        private const float CurbLeast = 0.6f, CurbMost = 2.4f, CurbGapShare = 0.65f, CurbShortest = 0.1f;

        public static void Build(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random, KwPierGaps gaps)
        {
            Joists(mesh, spec, kind, uv, random);
            for (int i = 0; i < spec.Stations; i++)
            {
                Vector3 cap = Cap(mesh, spec, kind, uv, random, gaps, i);
                KwPierRoughPole.Piling(mesh, spec, kind, uv, random, i, -1, cap);
                KwPierRoughPole.Piling(mesh, spec, kind, uv, random, i, 1, cap);
            }
            Curbs(mesh, spec, uv, gaps);
        }

        // The joists along under the boards: their tops at the boards' underside, resting on the cap beams.
        private static void Joists(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(spec.Width / JoistSpacing) + 1);
            float edge = spec.Width * 0.5f - 0.1f;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-edge, edge, i / (count - 1f)) + KwPierRough.Draw(random, -JoistShift, JoistShift);
                float wide = KwPierRough.Draw(random, 0.08f, 0.12f);
                x = Mathf.Clamp(x, -edge, edge);
                for (float z = 0f; z < spec.Length - 0.01f;)
                {
                    float end = Mathf.Min(spec.Length, z + KwPierRough.Draw(random, 2.5f, 4f)), step = KwPierRough.Draw(random, -JoistStep, JoistStep);
                    mesh.Box(new Vector3(x - wide * 0.5f, kind.JoistFoot, z), new Vector3(x + wide * 0.5f, -kind.Thick + step, end), 2, uv, uv.Timber(end - z));
                    z = end;
                }
            }
        }

        // A station's cap beam across under the joists (none at a joined start, the dock it meets has its own); its middle
        // along the section and half its width (x, y), and z 1 when it was made.
        private static Vector3 Cap(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random, KwPierGaps gaps, int station)
        {
            float wide = KwPierRough.Draw(random, 0.14f, 0.18f), shift = KwPierRough.Draw(random, -0.02f, 0.02f), skew = KwPierRough.Draw(random, -CapSkew, CapSkew);
            float overLeft = KwPierRough.Draw(random, CapOverLeast, CapOverMost), overRight = KwPierRough.Draw(random, CapOverLeast, CapOverMost);
            float z = Mathf.Clamp(spec.StationZ(station) + shift, wide * 0.5f, spec.Length - wide * 0.5f), half = spec.Width * 0.5f;
            if (spec.Joined && station == 0)
                return new Vector3(z, wide * 0.5f, 0f);
            float a = -half - (KwPierRough.Flush(gaps, KwPierGaps.Left, z - wide, z + wide) ? 0f : overLeft);
            float b = half + (KwPierRough.Flush(gaps, KwPierGaps.Right, z - wide, z + wide) ? 0f : overRight);
            Vector3[] c = new Vector3[8];
            for (int k = 0; k < 4; k++)
            {
                bool far = k == 2 || k == 3, wideSide = k == 1 || k == 2;
                float x = far ? b : a, at = z + (wideSide ? wide : -wide) * 0.5f + x * skew;
                (c[k], c[k + 4]) = (new Vector3(x, kind.CapFoot, at), new Vector3(x, kind.JoistFoot, at));
            }
            KwPierBlock.Make(mesh, c, uv, uv.Timber(b - a));
            return new Vector3(z, wide * 0.5f, 1f);
        }

        // Each side's curb: pieces laid end to end from a seed of the side's own, each cut to the runs no other dock or
        // ladder opens.
        private static void Curbs(KwPierMesh mesh, KwPierSpec spec, KwPierUv uv, KwPierGaps gaps)
        {
            foreach (int edge in new[] { KwPierGaps.Left, KwPierGaps.Right })
            {
                System.Random random = new System.Random(unchecked(spec.Seed * 31 + edge * 977 + 7));
                List<Vector2> runs = gaps.Runs(edge, spec.Length);
                for (float z = 0f; z < spec.Length - 0.01f;)
                {
                    float length = KwPierRough.Draw(random, CurbLeast, CurbMost), gap = random.NextDouble() < CurbGapShare ? 0f : KwPierRough.Draw(random, 0.02f, 0.12f);
                    Vector4 size = new Vector4(KwPierRough.Draw(random, 0.09f, 0.13f), KwPierRough.Draw(random, 0.08f, 0.12f), KwPierRough.Draw(random, 0f, 0.03f), KwPierRough.Draw(random, -0.008f, 0.008f));
                    float skew = KwPierRough.Draw(random, -0.01f, 0.01f), end = Mathf.Min(spec.Length, z + length);
                    foreach (Vector2 run in runs)
                        Curb(mesh, spec, uv, edge == KwPierGaps.Left ? -1f : 1f, new Vector2(Mathf.Max(z, run.x), Mathf.Min(end, run.y)), size, skew);
                    z = end + gap;
                }
            }
        }

        // One piece of curb from z span.x to span.y on a side (size: width, height, inset from the edge, lift), skewed a little.
        private static void Curb(KwPierMesh mesh, KwPierSpec spec, KwPierUv uv, float side, Vector2 span, Vector4 size, float skew)
        {
            if (span.y - span.x < CurbShortest)
                return;
            float middle = side * (spec.Width * 0.5f - size.x * 0.5f - size.z), zm = (span.x + span.y) * 0.5f;
            Vector3[] c = new Vector3[8];
            for (int k = 0; k < 4; k++)
            {
                bool far = k == 2 || k == 3, wide = k == 1 || k == 2;
                float z = far ? span.y : span.x, x = middle + (wide ? 0.5f : -0.5f) * size.x + (z - zm) * skew;
                (c[k], c[k + 4]) = (new Vector3(x, size.w - 0.01f, z), new Vector3(x, size.w + size.y, z));
            }
            KwPierBlock.Make(mesh, c, uv, uv.Timber(span.y - span.x));
        }
    }
}
