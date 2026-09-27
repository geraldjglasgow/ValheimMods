using System;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Gentle slopes, on the owner right after a height edit was written: the ground around the edited points is relaxed
    /// outward from them so no step to a neighbouring point is steeper than the configured angle. Digging lowers the
    /// surroundings, raising lifts them. Only points the edit or the relaxation moved push their neighbours, so natural
    /// cliffs nearby stay as they are, and a point once lowered is never raised again (and the other way round), which
    /// makes the pass finish. Works within one compiler: its edge points, shared with the neighbouring heightmap, never
    /// move (the seam stays closed), and points under building pieces stay when the edit skips them.
    /// </summary>
    public static class SlopeRelax
    {
        private static readonly int[] offsetX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] offsetY = { -1, -1, -1, 0, 0, 1, 1, 1 };
        private const byte Far = 255;

        private static float[] heights = new float[0];
        private static sbyte[] moved = new sbyte[0];
        private static byte[] steps = new byte[0];
        private static bool[] anchor = new bool[0];
        private static bool[] queued = new bool[0];
        private static sbyte[] covered = new sbyte[0];
        private static int[] queue = new int[0];
        private static int head;
        private static int size;

        /// <summary>The edit reshapes the ground (not smoothing, reset, paint or an undo restore) and gentle slopes are on.</summary>
        public static bool Applies(TerrainEdit edit)
        {
            if (edit == null || !SlopeSettings.On || edit.Has(EditFlags.IsRestore))
                return false;
            if (edit.Kind == EditKind.Vertices)
                return edit.Vertices != null && edit.Vertices.Mode == VertexMode.Targets;
            HeightOp op = edit.Stroke != null ? edit.Stroke.Height : HeightOp.None;
            return op != HeightOp.None && op != HeightOp.Smooth && op != HeightOp.Reset;
        }

        /// <summary>Relaxes around the points in <paramref name="edited"/> (already written into the view) into <paramref name="output"/>.</summary>
        public static void Run(HeightView view, ChangeBuffer edited, LimitContext limits, ChangeBuffer output)
        {
            output.Clear();
            if (edited.HeightCount == 0)
                return;
            Prepare(view, edited);
            int reach = Mathf.Clamp(Mathf.CeilToInt(SlopeSettings.RadiusValue / view.Scale), 1, Far - 1);
            MarkRegion(view, reach);
            Relax(view, SlopeSettings.Gradient * view.Scale);
            Collect(view, limits, output);
        }

        private static void Prepare(HeightView view, ChangeBuffer edited)
        {
            int count = view.Count;
            Ensure(count);
            for (int i = 0; i < count; i++)
                heights[i] = view.Current(i);
            Array.Clear(moved, 0, count);
            Array.Clear(anchor, 0, count);
            Array.Clear(covered, 0, count);
            for (int i = 0; i < count; i++)
                steps[i] = Far;
            for (int n = 0; n < edited.HeightCount; n++)
            {
                anchor[edited.Heights[n].Index] = true;
                steps[edited.Heights[n].Index] = 0;
            }
        }

        /// <summary>Breadth-first from the edited points: how many steps each point lies from the nearest of them, up to the reach.</summary>
        private static void MarkRegion(HeightView view, int reach)
        {
            StartQueue(view);
            while (size > 0)
            {
                int n = Pop();
                int x = n % view.Pitch;
                int y = n / view.Pitch;
                for (int k = 0; k < 8; k++)
                {
                    int v = Neighbour(view, x + offsetX[k], y + offsetY[k]);
                    if (v < 0 || steps[v] <= steps[n] + 1 || steps[n] + 1 > reach)
                        continue;
                    steps[v] = (byte)(steps[n] + 1);
                    Push(v);
                }
            }
        }

        /// <summary>From the edited points outward: every neighbour too high is lowered, every one too low lifted, and passes it on.</summary>
        private static void Relax(HeightView view, float gradient)
        {
            StartQueue(view);
            int budget = view.Count * 32;
            while (size > 0 && budget-- > 0)
            {
                int n = Pop();
                queued[n] = false;
                int x = n % view.Pitch;
                int y = n / view.Pitch;
                for (int k = 0; k < 8; k++)
                {
                    int v = Neighbour(view, x + offsetX[k], y + offsetY[k]);
                    float allowed = k == 1 || k == 3 || k == 4 || k == 6 ? gradient : gradient * 1.41421356f;
                    if (v >= 0 && Movable(view, v))
                        Pull(v, heights[n], allowed);
                }
            }
        }

        private static void Pull(int v, float from, float allowed)
        {
            if (heights[v] > from + allowed && moved[v] <= 0)
            {
                heights[v] = from + allowed;
                moved[v] = -1;
            }
            else if (heights[v] < from - allowed && moved[v] >= 0)
            {
                heights[v] = from - allowed;
                moved[v] = 1;
            }
            else
            {
                return;
            }
            if (!queued[v])
                Push(v);
        }

        /// <summary>Inside the reach, not an edited point, not on the heightmap's edge, not under a piece (when the edit skips those).</summary>
        private static bool Movable(HeightView view, int v)
        {
            int x = v % view.Pitch;
            int y = v / view.Pitch;
            if (anchor[v] || steps[v] == Far || view.OnEdge(x, y))
                return false;
            if (view.UnderPiece == null)
                return true;
            if (covered[v] == 0)
                covered[v] = (sbyte)(view.UnderPiece(view.VertexX(x), heights[v] + view.OriginY, view.VertexZ(y)) ? 1 : -1);
            return covered[v] < 0;
        }

        private static void Collect(HeightView view, LimitContext limits, ChangeBuffer output)
        {
            for (int i = 0; i < view.Count; i++)
            {
                if (moved[i] == 0)
                    continue;
                int x = i % view.Pitch;
                int y = i / view.Pitch;
                EngineRecord.Height(view, limits, output, i, view.VertexX(x), view.VertexZ(y), view.Current(i), heights[i], false);
            }
        }

        private static int Neighbour(HeightView view, int x, int y) => x < 0 || y < 0 || x > view.Width || y > view.Width ? -1 : y * view.Pitch + x;

        /// <summary>The queue starts with every edited point.</summary>
        private static void StartQueue(HeightView view)
        {
            head = 0;
            size = 0;
            Array.Clear(queued, 0, view.Count);
            for (int i = 0; i < view.Count; i++)
            {
                if (anchor[i])
                    Push(i);
            }
        }

        private static void Push(int v)
        {
            queue[(head + size) % queue.Length] = v;
            size++;
            queued[v] = true;
        }

        private static int Pop()
        {
            int v = queue[head];
            head = (head + 1) % queue.Length;
            size--;
            return v;
        }

        private static void Ensure(int count)
        {
            if (heights.Length == count)
                return;
            heights = new float[count];
            moved = new sbyte[count];
            steps = new byte[count];
            anchor = new bool[count];
            queued = new bool[count];
            covered = new sbyte[count];
            queue = new int[count];
        }
    }
}
