using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>One world vertex of the ground work: the height it goes to (when it moves), the height it had, and the paint it takes.</summary>
    public struct GroundPoint
    {
        public int X;
        public int Z;
        public float Height;
        public float Before;
        public bool Moves;
        public GroundPaint Paint;
    }

    /// <summary>The ground work a placed blueprint or a ground fix needs: every vertex to move or paint, and what it amounts to.</summary>
    public sealed class GroundWork
    {
        public readonly List<GroundPoint> Points = new List<GroundPoint>();

        /// <summary>Pad vertices with no loaded ground under them (too far from the player).</summary>
        public int Unloaded;

        /// <summary>Vertices that stop short of their height: the game keeps the ground within 8 m of the generated one.</summary>
        public int Limited;

        /// <summary>Earth taken away and brought in, cubic metres (one square metre per vertex).</summary>
        public float Cut;
        public float Fill;

        /// <summary>The deepest cut and the highest fill, metres.</summary>
        public float MaxCut;
        public float MaxFill;

        /// <summary>How far the ground moves at each moving vertex, by <see cref="Key"/>.</summary>
        public readonly Dictionary<long, float> Moves = new Dictionary<long, float>();

        public static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        /// <summary>How far the ground moves at the vertex nearest a world point (0 where it stays).</summary>
        public float MoveAt(Vector3 world)
        {
            return Moves.TryGetValue(Key(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z)), out float move) ? move : 0f;
        }

        /// <summary>Stone the fill needs and the cut gives, at <see cref="BlueprintRules.StonePerCubicMetre"/>.</summary>
        public int StoneNeeded => Mathf.CeilToInt(Fill * BlueprintRules.StonePerCubicMetre - 0.001f);
        public int StoneRemoved => Mathf.FloorToInt(Cut * BlueprintRules.StonePerCubicMetre + 0.001f);
    }

    /// <summary>
    /// Fits the ground to a target. Inside the pad every vertex goes to its pad height. Around the pad the ground is cut
    /// back at <see cref="BlueprintRules.CutSlope"/> where it is higher and filled at <see cref="BlueprintRules.FillSlope"/>
    /// where it is lower, measured from the nearest pad vertex, out to <see cref="BlueprintRules.SkirtReach"/>; ground
    /// already within those slopes is left as it is. Smoothing passes round off the slopes where they meet the ground
    /// and the pad, so the ground looks grown rather than cut. Every height stays within the game's 8 m of the generated
    /// ground. Paint goes where the target says, its height kept where the ground does not move.
    /// </summary>
    public static class GroundFit
    {
        private const float HeightTolerance = 0.02f;
        private static readonly int[] StepX = { -1, 1, 0, 0 };
        private static readonly int[] StepZ = { 0, 0, -1, 1 };

        public static GroundWork Plan(Blueprint bp, BuildFrame frame)
        {
            Rect world = SiteArea.World(bp, frame, BlueprintRules.SkirtReach);
            return Plan(GroundGrid.Sample(world, new BlueprintTarget(bp, frame)), 0);
        }

        internal static GroundWork Plan(GroundGrid g, int smoothPasses)
        {
            float[] targets = new float[g.Count];
            for (int i = 0; i < g.Count; i++)
                targets[i] = float.IsNaN(g.Current[i]) ? float.NaN : Target(g, i, g.Current[i]);
            for (int pass = 0; pass < smoothPasses; pass++)
                targets = Smooth(g, targets);
            GroundWork work = new GroundWork();
            for (int i = 0; i < g.Count; i++)
                Emit(g, i, targets[i], work);
            return work;
        }

        private static void Emit(GroundGrid g, int i, float wanted, GroundWork work)
        {
            float now = g.Current[i];
            if (float.IsNaN(now))
            {
                if (!float.IsNaN(g.Pad[i]))
                    work.Unloaded++;
                return;
            }
            float target = Limit(wanted, g.Natural[i], work);
            bool moves = Mathf.Abs(target - now) > HeightTolerance;
            if (!moves && g.Paint[i] == GroundPaint.None)
                return;
            work.Points.Add(new GroundPoint { X = g.X(i), Z = g.Z(i), Height = target, Before = now, Moves = moves, Paint = g.Paint[i] });
            if (!moves)
                return;
            Count(work, now, target);
            work.Moves[GroundWork.Key(g.X(i), g.Z(i))] = Mathf.Abs(target - now);
        }

        /// <summary>The pad height on the pad; around it the current height kept within the cut and fill slopes of the nearest pad vertex.</summary>
        private static float Target(GroundGrid g, int i, float now)
        {
            if (!float.IsNaN(g.Pad[i]))
                return g.Pad[i];
            int source = g.Source[i];
            if (source < 0)
                return now;
            float d = g.Distance(i, source);
            if (d > BlueprintRules.SkirtReach)
                return now;
            float pad = g.Pad[source];
            return Mathf.Clamp(now, pad - BlueprintRules.FillSlope * d, pad + BlueprintRules.CutSlope * d);
        }

        /// <summary>One pass: every slope vertex that moves goes halfway toward the mean of its four neighbours; the pad stays.</summary>
        private static float[] Smooth(GroundGrid g, float[] targets)
        {
            float[] next = (float[])targets.Clone();
            for (int i = 0; i < g.Count; i++)
            {
                if (!float.IsNaN(g.Pad[i]) || float.IsNaN(targets[i]) || Mathf.Abs(targets[i] - g.Current[i]) <= HeightTolerance)
                    continue;
                next[i] = 0.5f * targets[i] + 0.5f * NeighbourMean(g, targets, i);
            }
            return next;
        }

        private static float NeighbourMean(GroundGrid g, float[] targets, int i)
        {
            int x = i % g.Width, z = i / g.Width;
            float sum = 0f;
            int n = 0;
            for (int k = 0; k < 4; k++)
            {
                int nx = x + StepX[k], nz = z + StepZ[k];
                if (nx < 0 || nz < 0 || nx >= g.Width || nz >= g.Depth || float.IsNaN(targets[nz * g.Width + nx]))
                    continue;
                sum += targets[nz * g.Width + nx];
                n++;
            }
            return n > 0 ? sum / n : targets[i];
        }

        /// <summary>The target within the game's limit around the generated ground, counted when it had to stop short.</summary>
        private static float Limit(float target, float natural, GroundWork work)
        {
            float limited = Mathf.Clamp(target, natural - BlueprintRules.GameLimit, natural + BlueprintRules.GameLimit);
            if (Mathf.Abs(limited - target) > HeightTolerance)
                work.Limited++;
            return limited;
        }

        private static void Count(GroundWork work, float now, float target)
        {
            if (target < now)
            {
                work.Cut += now - target;
                work.MaxCut = Mathf.Max(work.MaxCut, now - target);
            }
            else
            {
                work.Fill += target - now;
                work.MaxFill = Mathf.Max(work.MaxFill, target - now);
            }
        }
    }
}
