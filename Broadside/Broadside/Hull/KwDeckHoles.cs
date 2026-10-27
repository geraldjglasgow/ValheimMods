using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Hatch openings cut through a deck. A deck with hatches on it is built in strips across it, split along its length
    /// at its stations and at every opening's ends; each strip leaves out the openings that cross it, so the deck keeps its
    /// boards and its thickness all round each opening, the same boards as without one. The openings come from the hatches
    /// loaded on this peer (<see cref="KwHatchCover.All"/>), so every peer cuts the same holes from the same pieces;
    /// nothing is stored or sent. A hatch is on a deck when it stands within <see cref="Level"/> of the deck's top.
    /// </summary>
    public static class KwDeckHoles
    {
        private const float Level = 0.3f, Tiny = 1e-4f;

        /// <summary>The openings of the hatches loaded on this peer standing on a deck, in its kit coordinates (x along it, y across).</summary>
        public static List<Rect> For(Transform deck, KwDef def) =>
            Of(deck.InverseTransformPoint, def, KwHatchCover.All.Select(h => (h.transform.position, h.Corners())));

        /// <summary>
        /// The openings of hatches (each its middle and its opening's corners, in some frame) standing on a deck, in the
        /// deck's kit coordinates; toDeck takes a point of that frame into the deck's own (Unity) axes.
        /// </summary>
        public static List<Rect> Of(Func<Vector3, Vector3> toDeck, KwDef def, IEnumerable<(Vector3 Middle, Vector3[] Corners)> hatches)
        {
            List<Rect> holes = new List<Rect>();
            Func<float, float> z = KwMesh.DeckHeight(def);
            float x0 = def.Stations[0].X, x1 = def.Stations[def.Stations.Count - 1].X;
            foreach ((Vector3 middle, Vector3[] corners) in hatches)
            {
                Vector3 at = KwMesh.ToKit(toDeck(middle));
                if (Mathf.Abs(at.z - z(Mathf.Clamp(at.x, x0, x1))) > Level)
                    continue;
                Rect box = Box(toDeck, corners);
                if (box.xMax > x0 && box.xMin < x1)
                    holes.Add(box);
            }
            return holes;
        }

        // The opening's corners in the deck's kit coordinates, and the box round them (exact for a hatch square to the deck).
        private static Rect Box(Func<Vector3, Vector3> toDeck, Vector3[] corners)
        {
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (Vector3 corner in corners)
            {
                Vector3 kit = KwMesh.ToKit(toDeck(corner));
                min = Vector2.Min(min, kit);
                max = Vector2.Max(max, kit);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// The deck's top and slab in strips, the openings left out. edge: the deck's half width along it. With its top and
        /// underside laid as boards apart (<see cref="KwDeckBoards"/>), only the strips' edges.
        /// </summary>
        public static void Slab(MeshBuilder b, List<Vector2> edge, Func<float, float> z, List<Rect> holes, Func<Vector3, Vector2> uv, bool boards = false)
        {
            foreach ((float a, float c, Vector2 sideA, Vector2 sideC) in Pieces(edge, holes))
            {
                List<Vector3> top = new List<Vector3>
                {
                    new Vector3(a, sideA.x, z(a)), new Vector3(c, sideC.x, z(c)), new Vector3(c, sideC.y, z(c)), new Vector3(a, sideA.y, z(a)),
                };
                if (boards)
                {
                    KwPlanks.DeckEdges(b, top, uv);
                    continue;
                }
                b.Polygon(top, top.ConvertAll(p => uv(p)));
                KwPlanks.DeckSlab(b, top, uv);
            }
        }

        /// <summary>
        /// The deck's top split round the openings into four-sided pieces, each from a to c along it with its sides at both
        /// ends (x the port side's y, y the starboard side's), so its boards and its colliders leave the same holes.
        /// </summary>
        public static List<(float A, float C, Vector2 SideA, Vector2 SideC)> Pieces(List<Vector2> edge, List<Rect> holes)
        {
            List<(float, float, Vector2, Vector2)> pieces = new List<(float, float, Vector2, Vector2)>();
            List<float> cuts = Cuts(edge, holes);
            for (int i = 0; i + 1 < cuts.Count; i++)
                Strip(pieces, edge, holes, cuts[i], cuts[i + 1]);
            return pieces;
        }

        private static List<float> Cuts(List<Vector2> edge, List<Rect> holes)
        {
            float a = edge[0].x, c = edge[edge.Count - 1].x;
            List<float> cuts = edge.Select(p => p.x).ToList();
            foreach (Rect hole in holes)
            {
                cuts.Add(Mathf.Clamp(hole.xMin, a, c));
                cuts.Add(Mathf.Clamp(hole.xMax, a, c));
            }
            cuts.Sort();
            List<float> kept = new List<float>();
            foreach (float x in cuts)
            {
                if (kept.Count == 0 || x - kept[kept.Count - 1] > Tiny)
                    kept.Add(x);
            }
            return kept;
        }

        // One strip from a to c: the pieces between the deck's sides and the openings crossing it, port to starboard.
        private static void Strip(List<(float, float, Vector2, Vector2)> pieces, List<Vector2> edge, List<Rect> holes, float a, float c)
        {
            float mid = (a + c) / 2f, wa = Width(edge, a), wc = Width(edge, c);
            List<Rect> crossing = holes.Where(h => h.xMin < mid && h.xMax > mid).OrderBy(h => h.yMin).ToList();
            float? from = null;
            foreach (Rect hole in crossing)
            {
                Piece(pieces, a, c, wa, wc, from, hole.yMin);
                from = from == null ? hole.yMax : Mathf.Max(from.Value, hole.yMax);
            }
            Piece(pieces, a, c, wa, wc, from, null);
        }

        // A piece of a strip between two lines across it: null is the deck's own side, else a fixed y kept on the deck.
        private static void Piece(List<(float, float, Vector2, Vector2)> pieces, float a, float c, float wa, float wc, float? lo, float? hi)
        {
            float loA = Line(lo, -wa, wa), loC = Line(lo, -wc, wc);
            float hiA = Mathf.Max(loA, Line(hi, wa, wa)), hiC = Mathf.Max(loC, Line(hi, wc, wc));
            if (hiA - loA >= Tiny || hiC - loC >= Tiny)
                pieces.Add((a, c, new Vector2(loA, hiA), new Vector2(loC, hiC)));
        }

        private static float Line(float? y, float side, float w) => y == null ? side : Mathf.Clamp(y.Value, -w, w);

        // The deck's half width at x along it, between the edge's points.
        private static float Width(List<Vector2> edge, float x)
        {
            for (int i = 0; i + 1 < edge.Count; i++)
            {
                if (x > edge[i + 1].x + 1e-5f)
                    continue;
                float span = edge[i + 1].x - edge[i].x;
                return span < 1e-5f ? edge[i + 1].y : Mathf.Lerp(edge[i].y, edge[i + 1].y, (x - edge[i].x) / span);
            }
            return edge[edge.Count - 1].y;
        }
    }
}
