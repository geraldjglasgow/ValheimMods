using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One slice of a hull along its length: where it stands (ship frame, z), how long it is, its cross-section at its
    /// middle (a closed outline in the ship's x and y, keel to sheer on both sides) and its breadth at the design waterline.</summary>
    public readonly struct KwHullSlice
    {
        public KwHullSlice(float z, float length, Vector2[] shape, float breadth)
        {
            Z = z;
            Length = length;
            Shape = shape;
            Breadth = breadth;
        }

        public float Z { get; }
        public float Length { get; }
        public Vector2[] Shape { get; }
        public float Breadth { get; }
    }

    /// <summary>
    /// A launched ship's hull cut into slices for its buoyancy (<see cref="KwShipHydro"/>): the cross-sections its
    /// planking is drawn from (every planking part's stations, in the ship's frame), in order along the ship, each slice
    /// the stretch to the next section, shaped as the hull is at its middle (<see cref="KwSectionBlend"/>: the two
    /// sections averaged; one end's shape lifted the bow and floated a ship differently each way round). A ship with no
    /// planking is a box of its measured beam and depth. At most <see cref="Most"/> slices, evenly picked.
    /// </summary>
    public static class KwHullSlices
    {
        public const int Most = 40;

        public static List<KwHullSlice> Of(KwBlueprint blueprint, KwBpHull hull, float waterline)
        {
            List<(float Z, Vector2[] Shape)> sections = Rings(blueprint, hull);
            List<KwHullSlice> slices = new List<KwHullSlice>();
            for (int i = 0; i + 1 < sections.Count; i++)
            {
                float length = sections[i + 1].Z - sections[i].Z;
                if (length < 0.01f)
                    continue;
                Vector2[] shape = KwSectionBlend.Middle(sections[i].Shape, sections[i + 1].Shape);
                slices.Add(new KwHullSlice((sections[i].Z + sections[i + 1].Z) / 2f, length, shape, KwSectionMath.Breadth(shape, waterline)));
            }
            return slices;
        }

        /// <summary>The hull's cross-sections in order along it (the slices run from each to the next), at most <see cref="Most"/>.</summary>
        public static List<(float Z, Vector2[] Shape)> Rings(KwBlueprint blueprint, KwBpHull hull)
        {
            List<(float Z, Vector2[] Shape)> sections = Sections(blueprint);
            if (sections.Count < 2)
                sections = Box(hull);
            sections.Sort((a, b) => a.Z.CompareTo(b.Z));
            return Thin(sections);
        }

        // Every planking station as a closed outline in the ship's frame (the part's height added, both sides).
        private static List<(float, Vector2[])> Sections(KwBlueprint blueprint)
        {
            List<(float, Vector2[])> sections = new List<(float, Vector2[])>();
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(blueprint))
            {
                if (tool != "planking")
                    continue;
                float way = (piece.Rotation * Vector3.forward).z;
                foreach (Station station in span.Stations())
                    sections.Add((piece.Position.z + way * station.X, Outline(station.Half, piece.Position.y)));
            }
            return sections;
        }

        // A half outline (out from the centre line, keel to sheer) as both sides, closed: starboard up, port down.
        private static Vector2[] Outline(Vector2[] half, float lift)
        {
            Vector2[] shape = new Vector2[half.Length * 2];
            for (int i = 0; i < half.Length; i++)
            {
                shape[i] = new Vector2(half[i].x, half[i].y + lift);
                shape[shape.Length - 1 - i] = new Vector2(-half[i].x, half[i].y + lift);
            }
            return shape;
        }

        // No planking: a box of the hull's beam and depth, its whole length.
        private static List<(float, Vector2[])> Box(KwBpHull hull)
        {
            float b = hull.Beam / 2f, d = hull.Depth;
            Vector2[] shape = { new Vector2(b, 0f), new Vector2(b, d), new Vector2(-b, d), new Vector2(-b, 0f) };
            float mid = hull.Origin.z, half = hull.Length / 2f;
            return new List<(float, Vector2[])> { (mid - half, shape), (mid + half, shape) };
        }

        // At most Most sections, evenly picked, the first and last kept; sections at one place merged (the first kept).
        private static List<(float Z, Vector2[] Shape)> Thin(List<(float Z, Vector2[] Shape)> sections)
        {
            List<(float Z, Vector2[] Shape)> kept = new List<(float, Vector2[])>();
            foreach ((float z, Vector2[] shape) in sections)
            {
                if (kept.Count == 0 || z - kept[kept.Count - 1].Z > 0.05f)
                    kept.Add((z, shape));
            }
            if (kept.Count <= Most + 1)
                return kept;
            List<(float Z, Vector2[] Shape)> even = new List<(float, Vector2[])>();
            for (int i = 0; i <= Most; i++)
                even.Add(kept[Mathf.RoundToInt(i * (kept.Count - 1) / (float)Most)]);
            return even;
        }
    }
}
