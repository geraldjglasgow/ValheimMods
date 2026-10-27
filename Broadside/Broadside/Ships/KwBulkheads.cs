using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a launched ship's hold is split into compartments (<see cref="KwFloodHolds"/>): bulkheads across the hull at
    /// its planking parts' ends (where one part meets the next, on a rib), any compartment longer than
    /// <see cref="Longest"/> split again evenly, none shorter than <see cref="Shortest"/>, and none where the hull's
    /// bottom stands within <see cref="LowestWall"/> of the bulkheads' top (a keel sweeping up into its stem leaves no wall
    /// there). Always at the slices' own ends, so a compartment is a run of slices. From the blueprint and the slices
    /// alone, so every peer splits a ship the same way. Pure arithmetic, no Unity objects. Water seeps through a bulkhead
    /// and pours over its top (<see cref="KwFloodWall"/>); none is drawn or walked into.
    /// </summary>
    public static class KwBulkheads
    {
        /// <summary>Metres: the longest and shortest compartment, the lowest wall, how near a part's end a slice's end must be.</summary>
        public const float Longest = 4f, Shortest = 1.2f, LowestWall = 0.3f, Near = 0.3f;

        /// <summary>
        /// The first slice of every compartment, then the slice count (compartment k runs from starts[k] up to, not
        /// including, starts[k + 1]); the bulkheads' top at the height <paramref name="top"/> over the keel line.
        /// </summary>
        public static int[] Starts(KwBlueprint? blueprint, List<KwHullSlice> slices, float top)
        {
            List<int> cuts = new List<int>();
            if (slices.Count == 0)
                return new[] { 0, 0 };
            if (blueprint != null)
            {
                foreach (float z in Ends(blueprint))
                    Cut(cuts, slices, Nearest(slices, z, 1, slices.Count - 1), z, top);
            }
            Split(cuts, slices, top);
            Merge(cuts, slices);
            List<int> starts = new List<int> { 0 };
            starts.AddRange(cuts);
            starts.Add(slices.Count);
            return starts.ToArray();
        }

        /// <summary>Where a slice begins along the ship (its aft end), or for the count, where the last one ends.</summary>
        public static float Edge(List<KwHullSlice> slices, int b) =>
            b < slices.Count ? slices[b].Z - slices[b].Length / 2f : slices[b - 1].Z + slices[b - 1].Length / 2f;

        // Every planking part's two ends along the ship, in the ship's frame.
        private static List<float> Ends(KwBlueprint blueprint)
        {
            List<float> ends = new List<float>();
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(blueprint))
            {
                if (tool != "planking")
                    continue;
                float way = (piece.Rotation * Vector3.forward).z;
                ends.Add(piece.Position.z);
                ends.Add(piece.Position.z + way * span.Length);
            }
            return ends;
        }

        // A bulkhead at slice end b, kept in order, when b is near the place asked, inside the hull and walled.
        private static void Cut(List<int> cuts, List<KwHullSlice> slices, int b, float z, float top)
        {
            if (b <= 0 || b >= slices.Count || Mathf.Abs(Edge(slices, b) - z) > Near || cuts.Contains(b) || !Walled(slices, b, top))
                return;
            cuts.Add(b);
            cuts.Sort();
        }

        // The slice end within [from, to] nearest a place along the ship.
        private static int Nearest(List<KwHullSlice> slices, float z, int from, int to)
        {
            int best = -1;
            float gap = float.MaxValue;
            for (int b = from; b <= to && b < slices.Count; b++)
            {
                float d = Mathf.Abs(Edge(slices, b) - z);
                if (d < gap)
                    (gap, best) = (d, b);
            }
            return best;
        }

        // A wall at least LowestWall high there: the hull's bottom on both sides of it under the top by that much.
        private static bool Walled(List<KwHullSlice> slices, int b, float top) =>
            top - Mathf.Max(Bottom(slices[b - 1].Shape), Bottom(slices[b].Shape)) >= LowestWall;

        private static float Bottom(Vector2[] shape)
        {
            float low = float.MaxValue;
            foreach (Vector2 p in shape)
                low = Mathf.Min(low, p.y);
            return low;
        }

        // Every compartment longer than Longest cut evenly into as few as keep each within it.
        private static void Split(List<int> cuts, List<KwHullSlice> slices, float top)
        {
            List<int> ends = new List<int> { 0 };
            ends.AddRange(cuts);
            ends.Add(slices.Count);
            for (int i = 0; i + 1 < ends.Count; i++)
            {
                float aft = Edge(slices, ends[i]), length = Edge(slices, ends[i + 1]) - aft;
                int pieces = Mathf.CeilToInt(length / Longest - 1e-3f);
                for (int j = 1; j < pieces; j++)
                {
                    float z = aft + length * j / pieces;
                    Cut(cuts, slices, Nearest(slices, z, ends[i] + 1, ends[i + 1] - 1), z, top);
                }
            }
        }

        // A compartment shorter than Shortest joined to the next one (the last to the one before it), until none is.
        private static void Merge(List<int> cuts, List<KwHullSlice> slices)
        {
            for (int i = 0; i <= cuts.Count && cuts.Count > 0;)
            {
                int from = i == 0 ? 0 : cuts[i - 1], to = i < cuts.Count ? cuts[i] : slices.Count;
                if (Edge(slices, to) - Edge(slices, from) >= Shortest)
                {
                    i++;
                    continue;
                }
                cuts.RemoveAt(i < cuts.Count ? i : i - 1);
                i = 0;
            }
        }
    }

    /// <summary>
    /// One bulkhead across a launched ship's hold, between two compartments (<see cref="KwBulkheads"/>), in the ship's
    /// frame: where it stands along the ship, its foot (the hull's bottom there) and top (straight across), the hull's
    /// section there (the narrower of its two sides') and its breadth at the top.
    /// </summary>
    public readonly struct KwFloodWall
    {
        private KwFloodWall(float z, float foot, float top, float width, Vector2[] shape)
        {
            (Z, Foot, Top, Width, Shape) = (z, foot, top, width, shape);
        }

        public float Z { get; }
        public float Foot { get; }
        public float Top { get; }
        public float Width { get; }
        public Vector2[] Shape { get; }

        /// <summary>The bulkhead before slice s, its top at a height over the keel line.</summary>
        public static KwFloodWall Before(List<KwHullSlice> slices, int s, float top)
        {
            Vector2[] aft = slices[s - 1].Shape, fore = slices[s].Shape;
            float wa = KwSectionMath.Breadth(aft, top), wf = KwSectionMath.Breadth(fore, top);
            Vector2[] shape = wa <= wf ? aft : fore;
            float foot = float.MaxValue;
            foreach (Vector2 p in shape)
                foot = Mathf.Min(foot, p.y);
            return new KwFloodWall(KwBulkheads.Edge(slices, s), foot, top, Mathf.Min(wa, wf), shape);
        }

        /// <summary>
        /// The way through it for water standing so high over its foot on one side, in square metres: a share
        /// (<paramref name="seep"/>) of its wet wall under its top, where water seeps through the frames' gaps, and a share
        /// (<paramref name="over"/>) of the gap over the top, where it pours across.
        /// </summary>
        public float Opening(float high, float seep, float over)
        {
            float wall = KwSectionMath.Wet(Shape, 0f, 1f, Foot + Mathf.Min(high, Top - Foot), out _);
            return seep * wall + over * Width * Mathf.Max(0f, high - (Top - Foot));
        }
    }
}
