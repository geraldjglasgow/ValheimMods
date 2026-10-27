using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Openings cut out of a wall's convex pieces (a strake's board between two stations, a cabin wall's piece): what is left
    /// of a piece once each opening's outline (convex, anticlockwise, in the wall's (along, up)) is taken away, as convex
    /// pieces again, so they are drawn as every piece is. A piece minus an outline is the piece outside the outline's first
    /// edge, then what is inside that edge and outside the second, and so on: each part convex, together the whole piece
    /// less the outline. Points on the cuts are blended from the piece's own corners, so a board keeps its measures (its
    /// strake, its place along the texture) on the new corners. A piece no opening reaches comes back as it is. Pure.
    /// </summary>
    public static class KwOpeningCut
    {
        private const float Sliver = 1e-10f;

        /// <summary>A piece less every hole, as convex pieces; at gives each corner's (along, up), blend a corner between two.</summary>
        public static List<List<T>> Apart<T>(List<T> piece, Func<T, Vector2> at, Func<T, T, float, T> blend, IReadOnlyList<List<Vector2>>? holes)
        {
            List<List<T>> pieces = new List<List<T>> { piece };
            if (holes == null || holes.Count == 0 || piece.Count < 3)
                return pieces;
            foreach (List<Vector2> hole in holes)
            {
                List<List<T>> left = new List<List<T>>();
                foreach (List<T> one in pieces)
                    left.AddRange(Minus(one, at, blend, hole));
                pieces = left;
            }
            return pieces;
        }

        /// <summary>Whether a hole's bounds and a piece's overlap at all (a quick test before cutting).</summary>
        public static bool Touches<T>(List<T> piece, Func<T, Vector2> at, Rect hole)
        {
            float u0 = float.MaxValue, u1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (T p in piece)
            {
                Vector2 q = at(p);
                (u0, u1, z0, z1) = (Mathf.Min(u0, q.x), Mathf.Max(u1, q.x), Mathf.Min(z0, q.y), Mathf.Max(z1, q.y));
            }
            return u1 > hole.xMin && u0 < hole.xMax && z1 > hole.yMin && z0 < hole.yMax;
        }

        // One piece less one convex hole.
        private static List<List<T>> Minus<T>(List<T> piece, Func<T, Vector2> at, Func<T, T, float, T> blend, List<Vector2> hole)
        {
            List<List<T>> kept = new List<List<T>>();
            if (!Touches(piece, at, KwOpeningShape.Bounds(hole)))
            {
                kept.Add(piece);
                return kept;
            }
            List<T> rest = piece;
            for (int i = 0; i < hole.Count && rest.Count >= 3; i++)
            {
                Vector2 a = hole[i], b = hole[(i + 1) % hole.Count];
                Func<T, float> inside = p => Cross(b - a, at(p) - a);
                List<T> outside = Keep(rest, p => -inside(p), blend);
                if (outside.Count >= 3 && Area(outside, at) > Sliver)
                    kept.Add(outside);
                rest = Keep(rest, inside, blend);
            }
            return kept;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        // The part of a ring where over is not below 0 (one cut, Sutherland-Hodgman).
        private static List<T> Keep<T>(List<T> ring, Func<T, float> over, Func<T, T, float, T> blend)
        {
            List<T> kept = new List<T>();
            for (int i = 0; i < ring.Count; i++)
            {
                T a = ring[i], b = ring[(i + 1) % ring.Count];
                float da = over(a), db = over(b);
                if (da >= 0f)
                    kept.Add(a);
                if ((da > 0f && db < 0f) || (da < 0f && db > 0f))
                    kept.Add(blend(a, b, da / (da - db)));
            }
            return kept;
        }

        // A ring's area, measured from its first corner (far from the origin a small piece's area is lost in rounding).
        private static float Area<T>(List<T> ring, Func<T, Vector2> at)
        {
            float twice = 0f;
            Vector2 o = at(ring[0]);
            for (int i = 1; i + 1 < ring.Count; i++)
                twice += Cross(at(ring[i]) - o, at(ring[i + 1]) - o);
            return Mathf.Abs(twice) / 2f;
        }
    }
}
