using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The zones kept clear marked into the cells (DECORATE.md, "Kept clear"), once the pieces round the area are gathered
    /// (<see cref="KwDecorKeepClear"/>): first a 1.6 m way between each path end (a door's side, a ladder's foot, a
    /// station's front, a portal's side, a dock's entry) and its two nearest others within 18 m, then every zone drawn into
    /// the cells it covers (each box only over the cells inside its own reach, so a large area with many pieces stays
    /// cheap). Plain arithmetic.
    /// </summary>
    public static class KwDecorClear
    {
        private const float WayHalf = 0.8f, WayReach = 18f;

        /// <summary>The ways between path ends, as zones kept clear.</summary>
        public static void Ways(KwDecorArea area)
        {
            List<Vector3> ends = new List<Vector3>(area.Ways);
            ends.AddRange(area.Entries);
            foreach (Vector3 end in ends.ToArray())
            {
                ends.Sort((a, b) => Flat(a - end).CompareTo(Flat(b - end)));
                for (int n = 1; n < Mathf.Min(3, ends.Count); n++)
                {
                    if (Flat(ends[n] - end) <= WayReach * WayReach)
                        area.Kept.Add(Between(end, ends[n]));
                }
            }
        }

        private static float Flat(Vector3 d) => d.x * d.x + d.z * d.z;

        private static KwDecorBox Between(Vector3 a, Vector3 b)
        {
            Vector3 along = new Vector3(b.x - a.x, 0f, b.z - a.z);
            Vector3 right = new Vector3(along.z, 0f, -along.x);
            return new KwDecorBox((a + b) / 2f, right, WayHalf, along.magnitude / 2f);
        }

        /// <summary>Every zone into the cells whose middles it holds.</summary>
        public static void Mark(KwDecorGrid grid, List<KwDecorBox> boxes)
        {
            foreach (KwDecorBox box in boxes)
                Mark(grid, box);
        }

        private static void Mark(KwDecorGrid grid, KwDecorBox box)
        {
            (int i0, int j0, int i1, int j1) = Reach(grid, box);
            for (int j = j0; j < j1; j++)
            {
                for (int i = i0; i < i1; i++)
                {
                    if (box.Contains(grid.At(i, j)))
                        grid.Cells[grid.Index(i, j)].Marks |= KwCellMark.Clear;
                }
            }
        }

        // The cells a box can reach, clamped to the grid: its centre in cells plus its radius either way.
        private static (int, int, int, int) Reach(KwDecorGrid grid, KwDecorBox box)
        {
            Vector3 d = box.Centre - grid.Centre;
            float u = (Vector3.Dot(d, grid.Right) + grid.Half) / KwDecorGrid.Step, v = (Vector3.Dot(d, grid.Forward) + grid.Half) / KwDecorGrid.Step;
            float r = Mathf.Sqrt(box.HalfX * box.HalfX + box.HalfZ * box.HalfZ) / KwDecorGrid.Step + 1f;
            int i0 = Mathf.Max(0, Mathf.FloorToInt(u - r)), j0 = Mathf.Max(0, Mathf.FloorToInt(v - r));
            int i1 = Mathf.Min(grid.N, Mathf.CeilToInt(u + r)), j1 = Mathf.Min(grid.N, Mathf.CeilToInt(v + r));
            return (i0, j0, i1, j1);
        }
    }
}
