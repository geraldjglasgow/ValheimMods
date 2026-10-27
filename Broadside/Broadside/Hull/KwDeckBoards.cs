using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A deck's top and underside laid as boards (<see cref="KwDeckLayout"/>): its tier's bands across the deck, each cut
    /// along into lengths of two to four metres (a mat each on the plains) that each take their own place in the texture,
    /// so the deck reads as many boards with staggered butt joints, not one texture repeating every four metres. The cuts
    /// and places come from a hash of the band, the length's number, the deck's own seed saved in its text (so two decks of
    /// the same shape in a row lay different boards) and its shape (<see cref="Seed"/>), so every peer, every rebuild and
    /// every ship built from a blueprint, all reading the same saved text, lay the same boards. The top is drawn to be
    /// seen from above only and the underside from below only (one face each: the other side is inside the deck). They
    /// are laid over the deck's pieces (<see cref="KwDeckHoles.Pieces"/>: split at its stations and round any hatch
    /// openings), so the openings stay open; the deck's edges and its collider stay whole (<see cref="KwMesh"/>). Kit axes.
    /// </summary>
    public sealed class KwDeckBoards
    {
        private const float Tiny = 1e-5f;

        private readonly MeshBuilder builder;
        private readonly KwDeckLayout layout;
        private readonly Func<float, float> z;
        private readonly float x0, x1;
        private readonly int seed;
        private readonly Dictionary<int, List<Run>> runs = new Dictionary<int, List<Run>>();

        private KwDeckBoards(MeshBuilder builder, KwDeckLayout layout, Func<float, float> z, List<Vector2> edge, int seed)
        {
            (this.builder, this.layout, this.z, this.seed) = (builder, layout, z, seed);
            (x0, x1) = (edge[0].x, edge[edge.Count - 1].x);
        }

        // One length of a band: from and to along the deck (kit x), its shift in the texture (texels), run backwards or not.
        private readonly struct Run
        {
            public Run(float from, float to, int shift, bool flip) => (From, To, Shift, Flip) = (from, to, shift, flip);

            public float From { get; }
            public float To { get; }
            public int Shift { get; }
            public bool Flip { get; }
        }

        /// <summary>The deck's top and underside as boards. edge: its half width along it; holes: the hatch openings left out.</summary>
        public static void Lay(MeshBuilder builder, List<Vector2> edge, Func<float, float> z, List<Rect> holes, KwDeckLayout layout, int seed) =>
            Lay(builder, KwDeckHoles.Pieces(edge, holes), edge[0].x, edge[edge.Count - 1].x, z, layout, seed);

        /// <summary>
        /// The same over pieces given whole (an outline unlike on its two sides: a cabin's roof drawn in on one side), each
        /// from a to c along with its sides at both ends (x the low y, y the high), the boards' lengths counted from x0 to x1.
        /// </summary>
        public static void Lay(MeshBuilder builder, List<(float A, float C, Vector2 SideA, Vector2 SideC)> pieces, float x0, float x1, Func<float, float> z, KwDeckLayout layout, int seed)
        {
            KwDeckBoards boards = new KwDeckBoards(builder, layout, z, new List<Vector2> { new Vector2(x0, 0f), new Vector2(x1, 0f) }, seed);
            foreach ((float a, float c, Vector2 sideA, Vector2 sideC) in pieces)
            {
                List<Vector2> piece = new List<Vector2> { new Vector2(a, sideA.x), new Vector2(c, sideC.x), new Vector2(c, sideC.y), new Vector2(a, sideA.y) };
                foreach (int g in boards.Bands(Mathf.Min(sideA.x, sideC.x), Mathf.Max(sideA.y, sideC.y)))
                    boards.Band(piece, g);
            }
        }

        /// <summary>
        /// A deck's seed for its boards: its saved seed (<see cref="KwDef.Seed"/>) mixed with its length, its beam at its start
        /// and its heights, to the centimetre (not where it stands).
        /// </summary>
        public static int Seed(KwDef def)
        {
            List<Station> st = def.Stations;
            Vector2[] start = st[0].Half;
            float end = float.IsNaN(def.DeckZEnd) ? def.DeckZ : def.DeckZEnd;
            int heights = (int)Hash(Cm(def.DeckZ), Cm(end), def.Seed);
            return (int)Hash(Cm(st[st.Count - 1].X - st[0].X), Cm(start[start.Length - 1].x), heights);
        }

        private static int Cm(float metres) => Mathf.RoundToInt(metres * 100f);

        // The bands (numbered across the deck, band 0 starting at the centre line) that reach between lo and hi across it.
        private IEnumerable<int> Bands(float lo, float hi)
        {
            int first = Mathf.FloorToInt(lo * KwUv.Density / KwUv.Size), last = Mathf.FloorToInt(hi * KwUv.Density / KwUv.Size);
            for (int tile = first; tile <= last; tile++)
            {
                for (int i = 0; i < layout.Bands; i++)
                {
                    (float from, float to) = Across(tile * layout.Bands + i);
                    if (to > lo + Tiny && from < hi - Tiny)
                        yield return tile * layout.Bands + i;
                }
            }
        }

        // Band g's sides across the deck (kit y): the texture's tile it falls in, and its columns there.
        private (float From, float To) Across(int g)
        {
            int tile = Mathf.FloorToInt((float)g / layout.Bands);
            (int from, int to) = layout.Band(g - tile * layout.Bands);
            return ((tile * KwUv.Size + from) / KwUv.Density, (tile * KwUv.Size + to) / KwUv.Density);
        }

        // A piece of the deck's band g, cut at its lengths' ends, each length a board on its own.
        private void Band(List<Vector2> piece, int g)
        {
            (float from, float to) = Across(g);
            List<Vector2> strip = Clip(Clip(piece, 1, from, 1f), 1, to, -1f);
            if (strip.Count < 3)
                return;
            float a = piece[0].x, c = piece[1].x;
            foreach (Run run in Runs(g))
            {
                if (run.To > a + Tiny && run.From < c - Tiny)
                    Board(Clip(Clip(strip, 0, run.From, 1f), 0, run.To, -1f), run);
            }
        }

        // Band g's lengths along the whole deck, from a start a whole number of steps before the deck's, made once a deck.
        private List<Run> Runs(int g)
        {
            if (runs.TryGetValue(g, out List<Run> list))
                return list;
            runs[g] = list = new List<Run>();
            int band = g - Mathf.FloorToInt((float)g / layout.Bands) * layout.Bands, end = Mathf.CeilToInt((x1 - x0) * KwUv.Density);
            int at = -(int)(Hash(seed, g, -1) % (uint)(layout.Longest / layout.Step)) * layout.Step;
            for (int j = 0; at < end; j++)
            {
                (int length, int shift, bool flip) = layout.Pick(Hash(seed, g, j), band, at);
                list.Add(new Run(x0 + at / KwUv.Density, x0 + (at + length) / KwUv.Density, shift, flip));
                at += length;
            }
            return list;
        }

        // One board on its own place in the texture: its top seen from above (its corners anticlockwise seen from there),
        // its underside a plank's thickness below seen from below (the same corners the other way round).
        private void Board(List<Vector2> outline, Run run)
        {
            List<Vector2> corners = Tidy(outline);
            float area = Area(corners);
            if (corners.Count < 3 || Mathf.Abs(area) < 1e-6f)
                return;
            if (area < 0f)
                corners.Reverse();
            List<Vector3> top = corners.ConvertAll(p => new Vector3(p.x, p.y, z(p.x)));
            List<Vector2> uv = corners.ConvertAll(p => KwUv.Board(p, x0, run.Shift, run.Flip));
            builder.Face(top, uv);
            List<Vector3> under = top.ConvertAll(p => p - new Vector3(0f, 0f, KwPlanks.Thick));
            under.Reverse();
            uv.Reverse();
            builder.Face(under, uv);
        }

        // The part of a convex outline on one side of a line square to an axis (0 along, 1 across): at or past the value
        // going up for side 1, at or before it for side -1. The points it adds lie on the line exactly.
        private static List<Vector2> Clip(List<Vector2> outline, int axis, float value, float side)
        {
            List<Vector2> kept = new List<Vector2>();
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 p = outline[i], q = outline[(i + 1) % outline.Count];
                float dp = side * (p[axis] - value), dq = side * (q[axis] - value);
                if (dp >= 0f)
                    kept.Add(p);
                if ((dp >= 0f) != (dq >= 0f))
                {
                    Vector2 cut = Vector2.Lerp(p, q, dp / (dp - dq));
                    cut[axis] = value;
                    kept.Add(cut);
                }
            }
            return kept;
        }

        // The outline without points that repeat the one before them (where a cut ran through a corner).
        private static List<Vector2> Tidy(List<Vector2> outline)
        {
            List<Vector2> corners = new List<Vector2>();
            foreach (Vector2 p in outline)
            {
                if (corners.Count == 0 || (p - corners[corners.Count - 1]).sqrMagnitude > Tiny * Tiny)
                    corners.Add(p);
            }
            while (corners.Count > 1 && (corners[0] - corners[corners.Count - 1]).sqrMagnitude <= Tiny * Tiny)
                corners.RemoveAt(corners.Count - 1);
            return corners;
        }

        // The outline's area, positive when its corners run anticlockwise seen from above (kit x along, y across).
        private static float Area(List<Vector2> corners)
        {
            float twice = 0f;
            for (int i = 0; i < corners.Count; i++)
            {
                Vector2 p = corners[i], q = corners[(i + 1) % corners.Count];
                twice += p.x * q.y - q.x * p.y;
            }
            return twice / 2f;
        }

        // A well-mixed hash of three numbers, the same on every machine.
        private static uint Hash(int a, int b, int c)
        {
            unchecked
            {
                uint h = (uint)a * 0x9E3779B1u ^ (uint)b * 0x85EBCA77u ^ (uint)c * 0xC2B2AE3Du;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return h;
            }
        }
    }
}
