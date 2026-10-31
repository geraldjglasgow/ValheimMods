using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus
{
    /// <summary>
    /// Columns of the world, 4 m square with height ignored, that tell the records of ground-held pieces
    /// (<see cref="SupportRecord"/>) that something near them changed. Each column has a stamp, raised by every change
    /// over it, and a time until which it is still settling. A ground-held record keeps the columns its boxes cover and
    /// the sum of their stamps when it was made: stamps only go up, so a different sum means a change. No record is made
    /// over a column that is still settling, so a door still swinging or a piece whose look switches a frame after it
    /// appears is never caught half way.
    /// <para>
    /// A piece's change (<see cref="Changed"/>: a piece appearing, its worn or broken look switching, a door swinging)
    /// covers the columns under its colliders, made if missing. A ground rebuild (<see cref="GroundChanged"/>) covers a
    /// whole zone, 256 columns, so it raises only columns that already exist (pieces and records make them) and holds
    /// back every new record for <see cref="GroundSettleSeconds"/>. Every area is widened by <see cref="Margin"/> before
    /// its columns are taken; a record covers at most <see cref="MaxRecordColumns"/> columns (a larger piece keeps the
    /// game's check), a change more than <see cref="MaxChangeColumns"/> (none in the game) raises every column. Columns
    /// hold two numbers and are kept until the world closes (<see cref="Reset"/>), so their number is bounded by the built
    /// area. Rock- and tree-held records need no columns: they check their anchors themselves.
    /// </para>
    /// </summary>
    internal static class SupportCells
    {
        internal sealed class Cell
        {
            public int Stamp;
            public float SettleUntil;
        }

        public const float GroundSettleSeconds = 0.5f;
        private const float Size = 4f;
        private const float Margin = 0.25f;
        private const int MaxRecordColumns = 64;
        private const int MaxChangeColumns = 4096;

        private static readonly Dictionary<long, Cell> Cells = new Dictionary<long, Cell>();
        private static float _groundSettleUntil;

        public static void Reset()
        {
            Cells.Clear();
            _groundSettleUntil = 0f;
        }

        /// <summary>Something that a ground-held piece's check could see appeared or moved inside
        /// <paramref name="area"/>: its columns are raised and settle for <paramref name="settleSeconds"/>.</summary>
        public static void Changed(Bounds area, float settleSeconds)
        {
            float until = Time.time + settleSeconds;
            if (!Range(area, MaxChangeColumns, out int x0, out int x1, out int z0, out int z1))
            {
                RaiseAll(until);
                return;
            }
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    Cell cell = GetOrAdd(Key(x, z));
                    cell.Stamp++;
                    cell.SettleUntil = Mathf.Max(cell.SettleUntil, until);
                }
            }
        }

        /// <summary>The ground under <paramref name="area"/> was rebuilt: the columns already in use are raised, and no
        /// record is made anywhere for a moment.</summary>
        public static void GroundChanged(Bounds area)
        {
            _groundSettleUntil = Time.time + GroundSettleSeconds;
            if (Cells.Count == 0)
            {
                return;
            }
            if (!Range(area, MaxChangeColumns, out int x0, out int x1, out int z0, out int z1))
            {
                RaiseAll(0f);
                return;
            }
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    if (Cells.TryGetValue(Key(x, z), out Cell cell))
                    {
                        cell.Stamp++;
                    }
                }
            }
        }

        /// <summary>True while the ground was rebuilt a moment ago, or any existing column under one of the piece's
        /// boxes is still settling: a record made now could miss what is still changing. Box by box, because the game
        /// sets a box at the world origin for a collider that was inactive at the piece's first check.</summary>
        public static bool Settling(List<WearNTear.BoundData> boxes)
        {
            if (Time.time < _groundSettleUntil)
            {
                return true;
            }
            foreach (WearNTear.BoundData box in boxes)
            {
                if (Settling(SupportScan.Box(box)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Gives <paramref name="record"/> the columns under the piece's boxes (made if missing) and the sum of
        /// their stamps; false when they cover too many columns.</summary>
        public static bool Collect(List<WearNTear.BoundData> boxes, SupportRecord record)
        {
            record.CellCount = 0;
            foreach (WearNTear.BoundData box in boxes)
            {
                if (!Range(SupportScan.Box(box), MaxRecordColumns, out int x0, out int x1, out int z0, out int z1))
                {
                    return false;
                }
                for (int x = x0; x <= x1; x++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        record.AddCell(GetOrAdd(Key(x, z)));
                    }
                }
            }
            record.StampSum = Sum(record.Cells, record.CellCount);
            return record.CellCount <= MaxRecordColumns;
        }

        private static bool Settling(Bounds area)
        {
            float now = Time.time;
            if (!Range(area, MaxRecordColumns, out int x0, out int x1, out int z0, out int z1))
            {
                return true;
            }
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    if (Cells.TryGetValue(Key(x, z), out Cell cell) && now < cell.SettleUntil)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static int Sum(Cell[] cells, int count)
        {
            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += cells[i].Stamp;
            }
            return sum;
        }

        // The columns under area, widened by Margin; false when there are more than max of them.
        private static bool Range(Bounds area, int max, out int x0, out int x1, out int z0, out int z1)
        {
            Vector3 min = area.min;
            Vector3 top = area.max;
            x0 = Mathf.FloorToInt((min.x - Margin) / Size);
            x1 = Mathf.FloorToInt((top.x + Margin) / Size);
            z0 = Mathf.FloorToInt((min.z - Margin) / Size);
            z1 = Mathf.FloorToInt((top.z + Margin) / Size);
            long count = (long)(x1 - x0 + 1) * (z1 - z0 + 1);
            return count > 0 && count <= max;
        }

        // A change too big to list by column (none in the game: a zone is 17 by 17 with the margin) raises every column.
        private static void RaiseAll(float settleUntil)
        {
            foreach (Cell cell in Cells.Values)
            {
                cell.Stamp++;
                cell.SettleUntil = Mathf.Max(cell.SettleUntil, settleUntil);
            }
        }

        private static long Key(int x, int z) => ((long)x << 32) | (uint)z;

        private static Cell GetOrAdd(long key)
        {
            if (!Cells.TryGetValue(key, out Cell cell))
            {
                cell = new Cell();
                Cells.Add(key, cell);
            }
            return cell;
        }
    }
}
