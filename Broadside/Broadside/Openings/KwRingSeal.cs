using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The pieces a cut leaves of one strake piece made to meet corner to corner (<see cref="KwOpeningRing"/>): a corner of
    /// one piece lying on another's edge (where one cut line ends on a line drawn further) is put into that edge too, so
    /// pieces side by side share every corner along their common edge, and their faces, drawn corner to corner, leave no
    /// slit between them however the board's faces bend. Pure.
    /// </summary>
    public static class KwRingSeal
    {
        /// <summary>How near a corner lies to an edge to be on it, and to another corner to be the same.</summary>
        private const float Near = 1e-5f;

        /// <summary>The pieces, each with every other piece's corners that lie on its edges put in.</summary>
        public static List<List<KwRingPoint>> Seal(List<List<KwRingPoint>> parts)
        {
            List<KwRingPoint> all = new List<KwRingPoint>();
            foreach (List<KwRingPoint> part in parts)
                all.AddRange(part);
            all.Sort((p, q) => p.At.x.CompareTo(q.At.x));
            return parts.ConvertAll(part => Sealed(part, all));
        }

        private static List<KwRingPoint> Sealed(List<KwRingPoint> part, List<KwRingPoint> all)
        {
            List<KwRingPoint> ring = new List<KwRingPoint>();
            for (int i = 0; i < part.Count; i++)
            {
                ring.Add(part[i]);
                On(part[i].At, part[(i + 1) % part.Count].At, all, ring);
            }
            return ring;
        }

        // The corners strictly inside the edge from a to b, in order from a, each place once, added to a ring (all sorted
        // along, so only those within the edge's reach along are looked at).
        private static void On(Vector2 a, Vector2 b, List<KwRingPoint> all, List<KwRingPoint> ring)
        {
            float length = Vector2.Distance(a, b);
            if (length < 2f * Near)
                return;
            Vector2 way = (b - a) / length;
            List<(float T, KwRingPoint P)>? found = null;
            for (int k = First(all, Mathf.Min(a.x, b.x) - Near); k < all.Count && all[k].At.x <= Mathf.Max(a.x, b.x) + Near; k++)
            {
                Vector2 off = all[k].At - a;
                float t = Vector2.Dot(off, way);
                if (t > Near && t < length - Near && Mathf.Abs(way.x * off.y - way.y * off.x) < Near && !(found?.Exists(f => Mathf.Abs(f.T - t) < Near) ?? false))
                    (found ??= new List<(float T, KwRingPoint P)>()).Add((t, all[k]));
            }
            if (found == null)
                return;
            found.Sort((x, y) => x.T.CompareTo(y.T));
            ring.AddRange(found.ConvertAll(f => f.P));
        }

        // The first of the sorted points at or past an x.
        private static int First(List<KwRingPoint> all, float x)
        {
            int lo = 0, hi = all.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (all[mid].At.x < x)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }
    }
}
