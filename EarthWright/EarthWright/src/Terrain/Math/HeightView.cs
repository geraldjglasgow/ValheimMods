using System;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// One heightmap's terrain as the engine plans against it, read-only: the pre-edit heights, the compiler's edit
    /// arrays (null for a heightmap nobody has edited) and the paint. The planners only read a view and write their
    /// results into a <see cref="ChangeBuffer"/>, so the owner (Apply) and the sender (Estimate) share one piece of math
    /// and the math can be tested with plain arrays.
    /// <para>Heights are local to the heightmap (add <see cref="OriginY"/> for world heights). Vertex (x, y) sits at the
    /// world position <see cref="VertexX"/>, <see cref="VertexZ"/>; the paint cell with the same index covers the square
    /// metre from that vertex toward +X and +Z (the game's half-cell offset: its grass and "cleared" lookups subtract half
    /// a metre before rounding), so its centre is half a metre further on both axes.</para>
    /// </summary>
    public sealed class HeightView
    {
        public int Width = 64;
        public float Scale = 1f;
        public float OriginX;
        public float OriginY;
        public float OriginZ;

        /// <summary>Heights before any compiler edit (the generated ground, with the flattening of locations).</summary>
        public float[] Base;

        public float[] Level;
        public float[] Smooth;
        public bool[] Modified;
        public Color[] Paint;
        public bool[] PaintModified;

        /// <summary>The generated paint (the heightmap's build data), for the "original" paint.</summary>
        public Color[] BaseMask;

        /// <summary>Read when <see cref="Paint"/> is null: the heightmap's own paint texture.</summary>
        public Texture2D PaintTexture;

        /// <summary>The display clamp: the shown height never leaves the base by more than this.</summary>
        public float Absolute = 8f;

        /// <summary>World height at a world position outside this heightmap (for smoothing across the edge), NaN when unknown.</summary>
        public Func<float, float, float> OutsideHeight;

        /// <summary>A building piece covers the ground at world (x, ground height, z); null when the filter is off.</summary>
        public Func<float, float, float, bool> UnderPiece;

        /// <summary>A buffer the view owns, for base heights it had to copy (reused between plans).</summary>
        public float[] OwnBase;

        public int Pitch => Width + 1;

        public int Count => Pitch * Pitch;

        public float VertexX(int x) => OriginX + (x - Width / 2) * Scale;

        public float VertexZ(int y) => OriginZ + (y - Width / 2) * Scale;

        public float CellX(int x) => VertexX(x) + 0.5f * Scale;

        public float CellZ(int y) => VertexZ(y) + 0.5f * Scale;

        /// <summary>The height shown now at vertex i, local.</summary>
        public float Current(int i)
        {
            float b = Base[i];
            if (Level == null)
                return b;
            float delta = Level[i] + Smooth[i];
            if (delta == 0f)
                return b;
            return Mathf.Clamp(b + delta, b - Absolute, b + Absolute);
        }

        /// <summary>The height at vertex (x, y), local; outside this heightmap read from its neighbour, NaN when not loaded.</summary>
        public float HeightAt(int x, int y)
        {
            if (x >= 0 && y >= 0 && x <= Width && y <= Width)
                return Current(y * Pitch + x);
            if (OutsideHeight == null)
                return float.NaN;
            return OutsideHeight(VertexX(x), VertexZ(y)) - OriginY;
        }

        public Color PaintAt(int i)
        {
            if (Paint != null)
                return Paint[i];
            return PaintTexture != null ? PaintTexture.GetPixel(i % Pitch, i / Pitch) : Color.black;
        }

        public bool PaintModifiedAt(int i) => PaintModified != null && PaintModified[i];

        public bool ModifiedAt(int i) => Modified != null && Modified[i];

        public float LevelAt(int i) => Level != null ? Level[i] : 0f;

        public float SmoothAt(int i) => Smooth != null ? Smooth[i] : 0f;

        public Color OriginalAt(int i) => BaseMask != null && i < BaseMask.Length ? BaseMask[i] : PaintAt(i);

        /// <summary>The vertex nearest to a world position (the game's WorldToVertex); false outside this heightmap.</summary>
        public bool TryVertex(float wx, float wz, out int x, out int y)
        {
            x = Mathf.FloorToInt((wx - OriginX) / Scale + 0.5f) + Width / 2;
            y = Mathf.FloorToInt((wz - OriginZ) / Scale + 0.5f) + Width / 2;
            return x >= 0 && y >= 0 && x <= Width && y <= Width;
        }

        /// <summary>The vertex (or cell, <paramref name="cells"/>) indices whose X lies within [min, max], clipped to the heightmap.</summary>
        public void RangeX(float min, float max, bool cells, out int from, out int to) => Range(min - OriginX, max - OriginX, cells, out from, out to);

        public void RangeZ(float min, float max, bool cells, out int from, out int to) => Range(min - OriginZ, max - OriginZ, cells, out from, out to);

        private void Range(float localMin, float localMax, bool cells, out int from, out int to)
        {
            float shift = cells ? 0.5f : 0f;
            from = Mathf.Max(0, Mathf.CeilToInt(localMin / Scale - shift) + Width / 2);
            to = Mathf.Min(Width, Mathf.FloorToInt(localMax / Scale - shift) + Width / 2);
        }

        /// <summary>The world position lies within this heightmap's square, away from its edges (a heightmap's own centre does).</summary>
        public bool Holds(Vector3 world)
        {
            float half = Width * Scale * 0.5f;
            return Mathf.Abs(world.x - OriginX) < half && Mathf.Abs(world.z - OriginZ) < half;
        }

        /// <summary>True for the vertices and cells a neighbouring heightmap shares (its copy of the same world point).</summary>
        public bool OnEdge(int x, int y) => x == 0 || y == 0 || x == Width || y == Width;
    }
}
