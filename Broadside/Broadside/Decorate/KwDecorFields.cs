using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the layout asks of an area again and again, worked out once its cells are read (DECORATE.md, "Scenes"): every
    /// cell's steps to the nearest path (a painted path, a door's side, a ladder's foot, a station's front, a dock's entry;
    /// 12 m at most) and the way there, its steps to the nearest wall face (6 m at most), the yard's heart (the dry dock,
    /// else a ship or workbench, else the middle of the open ground), the ways into the yard (where docks are stepped onto
    /// and painted paths cross the area's side; with neither, the path ends: doors, ladders, stations), the walkable
    /// heights' range, and four summed tables: where a standing piece may
    /// stand (dry level ground or a wide floor's open middle, nothing kept clear), the same on the land alone, where people
    /// walk, and where people walk or the water is. Plain arithmetic.
    /// </summary>
    public sealed class KwDecorFields
    {
        private const int PathReach = 24, WallReach = 12;
        private const float Level = 0.94f, Walkable = 0.8f;
        private const KwCellMark Kept = KwCellMark.Clear | KwCellMark.Path | KwCellMark.Lane | KwCellMark.Access;

        public KwDecorFields(KwDecorArea area)
        {
            KwDecorGrid grid = area.Grid;
            PathWay = Ways(grid.Cells.Length);
            PathSteps = KwDecorRuns.Spread(grid, PathSeeds(area), n => true, PathReach, PathWay);
            WallSteps = KwDecorRuns.Spread(grid, Marked(grid, KwCellMark.Wall), n => true, WallReach);
            Ground = new KwDecorTable(grid, k => !Standable(grid.Cells[k]));
            Land = new KwDecorTable(grid, k => !Standable(grid.Cells[k]) || grid.Cells[k].Kind != KwCellKind.Ground);
            Walk = new KwDecorTable(grid, k => !Walks(grid.Cells[k]));
            WalkWet = new KwDecorTable(grid, k => !Walks(grid.Cells[k]) && grid.Cells[k].Kind != KwCellKind.Water);
            Heart = FindHeart(area);
            Entrances = FindEntrances(area);
            (Low, High) = Heights(grid);
        }

        /// <summary>Steps (half metres) to the nearest path or path end; -1 farther than 12 m.</summary>
        public int[] PathSteps { get; }

        /// <summary>The grid direction toward that path; -1 on it or with none near.</summary>
        public sbyte[] PathWay { get; }

        /// <summary>Steps to the nearest cell with a wall face beside it; -1 farther than 6 m.</summary>
        public int[] WallSteps { get; }

        /// <summary>A standing piece may stand here (dry level ground or a wide floor's open middle, nothing kept clear): failing cells counted.</summary>
        public KwDecorTable Ground { get; }

        /// <summary>As <see cref="Ground"/>, on the land itself (no floor): failing cells counted.</summary>
        public KwDecorTable Land { get; }

        /// <summary>People walk here (ground not too steep, any floor): failing cells counted.</summary>
        public KwDecorTable Walk { get; }

        /// <summary>People walk here or it is water: failing cells counted.</summary>
        public KwDecorTable WalkWet { get; }

        /// <summary>The yard's heart: the dry dock, else the first ship or workbench, else the open ground's middle.</summary>
        public Vector3 Heart { get; }

        /// <summary>The ways into the yard: dock entries and painted paths at the area's side (else doors, ladders, stations).</summary>
        public List<Vector3> Entrances { get; }

        /// <summary>The lowest and highest walkable surface.</summary>
        public float Low { get; }
        public float High { get; }

        public static bool Standable(KwCell cell) =>
            ((cell.Kind == KwCellKind.Ground && cell.Up >= Level) || (cell.Kind == KwCellKind.Deck && cell.Has(KwCellMark.Open))) && !cell.Has(Kept);

        public static bool Walks(KwCell cell) => (cell.Kind == KwCellKind.Ground && cell.Up >= Walkable) || cell.Kind == KwCellKind.Deck;

        private static sbyte[] Ways(int count)
        {
            sbyte[] ways = new sbyte[count];
            for (int k = 0; k < count; k++)
                ways[k] = -1;
            return ways;
        }

        // Painted path cells, and the cells under each path end.
        private static List<int> PathSeeds(KwDecorArea area)
        {
            List<int> seeds = Marked(area.Grid, KwCellMark.Path);
            foreach (Vector3 end in area.Ways)
                Seed(area.Grid, end, seeds);
            foreach (Vector3 end in area.Entries)
                Seed(area.Grid, end, seeds);
            return seeds;
        }

        private static void Seed(KwDecorGrid grid, Vector3 at, List<int> seeds)
        {
            if (grid.CellOf(at, out int k) && !seeds.Contains(k))
                seeds.Add(k);
        }

        private static List<int> Marked(KwDecorGrid grid, KwCellMark mark)
        {
            List<int> found = new List<int>();
            for (int k = 0; k < grid.Cells.Length; k++)
            {
                if (grid.Cells[k].Has(mark))
                    found.Add(k);
            }
            return found;
        }

        private static Vector3 FindHeart(KwDecorArea area)
        {
            if (area.DryDock is KwDecorBox dock)
                return dock.Centre;
            if (area.Yards.Count > 0)
                return area.Yards[0];
            (Vector3 sum, int count) = (Vector3.zero, 0);
            for (int k = 0; k < area.Grid.Cells.Length; k++)
            {
                if (Standable(area.Grid.Cells[k]))
                    (sum, count) = (sum + area.Grid.On(k), count + 1);
            }
            return count > 0 ? sum / count : area.Grid.Centre;
        }

        // Dock entries and every fourth painted path cell on the area's outer ring; with none, the path ends.
        private static List<Vector3> FindEntrances(KwDecorArea area)
        {
            KwDecorGrid grid = area.Grid;
            List<Vector3> found = new List<Vector3>(area.Entries);
            for (int t = 0; t < grid.N; t += 4)
            {
                foreach (int k in new[] { grid.Index(t, 0), grid.Index(t, grid.N - 1), grid.Index(0, t), grid.Index(grid.N - 1, t) })
                {
                    if (grid.Cells[k].Has(KwCellMark.Path))
                        found.Add(grid.On(k));
                }
            }
            return found.Count > 0 ? found : new List<Vector3>(area.Ways);
        }

        private static (float, float) Heights(KwDecorGrid grid)
        {
            (float low, float high) = (float.MaxValue, float.MinValue);
            foreach (KwCell cell in grid.Cells)
            {
                if (Walks(cell))
                    (low, high) = (Mathf.Min(low, cell.Y), Mathf.Max(high, cell.Y));
            }
            return low <= high ? (low, high) : (grid.Centre.y, grid.Centre.y);
        }
    }
}
