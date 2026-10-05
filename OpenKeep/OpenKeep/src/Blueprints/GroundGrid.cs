using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>What the ground should become at a world vertex: a placed blueprint, or a building whose ground is fixed.</summary>
    internal interface IGroundTarget
    {
        /// <summary>The pad height at a vertex, given the ground there now (NaN: not part of the pad), and its paint (None: kept).</summary>
        float Pad(int x, int z, float current, out GroundPaint paint);
    }

    /// <summary>A placed blueprint as a ground target: its levelled squares or saved relief, its paint and dirt under its pieces.</summary>
    internal sealed class BlueprintTarget : IGroundTarget
    {
        private readonly Blueprint bp;
        private readonly BuildFrame frame;
        private readonly GroundMask mask;

        public BlueprintTarget(Blueprint bp, BuildFrame frame)
        {
            this.bp = bp;
            this.frame = frame;
            mask = GroundMask.For(bp);
        }

        public float Pad(int x, int z, float current, out GroundPaint paint)
        {
            Vector2 local = frame.Local(x, z);
            paint = SiteArea.PaintAt(bp, mask, local.x, local.y);
            return SiteArea.PadOffset(bp, local.x, local.y, out float offset) ? frame.Ground + offset : float.NaN;
        }
    }

    /// <summary>
    /// The world vertices (whole metres) of an area, sampled once: the ground now and as the world generated it (the
    /// game keeps the ground within 8 m of that), the pad height the target asks for (NaN outside the pad), the paint
    /// it asks for, and for every vertex outside the pad the nearest pad vertex (two sweeps passing nearest sources
    /// along, close to the true Euclidean nearest).
    /// </summary>
    internal sealed class GroundGrid
    {
        public int MinX;
        public int MinZ;
        public int Width;
        public int Depth;
        public float[] Current;
        public float[] Natural;
        public float[] Pad;
        public GroundPaint[] Paint;
        public int[] Source;

        private Heightmap lastMap;

        public int Count => Width * Depth;

        public int X(int i) => MinX + i % Width;

        public int Z(int i) => MinZ + i / Width;

        /// <summary>Metres from vertex i to vertex j.</summary>
        public float Distance(int i, int j)
        {
            float dx = i % Width - j % Width, dz = i / Width - j / Width;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Every vertex of the world rectangle (x, z) against the target.</summary>
        public static GroundGrid Sample(Rect world, IGroundTarget target)
        {
            GroundGrid g = Allocate(world);
            for (int i = 0; i < g.Count; i++)
                g.SampleVertex(target, i);
            g.FindNearest();
            return g;
        }

        private static GroundGrid Allocate(Rect world)
        {
            GroundGrid g = new GroundGrid
            {
                MinX = Mathf.FloorToInt(world.xMin), MinZ = Mathf.FloorToInt(world.yMin),
                Width = Mathf.CeilToInt(world.xMax) - Mathf.FloorToInt(world.xMin) + 1,
                Depth = Mathf.CeilToInt(world.yMax) - Mathf.FloorToInt(world.yMin) + 1,
            };
            g.Current = new float[g.Count];
            g.Natural = new float[g.Count];
            g.Pad = new float[g.Count];
            g.Paint = new GroundPaint[g.Count];
            g.Source = new int[g.Count];
            return g;
        }

        private void SampleVertex(IGroundTarget target, int i)
        {
            int x = X(i), z = Z(i);
            SampleGround(new Vector3(x, 0f, z), i);
            Pad[i] = target.Pad(x, z, Current[i], out Paint[i]);
            Source[i] = float.IsNaN(Pad[i]) ? -1 : i;
        }

        /// <summary>The ground shown now and the generated ground at a vertex, NaN where no heightmap is loaded; the last heightmap is tried first.</summary>
        private void SampleGround(Vector3 point, int i)
        {
            if (lastMap == null || !lastMap.IsPointInside(point))
                lastMap = Heightmap.FindHeightmap(point);
            Current[i] = Natural[i] = float.NaN;
            if (lastMap == null || !lastMap.GetWorldHeight(point, out float now) || !lastMap.GetWorldBaseHeight(point, out float natural))
                return;
            Current[i] = now;
            Natural[i] = natural;
        }

        /// <summary>Two sweeps: each vertex takes a neighbour's nearest pad vertex when that one is nearer than its own.</summary>
        private void FindNearest()
        {
            for (int z = 0; z < Depth; z++)
            {
                for (int x = 0; x < Width; x++)
                    RelaxFrom(z * Width + x, x, z, -1);
            }
            for (int z = Depth - 1; z >= 0; z--)
            {
                for (int x = Width - 1; x >= 0; x--)
                    RelaxFrom(z * Width + x, x, z, 1);
            }
        }

        /// <summary>The four neighbours already swept: behind and on the swept row (step -1 forwards, +1 backwards).</summary>
        private void RelaxFrom(int i, int x, int z, int step)
        {
            Relax(i, x + step, z);
            Relax(i, x + step, z + step);
            Relax(i, x, z + step);
            Relax(i, x - step, z + step);
        }

        private void Relax(int i, int nx, int nz)
        {
            if (nx < 0 || nz < 0 || nx >= Width || nz >= Depth)
                return;
            int source = Source[nz * Width + nx];
            if (source < 0 || source == Source[i])
                return;
            if (Source[i] < 0 || Distance(i, source) < Distance(i, Source[i]))
                Source[i] = source;
        }
    }
}
