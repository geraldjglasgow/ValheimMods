using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>One piece of a blueprint, in the blueprint's frame: x across the front, z from the front (the door, -z) to the back, y above the levelled ground, yaw relative to the frame.</summary>
    public sealed class BlueprintPiece
    {
        public string Prefab;
        public float X;
        public float Y;
        public float Z;
        public float Yaw;
    }

    /// <summary>A frame-aligned square levelled to the ground plus <see cref="Y"/> (negative: dug, e.g. a canal).</summary>
    public sealed class LevelStep
    {
        public float X;
        public float Z;
        public float Half;
        public float Y;

        public bool Contains(float x, float z) => Mathf.Abs(x - X) <= Half && Mathf.Abs(z - Z) <= Half;
    }

    /// <summary>A frame-aligned square painted with <see cref="Paint"/> (a road, a field).</summary>
    public sealed class PaintStep
    {
        public float X;
        public float Z;
        public float Half;
        public GroundPaint Paint;

        public bool Contains(float x, float z) => Mathf.Abs(x - X) <= Half && Mathf.Abs(z - Z) <= Half;
    }

    /// <summary>
    /// The ground saved with a build ('openkeep blueprint save'): heights above the levelled ground on a 1 m grid in the frame,
    /// row by row from the front (<see cref="Z0"/>) to the back, each row from <see cref="X0"/> rightwards.
    /// </summary>
    public sealed class Relief
    {
        public float X0;
        public float Z0;
        public int Width;
        public int Depth;
        public float[] Offsets;

        /// <summary>The saved height at a frame point (bilinear), or null outside the grid.</summary>
        public float? At(float x, float z)
        {
            float fx = x - X0, fz = z - Z0;
            if (fx < 0f || fz < 0f || fx > Width - 1 || fz > Depth - 1)
                return null;
            int ix = Mathf.Min((int)fx, Width - 2), iz = Mathf.Min((int)fz, Depth - 2);
            float tx = fx - ix, tz = fz - iz;
            float front = Mathf.Lerp(Get(ix, iz), Get(ix + 1, iz), tx);
            float back = Mathf.Lerp(Get(ix, iz + 1), Get(ix + 1, iz + 1), tx);
            return Mathf.Lerp(front, back, tz);
        }

        private float Get(int x, int z) => Offsets[Mathf.Clamp(z, 0, Depth - 1) * Width + Mathf.Clamp(x, 0, Width - 1)];
    }

    /// <summary>
    /// A saved build: its pieces in placement order (support order: lowest first, what hangs after what holds it) and
    /// its site - the levelled squares or saved relief the ground is shaped to, painted squares, and the water rule.
    /// The same JSON files DevBridge's blueprint.py writes and builds.
    /// </summary>
    public sealed class Blueprint
    {
        public string Name;
        public string Description = "";
        public string File;
        public readonly List<BlueprintPiece> Pieces = new List<BlueprintPiece>();
        public readonly List<LevelStep> Levels = new List<LevelStep>();
        public readonly List<PaintStep> Paints = new List<PaintStep>();
        public Relief Relief;

        /// <summary>The site has water: dug ground lies <see cref="WaterFloor"/> below the levelled ground, holding at least <see cref="WaterDepth"/> metres.</summary>
        public bool HasWater;
        public float WaterFloor;
        public float WaterDepth;

        /// <summary>The pieces' frame bounds (x min, z min, width, depth), set by <see cref="Finish"/>.</summary>
        public Rect PieceBounds;

        /// <summary>Works out the derived values after reading; without levelled squares or relief, one square covers the pieces.</summary>
        public void Finish()
        {
            PieceBounds = Bounds(Pieces);
            if (Levels.Count == 0 && Relief == null && Pieces.Count > 0)
                Levels.Add(DefaultPad());
        }

        /// <summary>The pad of a blueprint without a site: a square over the pieces that stand on the ground, plus half a metre.</summary>
        private LevelStep DefaultPad()
        {
            List<BlueprintPiece> low = Pieces.FindAll(p => p.Y < 1f);
            Rect r = Bounds(low.Count > 0 ? low : Pieces);
            float half = Mathf.Max(r.width, r.height) * 0.5f + 0.5f;
            return new LevelStep { X = r.center.x, Z = r.center.y, Half = half, Y = 0f };
        }

        private static Rect Bounds(List<BlueprintPiece> pieces)
        {
            if (pieces.Count == 0)
                return new Rect(0f, 0f, 0f, 0f);
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (BlueprintPiece p in pieces)
            {
                minX = Mathf.Min(minX, p.X);
                maxX = Mathf.Max(maxX, p.X);
                minZ = Mathf.Min(minZ, p.Z);
                maxZ = Mathf.Max(maxZ, p.Z);
            }
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }
    }
}
