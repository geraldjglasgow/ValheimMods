using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The rooms of one storey's plan. Every cell is open ground, wall (anything standing in the storey's height) or
    /// roofed (under a roof, walls included). The outside is the open ground reached from the grid's edge without
    /// crossing a wall or passing under a roof. A roofed area (one connected stretch under roofs) is a room; so is an
    /// enclosed open area (a pen, a courtyard) the outside cannot reach, unless it is bigger than
    /// <see cref="MaxYard"/> (a field, a walled village) or smaller than <see cref="MinYard"/> (a pocket behind a stair).
    /// An enclosed open area joins the roofed room it shares the longest border with (a pen and its lean-to, a courtyard
    /// and its house), so each building is one room; then each room takes the walls and pockets round it (<see cref="Shell"/>).
    /// </summary>
    internal sealed class SmartRooms
    {
        public const byte Open = 0;
        public const byte Wall = 1;
        public const byte Roofed = 2;

        private const float MaxYard = 1500f;
        private const float MinYard = 4f;
        private const float MinRoom = 2f;
        private const int ShellSteps = 12;
        private const int Pocket = -2;
        private const float CellArea = SmartGrid.Cell * SmartGrid.Cell;

        /// <summary>Per cell: the room it belongs to (1 and up, its walls included), 0 for any other wall, -1 for the outside, -2 for a pocket.</summary>
        public readonly int[] Labels;

        private readonly SmartGrid grid;
        private readonly byte[] kinds;
        private readonly int[] queue;
        private readonly List<int> sizes = new List<int> { 0 };
        private readonly List<bool> yards = new List<bool> { false };

        public SmartRooms(SmartGrid grid, byte[] kinds)
        {
            this.grid = grid;
            this.kinds = kinds;
            Labels = new int[kinds.Length];
            queue = new int[kinds.Length];
            MarkOutside();
            LabelAreas();
            Merge(YardOwners());
            Shell();
        }

        /// <summary>The highest room id (ids run from 1; some may be unused after merging).</summary>
        public int Count => sizes.Count - 1;

        /// <summary>
        /// Gives each room the walls round it: wall cells outside every roof, joined to the room through other wall cells
        /// or pockets (eight neighbours), up to <see cref="ShellSteps"/> cells deep, nearest room first (a thick stone
        /// wall, a fence, the corner a winding stair closes off).
        /// </summary>
        private void Shell()
        {
            int tail = 0, head = 0;
            for (int cell = 0; cell < Labels.Length; cell++)
            {
                if (Labels[cell] > 0)
                    queue[tail++] = cell;
            }
            for (int step = 0; step < ShellSteps && head < tail; step++)
            {
                for (int end = tail; head < end; head++)
                    tail = SpreadShell(queue[head], tail);
            }
        }

        private int SpreadShell(int cell, int tail)
        {
            int x = cell % grid.Width, z = cell / grid.Width;
            for (int nz = Mathf.Max(0, z - 1); nz <= Mathf.Min(grid.Depth - 1, z + 1); nz++)
            {
                for (int nx = Mathf.Max(0, x - 1); nx <= Mathf.Min(grid.Width - 1, x + 1); nx++)
                {
                    int next = nz * grid.Width + nx;
                    if (Labels[next] != Pocket && (Labels[next] != 0 || kinds[next] != Wall))
                        continue;
                    Labels[next] = Labels[cell];
                    queue[tail++] = next;
                }
            }
            return tail;
        }

        /// <summary>Floods the open ground from every open cell on the grid's edge.</summary>
        private void MarkOutside()
        {
            for (int ix = 0; ix < grid.Width; ix++)
            {
                FloodOutside(ix);
                FloodOutside((grid.Depth - 1) * grid.Width + ix);
            }
            for (int iz = 0; iz < grid.Depth; iz++)
            {
                FloodOutside(iz * grid.Width);
                FloodOutside(iz * grid.Width + grid.Width - 1);
            }
        }

        private void FloodOutside(int cell)
        {
            if (Labels[cell] == 0 && kinds[cell] == Open)
                Flood(cell, -1);
        }

        /// <summary>Numbers every roofed area and every enclosed open area; too big an open area is outside, too small a pocket.</summary>
        private void LabelAreas()
        {
            for (int cell = 0; cell < kinds.Length; cell++)
            {
                if (Labels[cell] != 0 || kinds[cell] == Wall)
                    continue;
                int count = Flood(cell, sizes.Count);
                float area = count * CellArea;
                if (kinds[cell] == Open && (area > MaxYard || area < MinYard))
                {
                    for (int i = 0; i < count; i++)
                        Labels[queue[i]] = area > MaxYard ? -1 : Pocket;
                    continue;
                }
                sizes.Add(count);
                yards.Add(kinds[cell] == Open);
            }
        }

        /// <summary>Labels the cells of the start cell's kind connected to it (four neighbours); returns how many, listed in the queue.</summary>
        private int Flood(int start, int id)
        {
            byte kind = kinds[start];
            int head = 0, tail = 0, width = grid.Width;
            Labels[start] = id;
            queue[tail++] = start;
            while (head < tail)
            {
                int cell = queue[head++], x = cell % width;
                if (x > 0)
                    tail = Visit(cell - 1, kind, id, tail);
                if (x < width - 1)
                    tail = Visit(cell + 1, kind, id, tail);
                if (cell >= width)
                    tail = Visit(cell - width, kind, id, tail);
                if (cell + width < kinds.Length)
                    tail = Visit(cell + width, kind, id, tail);
            }
            return tail;
        }

        private int Visit(int cell, byte kind, int id, int tail)
        {
            if (Labels[cell] != 0 || kinds[cell] != kind)
                return tail;
            Labels[cell] = id;
            queue[tail] = cell;
            return tail + 1;
        }

        /// <summary>For every room id, the room it merges into: an enclosed open area into the roofed room it borders most, others into themselves.</summary>
        private int[] YardOwners()
        {
            Dictionary<long, int> shared = new Dictionary<long, int>();
            if (yards.Contains(true))
            {
                for (int cell = 0; cell < Labels.Length; cell++)
                {
                    if (Labels[cell] > 0 && yards[Labels[cell]])
                        CountBorders(shared, cell);
                }
            }
            return Strongest(shared);
        }

        /// <summary>Each room id's merge target from the counted borders: the roofed room a yard borders most, else itself.</summary>
        private int[] Strongest(Dictionary<long, int> shared)
        {
            int[] owner = new int[sizes.Count], most = new int[sizes.Count];
            for (int id = 0; id < owner.Length; id++)
                owner[id] = id;
            foreach (KeyValuePair<long, int> pair in shared)
            {
                int yard = (int)(pair.Key >> 32), roofed = (int)(pair.Key & 0xffffffff);
                if (pair.Value > most[yard])
                {
                    most[yard] = pair.Value;
                    owner[yard] = roofed;
                }
            }
            return owner;
        }

        /// <summary>Counts the border steps from one cell of an enclosed open area to roofed rooms (four neighbours).</summary>
        private void CountBorders(Dictionary<long, int> shared, int cell)
        {
            int x = cell % grid.Width;
            if (x > 0)
                CountShared(shared, cell, cell - 1);
            if (x < grid.Width - 1)
                CountShared(shared, cell, cell + 1);
            if (cell >= grid.Width)
                CountShared(shared, cell, cell - grid.Width);
            if (cell + grid.Width < Labels.Length)
                CountShared(shared, cell, cell + grid.Width);
        }

        /// <summary>Counts one border step between an enclosed open area (<paramref name="yardCell"/>) and a roofed room.</summary>
        private void CountShared(Dictionary<long, int> shared, int yardCell, int other)
        {
            int roofed = Labels[other];
            if (roofed <= 0 || yards[roofed])
                return;
            long key = ((long)Labels[yardCell] << 32) | (uint)roofed;
            shared.TryGetValue(key, out int n);
            shared[key] = n + 1;
        }

        /// <summary>Relabels every cell with its merged room; rooms smaller than <see cref="MinRoom"/> become outside.</summary>
        private void Merge(int[] owner)
        {
            int[] total = new int[sizes.Count];
            for (int id = 1; id < owner.Length; id++)
                total[owner[id]] += sizes[id];
            for (int cell = 0; cell < Labels.Length; cell++)
            {
                int id = Labels[cell];
                if (id > 0)
                    Labels[cell] = total[owner[id]] * CellArea >= MinRoom ? owner[id] : -1;
            }
        }
    }
}
