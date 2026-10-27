using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Planking with substance: every strake a board <see cref="Thick"/> thick, its lower edge standing <see cref="Lap"/>
    /// proud of the strake below (clinker, as the game's longship is built), so each plank line throws a small shadow. The
    /// strakes are cut level along the hull's sides and one width round its bottom (<see cref="KwGirth"/>) and drawn by
    /// <see cref="KwStrakes"/> (outside and inside faces, the lip under each lower edge, the gunwale's cap, a part's ends
    /// closed through the thickness); here the section helpers they share, a flat stern's transom board and a deck's slab.
    /// Kit axes, one side.
    /// </summary>
    public static class KwPlanks
    {
        public const float Thick = 0.08f, Lap = 0.025f;

        /// <summary>A section's inside face: each point moved in by the planking's thickness, square to the section.</summary>
        public static Vector2[] Inside(Vector2[] half) => Offset(half, -Thick);

        /// <summary>A section with each point moved out (or in, for a negative distance), square to the section.</summary>
        public static Vector2[] Offset(Vector2[] half, float distance)
        {
            Vector2[] moved = new Vector2[half.Length];
            for (int i = 0; i < half.Length; i++)
            {
                Vector3 n = Normal(new Station(0f, half), i, 1f);
                moved[i] = half[i] + new Vector2(n.y, n.z) * distance;
            }
            return moved;
        }

        /// <summary>
        /// A flat stern's transom board at station s, the hull's last: the planking's thickness deep, inboard of the end and
        /// standing a little proud of it, its edge a little outside the planks all round so it covers their ends and their
        /// laps, planked across in the hull's outside wood in level rows a strake wide, at the heights of the hull's own side
        /// strakes (counted from the keel line at height level, <see cref="KwGirth"/>), so the rows meet the strakes at its
        /// corners however tall the stern. outward: 1 when the hull ends there (its last station), -1 when it starts there
        /// (its first: a part laid from a flat stern to another keel end).
        /// </summary>
        public static void TransomBoard(MeshBuilder b, Station s, float outward = 1f, float level = 0f)
        {
            List<Vector2> outline = TransomOutline(s);
            float floor = float.MaxValue, top = float.MinValue, w = KwUv.StrakeWidth;
            foreach (Vector2 p in outline)
                (floor, top) = (Mathf.Min(floor, p.y), Mathf.Max(top, p.y));
            float aft = s.X + 0.02f * outward, fore = s.X - Thick * outward;
            for (int r = Mathf.FloorToInt((floor - level) / w); level + r * w < top; r++)
            {
                List<Vector2> row = KwClip.Between(outline, p => p.y - level, Vector2.LerpUnclamped, r * w, (r + 1) * w);
                b.Row = new Vector2(r, 0f);
                TransomRow(b, row, aft, r, level + r * w);
                TransomRow(b, row, fore, r, level + r * w);
            }
            TransomRim(b, outline, aft, fore);
        }

        // A flat stern's board outline (half width, height) a little outside the planks: up the port side, down the starboard.
        private static List<Vector2> TransomOutline(Station s)
        {
            Vector2[] cover = Offset(s.Half, Lap + 0.015f);
            List<Vector2> outline = new List<Vector2>(cover);
            for (int i = cover.Length - 1; i >= 0; i--)
                outline.Add(new Vector2(-cover[i].x, cover[i].y));
            return outline;
        }

        // One row of the board's face at x, from its foot (a height) up a strake's width, planked across like strake r.
        private static void TransomRow(MeshBuilder b, List<Vector2> row, float x, int r, float foot)
        {
            if (row.Count >= 3)
                b.Polygon(row.ConvertAll(p => new Vector3(x, p.x, p.y)), row.ConvertAll(p => KwUv.Transom(p.x, r, p.y - foot)));
        }

        // The board's edge all round through its thickness, its texture running along the edge as a strake's does.
        private static void TransomRim(MeshBuilder b, List<Vector2> outline, float aft, float fore)
        {
            float along = 0f, deep = Mathf.Abs(aft - fore);
            b.Row = Vector2.zero;
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 p = outline[i], q = outline[(i + 1) % outline.Count];
                float next = along + Vector2.Distance(p, q);
                b.Polygon(new List<Vector3> { new Vector3(aft, p.x, p.y), new Vector3(aft, q.x, q.y), new Vector3(fore, q.x, q.y), new Vector3(fore, p.x, p.y) },
                    new List<Vector2> { KwUv.Strake(true, along, 0, 0f), KwUv.Strake(true, next, 0, 0f), KwUv.Strake(true, next, 0, deep), KwUv.Strake(true, along, 0, deep) });
                along = next;
            }
        }

        /// <summary>Point i of station s's section on one side (1 port, -1 starboard), kit axes.</summary>
        internal static Vector3 Base(Station s, int i, float side) => new Vector3(s.X, side * s.Half[i].x, s.Half[i].y);

        /// <summary>Outward from the hull, square to the section at point i (from its neighbours on the section), on one side.</summary>
        internal static Vector3 Normal(Station s, int i, float side)
        {
            Vector2[] half = s.Half;
            Vector2 d = half[Mathf.Min(i + 1, half.Length - 1)] - half[Mathf.Max(i - 1, 0)];
            if (d.sqrMagnitude < 1e-8f)
                return new Vector3(0f, side, 0f);
            Vector2 n = new Vector2(d.y, -d.x).normalized;
            return new Vector3(0f, side * n.x, n.y);
        }

        /// <summary>A deck's underside and its edges, <see cref="Thick"/> below its top, on the deck boards.</summary>
        public static void DeckSlab(MeshBuilder b, List<Vector3> top, System.Func<Vector3, Vector2> uv)
        {
            List<Vector3> under = top.ConvertAll(p => p - new Vector3(0f, 0f, Thick));
            b.Polygon(under, under.ConvertAll(p => uv(p)));
            DeckEdges(b, top, uv);
        }

        /// <summary>A deck's edges only, down through its thickness from its top's outline (its top and underside laid as boards apart).</summary>
        public static void DeckEdges(MeshBuilder b, List<Vector3> top, System.Func<Vector3, Vector2> uv)
        {
            List<Vector3> under = top.ConvertAll(p => p - new Vector3(0f, 0f, Thick));
            for (int i = 0; i < top.Count; i++)
            {
                int j = (i + 1) % top.Count;
                List<Vector3> edge = new List<Vector3> { top[i], top[j], under[j], under[i] };
                b.Polygon(edge, edge.ConvertAll(p => uv(p)));
            }
        }
    }
}
