using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Explicit per-vertex values computed by the sender. <see cref="VertexMode.Targets"/> entries are world vertex
    /// coordinates (whole metres) and are split per compiler by the dispatcher, a vertex on a shared edge going to both;
    /// <see cref="VertexMode.Restore"/> sets address one compiler by <see cref="CompPosition"/> and carry raw array values.
    /// </summary>
    public sealed class VertexSet
    {
        public VertexMode Mode = VertexMode.Targets;

        /// <summary>Restore only: the position of the heightmap (and its compiler) the indices belong to.</summary>
        public Vector3 CompPosition;

        public readonly List<TargetVertex> Targets = new List<TargetVertex>();

        public readonly List<RawVertex> Raw = new List<RawVertex>();

        /// <summary>The world-space bounds (XZ) of the entries, for finding the heightmaps they touch.</summary>
        public Bounds Area()
        {
            if (Mode == VertexMode.Restore)
                return RestoreArea();
            if (Targets.Count == 0)
                return new Bounds(Vector3.zero, Vector3.zero);
            Bounds bounds = new Bounds(new Vector3(Targets[0].X, 0f, Targets[0].Z), Vector3.zero);
            foreach (TargetVertex v in Targets)
                bounds.Encapsulate(new Vector3(v.X, 0f, v.Z));
            bounds.Expand(new Vector3(2f, 2000f, 2f));
            return bounds;
        }

        /// <summary>
        /// Restore sets: the bounds of the restored indices, so guards and the grass reset only look at the ground that
        /// changes, not the whole heightmap (64 m heightmaps, 65 vertices per row, index = y * 65 + x).
        /// </summary>
        private Bounds RestoreArea()
        {
            if (Raw.Count == 0)
                return new Bounds(CompPosition, new Vector3(66f, 2000f, 66f));
            Bounds bounds = new Bounds(IndexWorld(Raw[0].Index), Vector3.zero);
            foreach (RawVertex v in Raw)
                bounds.Encapsulate(IndexWorld(v.Index));
            bounds.Expand(new Vector3(2f, 2000f, 2f));
            return bounds;
        }

        private Vector3 IndexWorld(int index) => new Vector3(CompPosition.x + index % 65 - 32, CompPosition.y, CompPosition.z + index / 65 - 32);
    }

    /// <summary>A target for one vertex: the height blends from the current one to <see cref="Height"/> by <see cref="Weight"/>.</summary>
    public struct TargetVertex
    {
        /// <summary>World vertex coordinates, whole metres.</summary>
        public int X;
        public int Z;

        /// <summary>Absolute world height.</summary>
        public float Height;

        /// <summary>0..1; 1 sets the height exactly.</summary>
        public float Weight;

        /// <summary>Paint for the cell at this vertex; <see cref="PaintOp.None"/> keeps the ground texture.</summary>
        public PaintOp Paint;

        /// <summary>0..1.</summary>
        public float PaintStrength;
    }

    /// <summary>Raw TerrainComp values of one array index (the same index is used for height and paint).</summary>
    public struct RawVertex
    {
        public int Index;
        public bool HeightModified;
        public float LevelDelta;
        public float SmoothDelta;
        public bool PaintModified;
        public Color Paint;
    }
}
