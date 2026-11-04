using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A straight run of a dock's edge (<see cref="KwDecorEdges"/>): its floor, the way out over the edge, the way along
    /// it, its cells in order, whether it faces the water and whether it is a pier's head (the dock's end) rather than its
    /// side. Distances along it run from its first cell's outer side.
    /// </summary>
    public sealed class KwEdgeRun
    {
        public KwEdgeRun(short region, int outDir, int along, bool water, bool tip, List<int> cells)
        {
            (Region, Out, Along, Water, Tip, Cells) = (region, outDir, along, water, tip, cells);
        }

        /// <summary>The dock's usual width across the run (the middle of its cells' widths, so a branch joining it is not counted).</summary>
        public float Width { get; private set; }

        public short Region { get; }
        public int Out { get; }
        public int Along { get; }
        public bool Water { get; }
        public bool Tip { get; }
        public List<int> Cells { get; }

        public float Length => Cells.Count * KwDecorGrid.Step;

        /// <summary>The cell a distance along lies in.</summary>
        public int CellAt(float t) => Cells[Mathf.Clamp(Mathf.FloorToInt(t / KwDecorGrid.Step), 0, Cells.Count - 1)];

        /// <summary>Works out <see cref="Width"/>.</summary>
        public KwEdgeRun Measured(KwDecorGrid grid)
        {
            List<float> widths = Cells.ConvertAll(k => Across(grid, k));
            widths.Sort();
            Width = widths[widths.Count / 2];
            return this;
        }

        /// <summary>The dock's width across at one of its cells: from it inward, away from the edge, while the floor goes on.</summary>
        public float Across(KwDecorGrid grid, int k)
        {
            int back = KwDecorGrid.Back(Out), cells = 0;
            for (int at = k; at >= 0 && grid.Cells[at].Region == Region; at = grid.Next(at, back))
                cells++;
            return cells * KwDecorGrid.Step;
        }

        /// <summary>The point a distance along, on the cells' middle line, at the deck's height.</summary>
        public Vector3 PointAt(KwDecorGrid grid, float t)
        {
            int n = Mathf.Clamp(Mathf.FloorToInt(t / KwDecorGrid.Step), 0, Cells.Count - 1);
            return grid.On(Cells[n]) + grid.Dir(Along) * (t - (n + 0.5f) * KwDecorGrid.Step);
        }
    }

    /// <summary>
    /// The docks' edges as straight runs (DECORATE.md, "The quay"): a dock's outer row of band cells (its edge, at a drop
    /// or a wall) kept clear of nothing, grouped by floor, by the way out and by row, split where a gap, an access or a
    /// step in height breaks it. A run whose cells mostly face the water is a water run; one whose cells mostly point to
    /// the dock's end is a pier's head. Plain arithmetic on the reading.
    /// </summary>
    public static class KwDecorEdges
    {
        private const KwCellMark Kept = KwCellMark.Clear | KwCellMark.Access | KwCellMark.Lane | KwCellMark.Path;
        private const float Level = 0.15f;

        public static List<KwEdgeRun> Runs(KwDecorArea area)
        {
            KwDecorGrid grid = area.Grid;
            Dictionary<long, List<int>> lines = new Dictionary<long, List<int>>();
            for (int k = 0; k < grid.Cells.Length; k++)
            {
                KwCell cell = grid.Cells[k];
                if (cell.Kind != KwCellKind.Deck || !cell.Has(KwCellMark.Edge) || !cell.Has(KwCellMark.Band) || cell.Out < 0 || cell.Has(Kept))
                    continue;
                long key = ((long)cell.Region << 24) | ((long)cell.Out << 20) | (long)(cell.Out % 2 == 0 ? k % grid.N : k / grid.N);
                if (!lines.TryGetValue(key, out List<int> line))
                    lines[key] = line = new List<int>();
                line.Add(k);
            }
            List<KwEdgeRun> runs = new List<KwEdgeRun>();
            foreach (List<int> line in lines.Values)
                Split(grid, line, runs);
            return runs;
        }

        // A row's cells (in index order, so in order along it) cut where a cell is missing or the deck steps.
        private static void Split(KwDecorGrid grid, List<int> line, List<KwEdgeRun> runs)
        {
            int outDir = grid.Cells[line[0]].Out, along = outDir % 2 == 0 ? 1 : 0, step = along == 1 ? grid.N : 1;
            List<int> run = new List<int> { line[0] };
            for (int n = 1; n <= line.Count; n++)
            {
                bool joins = n < line.Count && line[n] - line[n - 1] == step && Mathf.Abs(grid.Cells[line[n]].Y - grid.Cells[line[n - 1]].Y) <= Level;
                if (joins)
                {
                    run.Add(line[n]);
                    continue;
                }
                runs.Add(Make(grid, run, outDir, along));
                if (n < line.Count)
                    run = new List<int> { line[n] };
            }
        }

        private static KwEdgeRun Make(KwDecorGrid grid, List<int> cells, int outDir, int along)
        {
            int water = cells.FindAll(k => grid.Cells[k].Has(KwCellMark.WaterEdge)).Count, tip = cells.FindAll(k => grid.Cells[k].Has(KwCellMark.Tip)).Count;
            return new KwEdgeRun(grid.Cells[cells[0]].Region, outDir, along, water * 2 > cells.Count, tip * 2 > cells.Count, cells).Measured(grid);
        }
    }
}
