using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The openings on one wall fitted to it with their tunnels (<see cref="KwOpeningTunnel"/>): on a hull's planking or a
    /// cabin's raised side each opening gets its own tunnel and its tunnel's ring (<see cref="KwOpeningRing"/>) is sized: as
    /// wide as its frame's face and a little more, and wide enough that the cut's corners on the inside face, which slide
    /// along it as far as the tunnel bends them, leave at least <see cref="Kept"/> of the ring between them and its outer
    /// edge; kept within the wall's reach along (its ends meet the next part or their caps unbent) and off every other
    /// opening's ring. Where a ring cannot be that wide its tunnel bends only as far as the ring has room for. A cabin's own
    /// walls keep the openings as they are. The same on every peer. Pure.
    /// </summary>
    public static class KwOpeningRings
    {
        /// <summary>The least share of a ring left between its cut and its outer edge on the inside face.</summary>
        private const float Kept = 0.4f;

        /// <summary>How much wider than a frame's face a ring is at least; the narrowest ring kept; how much a ring narrows at a try.</summary>
        private const float Spare = 0.02f, Least = 0.005f, Narrow = 0.8f, End = 0.005f;

        /// <summary>A wall's openings fitted to it (<see cref="KwOpening.On"/>), each with its tunnel on a hull's planking or raised side.</summary>
        public static List<KwOpening> Bind(KwOpeningSurface surface, List<KwOpening> openings)
        {
            if (!(surface is KwPlankSurface face) || openings.Count == 0)
                return openings.ConvertAll(o => o.On(surface));
            List<KwOpening> bound = openings.ConvertAll(o => o.On(new KwOpeningTunnel(face, o)));
            List<float> slides = bound.ConvertAll(Slide), rings = new List<float>();
            for (int i = 0; i < bound.Count; i++)
                rings.Add(Mathf.Max(KwOpeningFrames.Width(bound[i].Frame).Wide + Spare, slides[i] / (1f - Kept)));
            Fit(face, bound, rings);
            for (int i = 0; i < bound.Count; i++)
            {
                float ring = rings[i] >= Least ? rings[i] : 0f;
                Tunnel(bound[i]).Size(ring, slides[i] < 1e-4f ? 1f : ring * (1f - Kept) / slides[i]);
            }
            return bound;
        }

        private static KwOpeningTunnel Tunnel(KwOpening o) => (KwOpeningTunnel)o.Surface!;

        // How far the tunnel moves its cut's corners on the inside face along the face, at most.
        private static float Slide(KwOpening o)
        {
            KwOpeningTunnel t = Tunnel(o);
            float most = 0f;
            foreach (Vector2 c in o.Outline())
            {
                Vector3 fibre = t.Face.Fibre(c.x, c.y);
                most = Mathf.Max(most, (t.Bend(fibre, t.Face.Out(c.x, c.y), 1f) - fibre).magnitude);
            }
            return most * t.Face.Thick;
        }

        // Each ring narrowed until it lies within the wall's reach along and off every other opening's ring.
        private static void Fit(KwPlankSurface face, List<KwOpening> bound, List<float> rings)
        {
            (float from, float to) = face.Reach;
            for (int i = 0; i < bound.Count; i++)
            {
                while (rings[i] >= Least && !Within(bound[i].Outline(rings[i]), from + End, to - End))
                    rings[i] *= Narrow;
            }
            for (int i = 0; i < bound.Count; i++)
            {
                for (int j = i + 1; j < bound.Count; j++)
                {
                    while (Mathf.Max(rings[i], rings[j]) >= Least && Overlap(bound[i].Outline(rings[i]), bound[j].Outline(rings[j])))
                        (rings[i], rings[j]) = (rings[i] * Narrow, rings[j] * Narrow);
                }
            }
        }

        private static bool Within(List<Vector2> ring, float from, float to) => ring.TrueForAll(p => p.x > from && p.x < to);

        // Whether two convex outlines (anticlockwise) overlap: neither has an edge with the other wholly outside it.
        private static bool Overlap(List<Vector2> a, List<Vector2> b) => !Apart(a, b) && !Apart(b, a);

        private static bool Apart(List<Vector2> a, List<Vector2> b)
        {
            for (int i = 0; i < a.Count; i++)
            {
                Vector2 p = a[i], e = a[(i + 1) % a.Count] - p;
                if (b.TrueForAll(q => e.x * (q.y - p.y) - e.y * (q.x - p.x) <= 0f))
                    return true;
            }
            return false;
        }
    }
}
