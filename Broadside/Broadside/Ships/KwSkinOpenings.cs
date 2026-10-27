using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hull box on the planking's face (<see cref="KwHullSkin"/>) cut round the openings in its wall, so the crew can see,
    /// walk and shoot through an opening where its look shows one. An opening's bore (its outline swept through the wall
    /// along its tunnel, from the laps' face to the inside, <see cref="KwOpeningTunnel"/>; worked out once a part,
    /// <see cref="Of"/>) is seen square to the box's face, where the box lies on the wall; a bore that does not come within
    /// <see cref="Near"/> of the box's middle plane leaves it alone. What is left is cut as an opening's slabs always were
    /// (<see cref="KwOpeningBoxes.Apart"/>), and a piece narrower than <see cref="KwSkinFit.Least"/> at the box's edge is
    /// widened out over the edge, away from the opening, so its thin way stays its thickness (<see cref="KwHoleSlab"/>). Pure.
    /// </summary>
    public static class KwSkinOpenings
    {
        private const float Near = 0.3f;

        /// <summary>The bores of a part's openings.</summary>
        public static List<KwSkinBore> Of(List<KwOpening> openings)
        {
            List<KwSkinBore> bores = new List<KwSkinBore>();
            foreach (KwOpening o in openings)
            {
                KwOpeningSurface? wall = o.Surface;
                if (wall == null)
                    continue;
                List<Vector3> points = new List<Vector3>();
                foreach (Vector2 c in o.Outline())
                {
                    foreach (float depth in new[] { wall.Proud, 0f, -wall.Thick })
                        points.Add(wall.Point(c.x, c.y, depth));
                }
                Vector3 middle = Vector3.zero;
                points.ForEach(p => middle += p / points.Count);
                float reach = 0f;
                points.ForEach(p => reach = Mathf.Max(reach, (p - middle).magnitude));
                bores.Add(new KwSkinBore(o.Wall, points.ToArray(), middle, reach));
            }
            return bores;
        }

        /// <summary>A box less the bores of one wall that pass through it: itself when none does.</summary>
        public static List<KwSkinBox> Cut(KwSkinBox box, List<KwSkinBore> bores, string wall)
        {
            List<Rect> holes = new List<Rect>();
            Rect face = box.Face;
            float round = box.Size.magnitude / 2f;
            foreach (KwSkinBore bore in bores)
            {
                if (bore.Wall == wall && (bore.Middle - box.Centre).sqrMagnitude < (bore.Reach + round) * (bore.Reach + round)
                    && Seen(box, bore.Points, out Rect seen) && seen.Overlaps(face))
                    holes.Add(seen);
            }
            if (holes.Count == 0)
                return new List<KwSkinBox> { box };
            return KwOpeningBoxes.Apart(face, holes).ConvertAll(r => box.Part(Widened(r, face)));
        }

        // A bore seen square to a box's face (x along, y up from its middle); false where it passes far from the box.
        private static bool Seen(KwSkinBox box, Vector3[] points, out Rect bore)
        {
            Vector2 lo = Vector2.positiveInfinity, hi = Vector2.negativeInfinity;
            float deep = float.MaxValue, shallow = float.MinValue;
            foreach (Vector3 point in points)
            {
                Vector3 p = point - box.Centre;
                Vector2 seen = new Vector2(Vector3.Dot(p, box.Along), Vector3.Dot(p, box.Up));
                (lo, hi) = (Vector2.Min(lo, seen), Vector2.Max(hi, seen));
                float n = Vector3.Dot(p, box.Out);
                (deep, shallow) = (Mathf.Min(deep, n), Mathf.Max(shallow, n));
            }
            bore = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
            return deep < Near && shallow > -Near;
        }

        // A piece narrower than Least along or up, at the face's edge on that side, widened out over the edge.
        private static Rect Widened(Rect r, Rect face)
        {
            const float edge = 1e-4f;
            float least = KwSkinFit.Least;
            if (r.width < least && r.xMin <= face.xMin + edge)
                r.xMin = r.xMax - least;
            else if (r.width < least && r.xMax >= face.xMax - edge)
                r.xMax = r.xMin + least;
            if (r.height < least && r.yMin <= face.yMin + edge)
                r.yMin = r.yMax - least;
            else if (r.height < least && r.yMax >= face.yMax - edge)
                r.yMax = r.yMin + least;
            return r;
        }
    }

    /// <summary>An opening's bore through its wall (<see cref="KwSkinOpenings"/>): the wall, the swept points (kit), their middle and reach round it.</summary>
    public readonly struct KwSkinBore
    {
        public KwSkinBore(string wall, Vector3[] points, Vector3 middle, float reach) => (Wall, Points, Middle, Reach) = (wall, points, middle, reach);

        public string Wall { get; }
        public Vector3[] Points { get; }
        public Vector3 Middle { get; }
        public float Reach { get; }
    }
}
