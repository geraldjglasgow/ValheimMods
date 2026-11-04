using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// Runs of cells along the grid's rows or columns (axis 0 along x, 1 along z): for each cell the length of the unbroken
    /// run of cells sharing its key (a floor region, painted ground), and its place in that run; 0 for a cell whose key is
    /// negative. A floor's width across is the shorter of its two runs. Plus a breadth-first spread from seed cells
    /// (<see cref="Spread"/>). Plain arithmetic.
    /// </summary>
    public static class KwDecorRuns
    {
        /// <summary>Every cell's run and place along an axis.</summary>
        public static void Along(KwDecorGrid grid, int axis, Func<int, int> key, int[] run, int[] pos)
        {
            for (int line = 0; line < grid.N; line++)
                Line(grid, axis, line, key, run, pos);
        }

        private static void Line(KwDecorGrid grid, int axis, int line, Func<int, int> key, int[] run, int[] pos)
        {
            int start = 0, n = grid.N;
            for (int t = 1; t <= n; t++)
            {
                if (t < n && key(Cell(grid, axis, line, t)) == key(Cell(grid, axis, line, start)))
                    continue;
                bool kept = key(Cell(grid, axis, line, start)) >= 0;
                for (int s = start; s < t; s++)
                {
                    int k = Cell(grid, axis, line, s);
                    (run[k], pos[k]) = kept ? (t - start, s - start) : (0, 0);
                }
                start = t;
            }
        }

        private static int Cell(KwDecorGrid grid, int axis, int line, int t) => axis == 0 ? grid.Index(t, line) : grid.Index(line, t);

        /// <summary>
        /// Steps from the nearest seed (0 on a seed) over the cells a test lets through, at most a number of steps; -1
        /// where it never came. The way each cell was reached from (toward its seed) goes into <paramref name="toward"/>.
        /// </summary>
        public static int[] Spread(KwDecorGrid grid, List<int> seeds, Func<int, bool> through, int most, sbyte[]? toward = null)
        {
            int[] steps = new int[grid.Cells.Length];
            for (int k = 0; k < steps.Length; k++)
                steps[k] = -1;
            Queue<int> queue = new Queue<int>();
            foreach (int seed in seeds)
            {
                steps[seed] = 0;
                queue.Enqueue(seed);
            }
            while (queue.Count > 0)
                Visit(grid, queue, steps, queue.Dequeue(), through, most, toward);
            return steps;
        }

        private static void Visit(KwDecorGrid grid, Queue<int> queue, int[] steps, int k, Func<int, bool> through, int most, sbyte[]? toward)
        {
            if (steps[k] >= most)
                return;
            for (int dir = 0; dir < 4; dir++)
            {
                int next = grid.Next(k, dir);
                if (next < 0 || steps[next] >= 0 || !through(next))
                    continue;
                steps[next] = steps[k] + 1;
                if (toward != null)
                    toward[next] = toward[k] >= 0 ? toward[k] : (sbyte)KwDecorGrid.Back(dir);
                queue.Enqueue(next);
            }
        }
    }
}
