using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One cross-section the water mask is lofted through (<see cref="KwMaskSections"/>): where it is drawn along the ship
    /// (<see cref="Z"/>, the hull's first and last kept in from its ends) and where it stands (<see cref="At"/>, which
    /// compartment), the two hull sections it lies between and how far (<see cref="A"/>, <see cref="B"/>,
    /// <see cref="T"/>: the same one twice for a hull section itself), and its outline drawn in (<see cref="Inner"/>).
    /// </summary>
    public readonly struct KwMaskSection
    {
        public KwMaskSection(float z, float at, int a, int b, float t, Vector2[]? inner) => (Z, At, A, B, T, Inner) = (z, at, a, b, t, inner);

        public float Z { get; }
        public float At { get; }
        public int A { get; }
        public int B { get; }
        public float T { get; }
        public Vector2[]? Inner { get; }
    }

    /// <summary>
    /// The cross-sections the water mask is lofted through (<see cref="KwShipWaterMask"/>), plain arithmetic: every hull
    /// section drawn in (<see cref="KwMaskShape.Inner"/>), and between two more than <see cref="Spacing"/> apart (a long
    /// straight run amidships has its sections 4 to 6 m apart) more, each a blend of the two drawn-in outlines, so where
    /// the sea's line crosses the hull's sheer or its rising keel between two of the hull's own sections the mask follows
    /// it there instead of stopping a whole run short and leaving the sea showing in the hold. The sea's line at one is its
    /// two hull sections' lines blended (<see cref="Line"/>).
    /// </summary>
    public static class KwMaskSections
    {
        /// <summary>Metres: the most between two of the mask's sections.</summary>
        public const float Spacing = 0.5f;

        public static List<KwMaskSection> Of(List<(float Z, Vector2[] Shape)> rings)
        {
            List<Vector2[]?> inner = rings.ConvertAll(r => KwMaskShape.Inner(r.Shape));
            List<KwMaskSection> sections = new List<KwMaskSection>();
            if (rings.Count == 0)
                return sections;
            float first = rings[0].Z + KwMaskShape.Inset, last = rings[rings.Count - 1].Z - KwMaskShape.Inset;
            for (int r = 0; r < rings.Count; r++)
            {
                sections.Add(new KwMaskSection(Mathf.Clamp(rings[r].Z, first, last), rings[r].Z, r, r, 0f, inner[r]));
                if (r + 1 < rings.Count)
                    Between(rings, inner, r, first, last, sections);
            }
            return sections;
        }

        /// <summary>The sea's line at a section, n·p = d, from its two hull sections' lines; false where either has none.</summary>
        public static bool Line(KwMaskSection s, Vector2[] normals, float[] levels, bool[] found, out Vector2 n, out float d)
        {
            (n, d) = (Vector2.up, 0f);
            if (s.A >= found.Length || s.B >= found.Length || !found[s.A] || !found[s.B])
                return false;
            n = Vector2.Lerp(normals[s.A], normals[s.B], s.T).normalized;
            d = Mathf.Lerp(levels[s.A], levels[s.B], s.T);
            return true;
        }

        // Sections between hull sections r and r + 1, Spacing apart at most: their drawn-in outlines blended.
        private static void Between(List<(float Z, Vector2[] Shape)> rings, List<Vector2[]?> inner, int r, float first, float last, List<KwMaskSection> into)
        {
            float from = rings[r].Z, to = rings[r + 1].Z;
            int steps = Mathf.CeilToInt((to - from) / Spacing);
            if (steps < 2 || inner[r] == null || inner[r + 1] == null)
                return;
            Vector2[] a = Even(inner[r]!), b = Even(inner[r + 1]!);
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps, z = Mathf.Lerp(from, to, t);
                Vector2[] blend = new Vector2[KwMaskShape.Round];
                for (int k = 0; k < blend.Length; k++)
                    blend[k] = Vector2.Lerp(a[k], b[k], t);
                into.Add(new KwMaskSection(Mathf.Clamp(z, first, last), z, r, r + 1, t, blend));
            }
        }

        // An outline as Round points evenly round it, paired with another made the same way (KwMaskShape.Resample).
        private static Vector2[] Even(Vector2[] outline)
        {
            Vector2[] even = new Vector2[KwMaskShape.Round];
            KwMaskShape.Resample(new List<Vector2>(outline), even);
            return even;
        }
    }
}
