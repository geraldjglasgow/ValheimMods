using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ground and the water in the read area (DECORATE.md, "Reading the area"). Painted ground (the hoe's path, a paved
    /// road) is a path kept clear where its strip is 4.5 m across or less; a wider painted yard is open ground. Each cell's
    /// distance to the nearest water (up to 16 m) and the way to it (<see cref="KwDecorArea.WaterWay"/>); dry walkable ground
    /// within 1.5 m of water and the waterline itself (0.4 m deep at most, beside dry ground) are the shore. Wall faces found
    /// beside walkable cells give those cells their way to the wall when nothing nearer does.
    /// </summary>
    public static class KwDecorGround
    {
        private const float PathWidth = 4.5f, ShoreReach = 1.5f, Waterline = 0.4f, Far = 16f, Step = KwDecorGrid.Step;

        public static void Read(KwDecorArea area)
        {
            Paths(area.Grid);
            Water(area.Grid, area.WaterWay);
            Walls(area.Grid, area.Walls);
        }

        private static void Paths(KwDecorGrid grid)
        {
            int count = grid.Cells.Length;
            int[] rx = new int[count], px = new int[count], rz = new int[count], pz = new int[count];
            KwDecorRuns.Along(grid, 0, k => grid.Cells[k].Has(KwCellMark.Paint) ? 0 : -1, rx, px);
            KwDecorRuns.Along(grid, 1, k => grid.Cells[k].Has(KwCellMark.Paint) ? 0 : -1, rz, pz);
            for (int k = 0; k < count; k++)
            {
                if (grid.Cells[k].Has(KwCellMark.Paint) && Mathf.Min(rx[k], rz[k]) * Step <= PathWidth)
                    grid.Cells[k].Marks |= KwCellMark.Path;
            }
        }

        // Distance to water from every cell, the way to it, and the shore.
        private static void Water(KwDecorGrid grid, sbyte[] toward)
        {
            List<int> seeds = new List<int>();
            for (int k = 0; k < grid.Cells.Length; k++)
            {
                if (grid.Cells[k].Kind == KwCellKind.Water)
                    seeds.Add(k);
            }
            int[] steps = KwDecorRuns.Spread(grid, seeds, n => true, Mathf.RoundToInt(Far / Step), toward);
            for (int k = 0; k < steps.Length; k++)
                Shore(grid, k, steps[k], toward[k]);
        }

        private static void Shore(KwDecorGrid grid, int k, int steps, sbyte toward)
        {
            ref KwCell cell = ref grid.Cells[k];
            cell.Water = steps >= 0 ? steps * Step : Far;
            if (cell.Kind == KwCellKind.Ground && steps >= 0 && steps * Step <= ShoreReach)
            {
                cell.Marks |= KwCellMark.Shore;
                cell.Out = cell.Out >= 0 ? cell.Out : toward;
            }
            else if (cell.Kind == KwCellKind.Water && cell.Depth <= Waterline && BesideDry(grid, k, out int dry))
            {
                cell.Marks |= KwCellMark.Shore;
                cell.Out = (sbyte)KwDecorGrid.Back(dry);
            }
        }

        private static bool BesideDry(KwDecorGrid grid, int k, out int dir)
        {
            for (dir = 0; dir < 4; dir++)
            {
                int next = grid.Next(k, dir);
                if (next >= 0 && grid.Cells[next].Kind == KwCellKind.Ground)
                    return true;
            }
            return false;
        }

        // A wall beside a cell gives it its way, unless a floor's edge or the water already did (a dock's band keeps its own).
        private static void Walls(KwDecorGrid grid, List<KwWallSpot> walls)
        {
            foreach (KwWallSpot spot in walls)
            {
                ref KwCell cell = ref grid.Cells[spot.Cell];
                bool free = cell.Kind == KwCellKind.Ground ? !cell.Has(KwCellMark.Shore) : cell.Has(KwCellMark.Open) || cell.Has(KwCellMark.Path);
                if (cell.Out >= 0 && !free)
                    continue;
                cell.Out = (sbyte)grid.DirOf(-spot.Normal);
            }
        }
    }
}
