using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The quarter-metre plan grid smart select works on: it covers every piece's footprint plus a free margin (so the
    /// outside is one connected ring), marks footprints into cell flags and shrinks a flagged area (erosion).
    /// Cells are numbered row by row from the front (<see cref="Z0"/>), each row from <see cref="X0"/> rightwards.
    /// </summary>
    internal sealed class SmartGrid
    {
        public const float Cell = 0.25f;
        private const float Margin = 2f;

        public readonly float X0;
        public readonly float Z0;
        public readonly int Width;
        public readonly int Depth;

        public SmartGrid(SmartBox[] boxes)
        {
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (SmartBox box in boxes)
            {
                float sx = box.Spread(true), sz = box.Spread(false);
                minX = Mathf.Min(minX, box.X - sx);
                maxX = Mathf.Max(maxX, box.X + sx);
                minZ = Mathf.Min(minZ, box.Z - sz);
                maxZ = Mathf.Max(maxZ, box.Z + sz);
            }
            X0 = minX - Margin;
            Z0 = minZ - Margin;
            Width = Mathf.CeilToInt((maxX - minX + 2f * Margin) / Cell) + 1;
            Depth = Mathf.CeilToInt((maxZ - minZ + 2f * Margin) / Cell) + 1;
        }

        public int Count => Width * Depth;

        /// <summary>The cell holding a frame point, or -1 off the grid.</summary>
        public int Index(float x, float z)
        {
            int ix = Mathf.FloorToInt((x - X0) / Cell), iz = Mathf.FloorToInt((z - Z0) / Cell);
            return ix >= 0 && iz >= 0 && ix < Width && iz < Depth ? iz * Width + ix : -1;
        }

        public float CentreX(int cell) => X0 + (cell % Width + 0.5f) * Cell;

        public float CentreZ(int cell) => Z0 + (cell / Width + 0.5f) * Cell;

        /// <summary>Sets <paramref name="flag"/> on every cell whose centre lies in the box's footprint grown by <paramref name="grow"/>.</summary>
        public void Mark(byte[] cells, SmartBox box, float grow, byte flag)
        {
            float sx = box.Spread(true) + grow, sz = box.Spread(false) + grow;
            int minX = Mathf.Max(0, Mathf.FloorToInt((box.X - sx - X0) / Cell)), maxX = Mathf.Min(Width - 1, Mathf.FloorToInt((box.X + sx - X0) / Cell));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((box.Z - sz - Z0) / Cell)), maxZ = Mathf.Min(Depth - 1, Mathf.FloorToInt((box.Z + sz - Z0) / Cell));
            for (int iz = minZ; iz <= maxZ; iz++)
            {
                float z = Z0 + (iz + 0.5f) * Cell;
                for (int ix = minX; ix <= maxX; ix++)
                {
                    if (box.Covers(X0 + (ix + 0.5f) * Cell, z, grow))
                        cells[iz * Width + ix] |= flag;
                }
            }
        }

        /// <summary>Clears <paramref name="flag"/> from every cell closer than <paramref name="steps"/> cells (square distance) to a cell without it.</summary>
        public void Erode(byte[] cells, byte flag, int steps)
        {
            bool[] rows = new bool[cells.Length];
            int[] run = new int[Mathf.Max(Width, Depth)];
            for (int iz = 0; iz < Depth; iz++)
                ErodeRow(cells, flag, rows, iz * Width, steps, run);
            for (int ix = 0; ix < Width; ix++)
                ErodeColumn(rows, cells, flag, ix, steps, run);
        }

        /// <summary>One row: a cell stays when the <paramref name="steps"/> cells before and after it are flagged too.</summary>
        private void ErodeRow(byte[] cells, byte flag, bool[] rows, int start, int steps, int[] run)
        {
            int n = 0;
            for (int i = 0; i < Width; i++)
            {
                n = (cells[start + i] & flag) != 0 ? n + 1 : 0;
                run[i] = n;
            }
            n = 0;
            for (int i = Width - 1; i >= 0; i--)
            {
                n = (cells[start + i] & flag) != 0 ? n + 1 : 0;
                rows[start + i] = run[i] > steps && n > steps;
            }
        }

        /// <summary>One column of the row-eroded cells: the flag is cleared where the cells above or below fall short.</summary>
        private void ErodeColumn(bool[] rows, byte[] cells, byte flag, int column, int steps, int[] run)
        {
            int n = 0;
            for (int i = 0; i < Depth; i++)
            {
                n = rows[column + i * Width] ? n + 1 : 0;
                run[i] = n;
            }
            n = 0;
            for (int i = Depth - 1; i >= 0; i--)
            {
                int cell = column + i * Width;
                n = rows[cell] ? n + 1 : 0;
                if (run[i] <= steps || n <= steps)
                    cells[cell] &= (byte)~flag;
            }
        }
    }
}
