using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>What the dock's waterline draws, in the dock's frame: points, each one's colour, and triangles.</summary>
    public sealed class KwWaterlineDraw
    {
        public List<Vector3> Points { get; } = new List<Vector3>();
        public List<Color> Colors { get; } = new List<Color>();
        public List<int> Triangles { get; } = new List<int>();
        public bool Empty => Triangles.Count == 0;

        public int Point(Vector3 at, Color color)
        {
            Points.Add(at);
            Colors.Add(color);
            return Points.Count - 1;
        }

        public void Quad(int a, int b, int c, int d) => Triangles.AddRange(new[] { a, b, c, a, c, d });
    }

    /// <summary>
    /// The waterline drawn on a ship on the dock (<see cref="KwDockWaterline"/>): a band <see cref="Band"/> tall round the
    /// outside of the hull where the sea will stand (<see cref="KwWaterline"/>), <see cref="Out"/> outside the planking,
    /// following it at every planking station (each station's breadth at the water's height, <see cref="KwSectionMath.Breadth"/>,
    /// just under and over it), straight from station to station as the planks are lofted, and across a hull's end (a flat
    /// stern) just outside it; where the hull's bottom rises out of the water (a stem's forefoot) the band ends at the keel. A
    /// faint sheet inside the hull at the same height, <see cref="Inset"/> in from the outside. Pure arithmetic: the mesh is
    /// made from it by its caller.
    /// </summary>
    public static class KwWaterlineShape
    {
        public const float Band = 0.08f, Out = 0.04f, EndOut = 0.06f, Inset = KwPlanks.Thick + 0.02f;

        /// <summary>The band's colour, the sea's light blue-green, and the inside sheet's, the same faint.</summary>
        public static readonly Color Line = new Color(0.32f, 0.82f, 0.84f, 0.95f), Sheet = new Color(0.32f, 0.76f, 0.86f, 0.14f);

        // Stations closer than this along the dock are one (the end of one part and the start of the next).
        private const float Same = 0.02f;

        // One planking station on the dock: its keel line's middle (dock frame), its closed outline (heights from that keel
        // line), and its bottom's and top's heights and its bottom's half width (the keel's top edge).
        private readonly struct Cut
        {
            public Cut(Vector3 at, Vector2[] half)
            {
                At = at;
                Shape = new Vector2[half.Length * 2];
                (Bottom, Top, Foot) = (float.MaxValue, float.MinValue, half[0].x);
                for (int i = 0; i < half.Length; i++)
                {
                    (Shape[i], Shape[Shape.Length - 1 - i]) = (half[i], new Vector2(-half[i].x, half[i].y));
                    (Bottom, Top) = (Mathf.Min(Bottom, half[i].y), Mathf.Max(Top, half[i].y));
                }
            }

            public Vector3 At { get; }
            public Vector2[] Shape { get; }
            public float Bottom { get; }
            public float Top { get; }
            public float Foot { get; }
        }

        // One place along the band: along the dock, the hull's middle across, the band's height, the hull's half breadth
        // just under it and just over it, and inside the hull (negative: no sheet from here).
        private readonly struct Rim
        {
            public Rim(float z, float x, float y, float low, float high, float inside) => (Z, X, Y, Low, High, Inside) = (z, x, y, low, high, inside);

            public float Z { get; }
            public float X { get; }
            public float Y { get; }
            public float Low { get; }
            public float High { get; }
            public float Inside { get; }

            public Rim Moved(float by) => new Rim(Z + by, X, Y, Low, High, -1f);
        }

        /// <summary>The band and sheet for a blueprint in the dock's frame at this waterline; empty without planking in the water.</summary>
        public static KwWaterlineDraw Of(KwBlueprint dock, KwWaterline line)
        {
            List<Cut> cuts = Cuts(dock, line);
            KwWaterlineDraw draw = new KwWaterlineDraw();
            for (int i = 0; i < cuts.Count; i++)
            {
                if (!Wet(cuts[i], line))
                    continue;
                int last = i;
                while (last + 1 < cuts.Count && Wet(cuts[last + 1], line))
                    last++;
                Run(draw, Rims(cuts, i, last, line), i == 0, last == cuts.Count - 1);
                i = last;
            }
            return draw;
        }

        // Every planking station placed on the dock, in order along it; of those at one place (where one part ends and the
        // next starts, or planking laid twice) the one widest at the water, so the band stands outside them all.
        private static List<Cut> Cuts(KwBlueprint dock, KwWaterline line)
        {
            List<Cut> cuts = new List<Cut>();
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(dock))
            {
                if (tool != "planking")
                    continue;
                foreach (Station station in span.Stations())
                    cuts.Add(new Cut(piece.Position + piece.Rotation * new Vector3(0f, 0f, station.X), station.Half));
            }
            cuts.Sort((a, b) => a.At.z.CompareTo(b.At.z));
            List<Cut> kept = new List<Cut>();
            foreach (Cut cut in cuts)
            {
                if (kept.Count == 0 || cut.At.z - kept[kept.Count - 1].At.z > Same)
                    kept.Add(cut);
                else if (Breadth(cut, line) > Breadth(kept[kept.Count - 1], line))
                    kept[kept.Count - 1] = cut;
            }
            return kept;
        }

        private static float Breadth(Cut cut, KwWaterline line) => KwSectionMath.Breadth(cut.Shape, line.At(cut.At.z) - cut.At.y);

        // The water reaches the station's planking: over its bottom.
        private static bool Wet(Cut cut, KwWaterline line) => line.At(cut.At.z) - cut.At.y >= cut.Bottom;

        // A run of stations in the water, with its two ends: past the hull's end where the run reaches it, else where the
        // hull's bottom comes out of the water between the last station in it and the first out.
        private static List<Rim> Rims(List<Cut> cuts, int first, int last, KwWaterline line)
        {
            List<Rim> rims = new List<Rim>();
            for (int i = first; i <= last; i++)
                rims.Add(At(cuts[i], line));
            rims.Insert(0, first > 0 ? Crossing(cuts[first], cuts[first - 1], line) : rims[0].Moved(-EndOut));
            rims.Add(last + 1 < cuts.Count ? Crossing(cuts[last], cuts[last + 1], line) : rims[rims.Count - 1].Moved(EndOut));
            return rims;
        }

        // The band at a station in the water (the water's height kept under its top, should the sea come over it).
        private static Rim At(Cut cut, KwWaterline line)
        {
            float level = Mathf.Min(line.At(cut.At.z) - cut.At.y, cut.Top - 0.001f);
            float half = Half(cut, level, cut.Foot);
            float low = Half(cut, level - Band / 2f, half), high = Half(cut, Mathf.Min(level + Band / 2f, cut.Top), half);
            return new Rim(cut.At.z, cut.At.x, cut.At.y + level, low, high, Mathf.Max(0f, half - Inset));
        }

        // Where the hull's bottom meets the water between a station in it and one out of it: the band's end, at the keel.
        private static Rim Crossing(Cut wet, Cut dry, KwWaterline line)
        {
            float under = line.At(wet.At.z) - wet.At.y - wet.Bottom, over = line.At(dry.At.z) - dry.At.y - dry.Bottom;
            float t = Mathf.Clamp01(under / Mathf.Max(under - over, 1e-4f));
            Vector3 at = Vector3.Lerp(wet.At, dry.At, t);
            float foot = Mathf.Lerp(wet.Foot, dry.Foot, t);
            return new Rim(at.z, at.x, line.At(at.z), foot, foot, -1f);
        }

        // Half the outline's breadth at a height over its keel line; the fallback where it has none there.
        private static float Half(Cut cut, float level, float fallback)
        {
            float half = KwSectionMath.Breadth(cut.Shape, level) / 2f;
            return half > 0f ? half : fallback;
        }

        // One run drawn: both sides of the band, across the hull's ends where the run reaches them, and the sheet inside.
        private static void Run(KwWaterlineDraw draw, List<Rim> rims, bool aftEnd, bool foreEnd)
        {
            int starboard = Side(draw, rims, 1f), port = Side(draw, rims, -1f), n = rims.Count;
            if (aftEnd)
                draw.Quad(starboard, starboard + 1, port + 1, port);
            if (foreEnd)
                draw.Quad(starboard + 2 * (n - 1), starboard + 2 * (n - 1) + 1, port + 2 * (n - 1) + 1, port + 2 * (n - 1));
            for (int i = 0; i + 1 < n; i++)
                Fill(draw, rims[i], rims[i + 1]);
        }

        // One side of the band, two points a rim (just under the water, just over it) joined rim to rim; its first point.
        private static int Side(KwWaterlineDraw draw, List<Rim> rims, float side)
        {
            int first = draw.Points.Count;
            foreach (Rim r in rims)
            {
                draw.Point(new Vector3(r.X + side * (r.Low + Out), r.Y - Band / 2f, r.Z), Line);
                draw.Point(new Vector3(r.X + side * (r.High + Out), r.Y + Band / 2f, r.Z), Line);
            }
            for (int i = 0; i + 1 < rims.Count; i++)
                draw.Quad(first + 2 * i, first + 2 * i + 1, first + 2 * i + 3, first + 2 * i + 2);
            return first;
        }

        // The sheet inside the hull between two stations in the water, where it has room.
        private static void Fill(KwWaterlineDraw draw, Rim a, Rim b)
        {
            if (a.Inside < 0f || b.Inside < 0f || a.Inside + b.Inside <= 0f)
                return;
            int p = draw.Point(new Vector3(a.X - a.Inside, a.Y, a.Z), Sheet), q = draw.Point(new Vector3(a.X + a.Inside, a.Y, a.Z), Sheet);
            int r = draw.Point(new Vector3(b.X + b.Inside, b.Y, b.Z), Sheet), s = draw.Point(new Vector3(b.X - b.Inside, b.Y, b.Z), Sheet);
            draw.Quad(p, q, r, s);
        }
    }
}
