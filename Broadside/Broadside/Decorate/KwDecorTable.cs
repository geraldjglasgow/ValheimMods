using System;

namespace Broadside
{
    /// <summary>
    /// A summed-area table over the read area's cells: how many cells of a rectangle fail a test, in four lookups whatever
    /// its size, so a scene's every footprint can be tried at thousands of places cheaply (<see cref="KwDecorSites"/>).
    /// Rectangles are cell ranges, the end not included. Plain arithmetic.
    /// </summary>
    public sealed class KwDecorTable
    {
        private readonly int n;
        private readonly int[] sums;

        public KwDecorTable(KwDecorGrid grid, Func<int, bool> bad)
        {
            n = grid.N;
            sums = new int[(n + 1) * (n + 1)];
            for (int j = 0; j < n; j++)
            {
                int row = 0;
                for (int i = 0; i < n; i++)
                {
                    row += bad(grid.Index(i, j)) ? 1 : 0;
                    sums[(j + 1) * (n + 1) + i + 1] = sums[j * (n + 1) + i + 1] + row;
                }
            }
        }

        /// <summary>The failing cells in the rectangle, clamped to the grid.</summary>
        public int Count(int i0, int j0, int i1, int j1)
        {
            (i0, j0, i1, j1) = (Math.Max(0, i0), Math.Max(0, j0), Math.Min(n, i1), Math.Min(n, j1));
            if (i1 <= i0 || j1 <= j0)
                return 0;
            int w = n + 1;
            return sums[j1 * w + i1] - sums[j0 * w + i1] - sums[j1 * w + i0] + sums[j0 * w + i0];
        }

        /// <summary>No failing cell in the rectangle; with <paramref name="inside"/> it must also lie wholly in the grid.</summary>
        public bool Clear(int i0, int j0, int i1, int j1, bool inside)
        {
            if (inside && (i0 < 0 || j0 < 0 || i1 > n || j1 > n))
                return false;
            return Count(i0, j0, i1, j1) == 0;
        }
    }
}
