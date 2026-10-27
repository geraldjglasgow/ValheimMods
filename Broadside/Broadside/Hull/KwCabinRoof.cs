using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin's roof: a raised deck over the cabin's own footprint at its top (<see cref="KwCabinOutline.Roof"/>: out to
    /// the hull's inside on a side at the hull, to the drawn-in wall's outer face on the other), laid as its tier's deck
    /// boards as any deck is (<see cref="KwDeckBoards"/>), its edges down through its thickness, and walked on as one whole
    /// slab. Nothing stands above it: a railing on it is the railing tool's. Kit axes.
    /// </summary>
    public static class KwCabinRoof
    {
        public static void Draw(KwHouseLayers layers, KwCabinSpec spec, KwBiomeTier tier)
        {
            List<(float X, float Left, float Right)> edge = KwCabinOutline.Roof(spec);
            if (edge.Count < 2)
                return;
            float top = spec.Top, x0 = edge[0].X;
            Func<Vector3, Vector2> uv = p => KwUv.Deck(p, x0);
            List<Vector3> outline = Outline(edge, top);
            KwDeckBoards.Lay(layers.Deck, Pieces(edge), x0, edge[edge.Count - 1].X, _ => top, KwDeckLayout.Of(tier), Seed(spec));
            KwPlanks.DeckEdges(layers.Deck, outline, uv);
            layers.Hard.Polygon(outline, outline.ConvertAll(p => uv(p)));
            KwPlanks.DeckSlab(layers.Hard, outline, uv);
        }

        /// <summary>The roof's top all round: up its left edge, back down its right (a pointed end's tip once).</summary>
        public static List<Vector3> Outline(List<(float X, float Left, float Right)> edge, float z)
        {
            List<Vector3> outline = edge.ConvertAll(e => new Vector3(e.X, e.Left, z));
            for (int i = edge.Count - 1; i >= 0; i--)
            {
                if (edge[i].Left + edge[i].Right > 1e-4f)
                    outline.Add(new Vector3(edge[i].X, -edge[i].Right, z));
            }
            return outline;
        }

        /// <summary>The roof's top in four-sided pieces between its places along, each from a to c with its sides (right as -y, left) at both.</summary>
        public static List<(float A, float C, Vector2 SideA, Vector2 SideC)> Pieces(List<(float X, float Left, float Right)> edge)
        {
            List<(float, float, Vector2, Vector2)> pieces = new List<(float, float, Vector2, Vector2)>();
            for (int i = 0; i + 1 < edge.Count; i++)
            {
                var (a, c) = (edge[i], edge[i + 1]);
                if (c.X - a.X > 1e-4f)
                    pieces.Add((a.X, c.X, new Vector2(-a.Right, a.Left), new Vector2(-c.Right, c.Left)));
            }
            return pieces;
        }

        // The roof's seed for its boards, as a deck's: the cabin's own mixed with the roof's length, beam and height.
        private static int Seed(KwCabinSpec spec)
        {
            List<Station> up = KwCabinOutline.Inside(spec).ConvertAll(s => KwCabinOutline.Extend(s, spec.Top));
            return KwDeckBoards.Seed(new KwDef("KW_gen_cabin_roof", "Cabin roof", "deck") { Stations = up, DeckZ = spec.Top, Seed = spec.Seed });
        }

        /// <summary>The roof's area in m², for the cabin's cost.</summary>
        public static float Area(KwCabinSpec spec)
        {
            List<(float X, float Left, float Right)> edge = KwCabinOutline.Roof(spec);
            float area = 0f;
            for (int i = 0; i + 1 < edge.Count; i++)
                area += (edge[i].Left + edge[i].Right + edge[i + 1].Left + edge[i + 1].Right) / 2f * (edge[i + 1].X - edge[i].X);
            return area;
        }
    }
}
