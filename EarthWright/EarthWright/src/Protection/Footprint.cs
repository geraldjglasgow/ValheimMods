using System.Collections.Generic;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>A circle on the ground (only X and Z count): one part of an edit's footprint.</summary>
    public struct Disc
    {
        public Vector3 Center;
        public float Radius;

        public Disc(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }

    /// <summary>
    /// The ground an edit may change, as circles the protection checks test against wards, locations and zones. A
    /// brush stroke is one circle around its whole footprint, corners included. Explicit vertex sets (ramps, roads, undo
    /// restores) are bucketed into 4 m cells, so a long ramp is not judged by one huge circle and an undo is not judged
    /// by its whole 64 m heightmap. Every circle grows by the ring gentle slopes may relax beyond the edit
    /// (<c>Engine.ExtraReach</c>). The last result is reused within a frame, because several guards ask for the same
    /// edit one after the other.
    /// </summary>
    public static class Footprint
    {
        private const float Cell = 4f;
        private const int Pitch = 65;
        private const int Half = 32;

        /// <summary>Half the diagonal of a cell, plus half a metre for grid snapping.</summary>
        private const float CellRadius = Cell * 0.5f * 1.415f + 0.5f;

        private static TerrainEdit lastEdit;
        private static int lastFrame = -1;
        private static List<Disc> lastDiscs = new List<Disc>();

        public static List<Disc> Of(TerrainEdit edit)
        {
            if (ReferenceEquals(edit, lastEdit) && lastFrame == Time.frameCount)
                return lastDiscs;
            lastDiscs = Compute(edit);
            lastEdit = edit;
            lastFrame = Time.frameCount;
            return lastDiscs;
        }

        private static List<Disc> Compute(TerrainEdit edit)
        {
            List<Disc> discs = new List<Disc>();
            if (edit == null)
                return discs;
            // Gentle slopes may relax the ground this far beyond the edit's own vertices; that ground counts too.
            float extra = Mathf.Max(0f, Engine.ExtraReach(edit));
            if (edit.Kind == EditKind.Stroke)
            {
                if (edit.Stroke != null)
                    discs.Add(new Disc(edit.Stroke.Center, StrokeRadius(edit.Stroke) + extra));
            }
            else if (edit.Vertices != null)
            {
                Bucket(Points(edit.Vertices), CellRadius + extra, discs);
            }
            return discs;
        }

        /// <summary>A radius around the stroke's centre that holds every vertex and paint cell it changes.</summary>
        public static float StrokeRadius(BrushStroke stroke)
        {
            float size = Mathf.Max(Mathf.Max(stroke.Radius, stroke.Radius2), stroke.EffectivePaintRadius);
            bool cornered = stroke.Shape == BrushShape.Square || stroke.Shape == BrushShape.Rectangle || stroke.Shape == BrushShape.Frame;
            return (cornered ? size * 1.415f : size) + 0.5f;
        }

        /// <summary>World X and Z of every vertex of the set: targets carry them, restores carry heightmap indices.</summary>
        private static IEnumerable<Vector2> Points(VertexSet set)
        {
            if (set.Mode == VertexMode.Restore)
            {
                Vector3 origin = set.CompPosition;
                foreach (RawVertex v in set.Raw)
                    yield return new Vector2(origin.x + (v.Index % Pitch - Half), origin.z + (v.Index / Pitch - Half));
                yield break;
            }
            foreach (TargetVertex v in set.Targets)
                yield return new Vector2(v.X, v.Z);
        }

        private static void Bucket(IEnumerable<Vector2> points, float radius, List<Disc> discs)
        {
            HashSet<long> seen = new HashSet<long>();
            foreach (Vector2 p in points)
            {
                int cx = Mathf.FloorToInt(p.x / Cell);
                int cz = Mathf.FloorToInt(p.y / Cell);
                if (seen.Add(((long)cx << 32) ^ (uint)cz))
                    discs.Add(new Disc(new Vector3((cx + 0.5f) * Cell, 0f, (cz + 0.5f) * Cell), radius));
            }
        }
    }
}
