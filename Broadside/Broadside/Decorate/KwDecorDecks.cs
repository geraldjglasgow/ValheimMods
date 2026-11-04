using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The floors in the read area (DECORATE.md, "Reading the area"). Floor cells joined across steps of at most 0.35 m are
    /// one floor; one smaller than 3 m² is something standing (a table's top). A floor mostly stepped onto from round it
    /// (flush with the ground, or walled in) is open: decorated as ground, a narrow one (4.5 m or less across) a path kept
    /// clear. Any other floor is a dock or walkway: across its width (the shorter of its two runs through a cell) its middle
    /// is a lane kept clear, at least 1.6 m, or all but its outer 0.5 m rows when narrow; its outer 1.75 m either side is its
    /// edge band; a wide one's middle beyond that is open; its last 1.5 m before a drop at its end (a pier's end) is band, not
    /// lane; within 1.5 m of where it is stepped onto from the land or another floor is its access, kept clear. Each band cell
    /// points (<see cref="KwCell.Out"/>) to its nearer side, or to the end at a pier's end (a corner to the end, so a pier head
    /// is its whole width, <see cref="KwCellMark.Tip"/>); the outer row at a drop or wall is an edge, at water a water edge.
    /// </summary>
    public static class KwDecorDecks
    {
        private const float Rise = 0.35f, StepOn = 0.6f, LaneHalf = 0.8f, EdgeRow = 0.5f, Band = 1.75f, OpenRun = 8f;
        private const float TipReach = 1.5f, AccessReach = 1.5f, FlushPath = 4.5f, Step = KwDecorGrid.Step;
        private const int MinCells = 12;

        private enum Past { Same, Drop, WaterDrop, Step, Neutral, Side }

        public static void Read(KwDecorGrid grid, List<Vector3> entries)
        {
            List<int> sizes = Regions(grid);
            bool[] flush = Flush(grid, sizes.Count);
            int count = grid.Cells.Length;
            int[] rx = new int[count], px = new int[count], rz = new int[count], pz = new int[count];
            KwDecorRuns.Along(grid, 0, k => grid.Cells[k].Region, rx, px);
            KwDecorRuns.Along(grid, 1, k => grid.Cells[k].Region, rz, pz);
            for (int k = 0; k < count; k++)
            {
                short region = grid.Cells[k].Region;
                if (region >= 0 && flush[region])
                    grid.Cells[k].Marks |= Boardwalk(rx[k], rz[k]) ? KwCellMark.Path : KwCellMark.Open;
                else if (region >= 0)
                    Shape(grid, k, rx[k], px[k], rz[k], pz[k]);
            }
            Edges(grid);
            Access(grid, flush, entries);
        }

        // An open floor's strip 4.5 m across or less and at least two and a half times as long: a boardwalk, kept clear.
        private static bool Boardwalk(int rx, int rz) => Mathf.Min(rx, rz) * Step <= FlushPath && Mathf.Max(rx, rz) >= 2.5f * Mathf.Min(rx, rz);

        private static bool Floored(KwCell cell) => cell.Kind == KwCellKind.Deck || cell.Has(KwCellMark.Cover);

        // Floors joined up; the region ids' cell counts. Too small a floor is something standing.
        private static List<int> Regions(KwDecorGrid grid)
        {
            List<int> sizes = new List<int>();
            for (int k = 0; k < grid.Cells.Length; k++)
            {
                if (Floored(grid.Cells[k]) && grid.Cells[k].Region < 0)
                    sizes.Add(Fill(grid, k, (short)sizes.Count));
            }
            for (int k = 0; k < grid.Cells.Length; k++)
            {
                ref KwCell cell = ref grid.Cells[k];
                if (cell.Region < 0 || sizes[cell.Region] >= MinCells)
                    continue;
                cell.Region = -1;
                if (cell.Kind == KwCellKind.Deck)
                    cell.Kind = KwCellKind.Blocked;
            }
            return sizes;
        }

        private static int Fill(KwDecorGrid grid, int seed, short id)
        {
            Stack<int> open = new Stack<int>();
            (grid.Cells[seed].Region, int count) = (id, 0);
            open.Push(seed);
            while (open.Count > 0)
            {
                int k = open.Pop();
                count += grid.Cells[k].Kind == KwCellKind.Deck ? 1 : 0;
                for (int dir = 0; dir < 4; dir++)
                {
                    int next = grid.Next(k, dir);
                    if (next < 0 || grid.Cells[next].Region >= 0 || !Floored(grid.Cells[next]) || Mathf.Abs(grid.Cells[next].Y - grid.Cells[k].Y) > Rise)
                        continue;
                    grid.Cells[next].Region = id;
                    open.Push(next);
                }
            }
            return count;
        }

        // A floor with no drop round it, or stepped onto more than it drops, is open (flush with the ground, walled in).
        private static bool[] Flush(KwDecorGrid grid, int regions)
        {
            int[] drops = new int[regions], steps = new int[regions];
            for (int k = 0; k < grid.Cells.Length; k++)
            {
                short region = grid.Cells[k].Region;
                for (int dir = 0; region >= 0 && dir < 4; dir++)
                {
                    Past past = Beyond(grid, k, dir);
                    drops[region] += past == Past.Drop || past == Past.WaterDrop ? 1 : 0;
                    steps[region] += past == Past.Step ? 1 : 0;
                }
            }
            bool[] flush = new bool[regions];
            for (int r = 0; r < regions; r++)
                flush[r] = drops[r] == 0 || steps[r] > drops[r];
            return flush;
        }

        // A dock's cell: its lane, band or open middle, and the way to its nearer side (or to its end, near a drop there).
        private static void Shape(KwDecorGrid grid, int k, int rx, int px, int rz, int pz)
        {
            bool acrossX = rx <= rz;
            (int run, int pos, int runL, int posL) = acrossX ? (rx, px, rz, pz) : (rz, pz, rx, px);
            bool low = pos <= run - 1 - pos, lowL = posL <= runL - 1 - posL;
            int outDir = acrossX ? (low ? 2 : 0) : (low ? 3 : 1), tipDir = acrossX ? (lowL ? 3 : 1) : (lowL ? 2 : 0);
            float side = (Mathf.Min(pos, run - 1 - pos) + 0.5f) * Step;
            float tip = Tip(grid, k, tipDir, Mathf.Min(posL, runL - 1 - posL));
            ref KwCell cell = ref grid.Cells[k];
            cell.Out = (sbyte)(tip <= side ? tipDir : outDir);
            cell.Marks |= tip <= side ? KwCellMark.Tip : KwCellMark.None;
            float laneFrom = Mathf.Max(run * Step / 2f - LaneHalf, EdgeRow);
            if (side > laneFrom && tip > TipReach)
                cell.Marks |= KwCellMark.Lane;
            else if (Mathf.Min(side, tip) <= Band)
                cell.Marks |= KwCellMark.Band;
            else if (run * Step >= OpenRun && runL * Step >= OpenRun)
                cell.Marks |= KwCellMark.Open;
        }

        // Metres to the floor's end along its length when that end is a drop (a pier's end); far otherwise.
        private static float Tip(KwDecorGrid grid, int k, int dir, int cells)
        {
            int end = k;
            for (int s = 0; s < cells && end >= 0; s++)
                end = grid.Next(end, dir);
            Past past = end >= 0 ? Beyond(grid, end, dir) : Past.Side;
            return past == Past.Drop || past == Past.WaterDrop ? (cells + 0.5f) * Step : float.MaxValue;
        }

        // The outer row of a band at a drop or a wall: an edge; at water, a water edge and the shore.
        private static void Edges(KwDecorGrid grid)
        {
            for (int k = 0; k < grid.Cells.Length; k++)
            {
                ref KwCell cell = ref grid.Cells[k];
                if (cell.Kind != KwCellKind.Deck || cell.Out < 0 || !cell.Has(KwCellMark.Band))
                    continue;
                Past past = Beyond(grid, k, cell.Out);
                if (past == Past.Drop || past == Past.WaterDrop || past == Past.Neutral)
                    cell.Marks |= KwCellMark.Edge;
                if (past == Past.WaterDrop)
                    cell.Marks |= KwCellMark.WaterEdge | KwCellMark.Shore;
            }
        }

        // Within 1.5 m of where a dock is stepped onto: kept clear; each dock's way on noted (for the paths between).
        private static void Access(KwDecorGrid grid, bool[] flush, List<Vector3> entries)
        {
            List<int> seeds = new List<int>();
            Dictionary<short, (Vector3 Sum, int Count)> ons = new Dictionary<short, (Vector3, int)>();
            for (int k = 0; k < grid.Cells.Length; k++)
            {
                short region = grid.Cells[k].Region;
                if (region < 0 || flush[region] || !SteppedOn(grid, k))
                    continue;
                seeds.Add(k);
                (Vector3 sum, int count) = ons.TryGetValue(region, out var had) ? had : (Vector3.zero, 0);
                ons[region] = (sum + grid.On(k), count + 1);
            }
            int[] steps = KwDecorRuns.Spread(grid, seeds, n => grid.Cells[n].Walkable, Mathf.RoundToInt(AccessReach / Step));
            for (int k = 0; k < steps.Length; k++)
                grid.Cells[k].Marks |= steps[k] >= 0 ? KwCellMark.Access : KwCellMark.None;
            foreach ((Vector3 sum, int count) in ons.Values)
                entries.Add(sum / count);
        }

        private static bool SteppedOn(KwDecorGrid grid, int k)
        {
            for (int dir = 0; dir < 4; dir++)
            {
                if (Beyond(grid, k, dir) == Past.Step)
                    return true;
            }
            return false;
        }

        // What lies past a floor cell one way.
        private static Past Beyond(KwDecorGrid grid, int k, int dir)
        {
            int next = grid.Next(k, dir);
            if (next < 0)
                return Past.Side;
            KwCell here = grid.Cells[k], there = grid.Cells[next];
            if (there.Region == here.Region)
                return Past.Same;
            if (there.Kind == KwCellKind.Water)
                return Past.WaterDrop;
            if (there.Kind == KwCellKind.None)
                return Past.Drop;
            float rise = there.Y - here.Y;
            if (there.Kind == KwCellKind.Blocked)
                return rise < -StepOn ? Past.Drop : Past.Neutral;
            if (Mathf.Abs(rise) <= StepOn)
                return Past.Step;
            return rise < 0f ? Past.Drop : Past.Neutral;
        }
    }
}
