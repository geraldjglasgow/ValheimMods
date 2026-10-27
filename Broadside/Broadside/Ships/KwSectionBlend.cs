using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cross-section halfway between two of a hull's sections, as the planking runs from one to the other: each
    /// section's starboard half (the first half of its outline, keel to sheer) measured round its edge, both halves read
    /// at every share of the way round where either has a corner, the two points at each share averaged, and the port
    /// side made as its mirror. So a slice of the hull (<see cref="KwHullSlices.Of"/>) has the shape at its middle: it
    /// holds the water the hull holds there, the same whichever way round the ship was saved, and it is level across.
    /// </summary>
    public static class KwSectionBlend
    {
        private const float Same = 1e-4f;

        /// <summary>The outline halfway between two outlines made as <see cref="KwHullSlices.Rings"/> makes them.</summary>
        public static Vector2[] Middle(Vector2[] a, Vector2[] b)
        {
            if (a.Length < 4 || b.Length < 4 || a.Length % 2 != 0 || b.Length % 2 != 0)
                return a.Length >= b.Length ? a : b;
            Vector2[] ha = Half(a), hb = Half(b);
            float[] sa = Shares(ha), sb = Shares(hb);
            List<float> shares = Merged(sa, sb);
            Vector2[] shape = new Vector2[shares.Count * 2];
            for (int i = 0; i < shares.Count; i++)
            {
                Vector2 p = (At(ha, sa, shares[i]) + At(hb, sb, shares[i])) / 2f;
                shape[i] = p;
                shape[shape.Length - 1 - i] = new Vector2(-p.x, p.y);
            }
            return shape;
        }

        // The outline's starboard half, keel to sheer.
        private static Vector2[] Half(Vector2[] shape)
        {
            Vector2[] half = new Vector2[shape.Length / 2];
            System.Array.Copy(shape, half, half.Length);
            return half;
        }

        // How far round the half each point lies, as a share of its whole length (by count where it has no length).
        private static float[] Shares(Vector2[] half)
        {
            float[] at = new float[half.Length];
            for (int i = 1; i < half.Length; i++)
                at[i] = at[i - 1] + Vector2.Distance(half[i - 1], half[i]);
            float whole = at[half.Length - 1];
            for (int i = 0; i < half.Length; i++)
                at[i] = whole > 1e-5f ? at[i] / whole : i / (float)(half.Length - 1);
            return at;
        }

        // Every share where either half has a corner, in order, those within a hair of each other once.
        private static List<float> Merged(float[] a, float[] b)
        {
            List<float> all = new List<float>(a);
            all.AddRange(b);
            all.Sort();
            List<float> kept = new List<float>();
            foreach (float s in all)
            {
                if (kept.Count == 0 || s - kept[kept.Count - 1] > Same)
                    kept.Add(s);
            }
            kept[kept.Count - 1] = all[all.Count - 1]; // the sheer itself, not a hair short of it
            return kept;
        }

        // The point a share of the way round a half.
        private static Vector2 At(Vector2[] half, float[] shares, float share)
        {
            int i = 0;
            while (i < half.Length - 2 && shares[i + 1] < share)
                i++;
            float span = shares[i + 1] - shares[i];
            return Vector2.Lerp(half[i], half[i + 1], span > 1e-6f ? (share - shares[i]) / span : 0f);
        }
    }
}
