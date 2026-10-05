using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// Some pieces' drawn boxes on a 4 m grid of their centres, to find the boxes one box can touch without testing every
    /// pair, and to spread from a box over touching boxes (<see cref="Flood"/>): a run of one type, or one building of a
    /// selection.
    /// </summary>
    internal sealed class CopyGrid
    {
        private const float Cell = 4f;

        private readonly Dictionary<long, List<FixPiece>> cells = new Dictionary<long, List<FixPiece>>();
        private float longest;

        public CopyGrid(IEnumerable<FixPiece> boxes)
        {
            foreach (FixPiece p in boxes)
            {
                long key = Key(Mathf.FloorToInt(p.Centre.x / Cell), Mathf.FloorToInt(p.Centre.z / Cell));
                if (!cells.TryGetValue(key, out List<FixPiece> list))
                    cells[key] = list = new List<FixPiece>();
                list.Add(p);
                longest = Mathf.Max(longest, p.Reach);
            }
        }

        /// <summary>Every box of the grid joined to <paramref name="start"/> through touching boxes, the start first; each is added to <paramref name="seen"/>.</summary>
        public List<FixPiece> Flood(FixPiece start, HashSet<Piece> seen)
        {
            List<FixPiece> found = new List<FixPiece> { start };
            seen.Add(start.Piece);
            for (int next = 0; next < found.Count; next++)
            {
                foreach (FixPiece other in Near(found[next]))
                {
                    if (!seen.Contains(other.Piece) && CopyTouch.Touching(found[next], other))
                    {
                        seen.Add(other.Piece);
                        found.Add(other);
                    }
                }
            }
            return found;
        }

        /// <summary>The boxes whose centres lie in the cells this box's reach plus the longest box's can touch.</summary>
        private IEnumerable<FixPiece> Near(FixPiece p)
        {
            int span = Mathf.CeilToInt((p.Reach + longest + CopyTouch.Touch) / Cell);
            int cx = Mathf.FloorToInt(p.Centre.x / Cell), cz = Mathf.FloorToInt(p.Centre.z / Cell);
            for (int x = cx - span; x <= cx + span; x++)
            {
                for (int z = cz - span; z <= cz + span; z++)
                {
                    if (cells.TryGetValue(Key(x, z), out List<FixPiece> list))
                    {
                        foreach (FixPiece other in list)
                            yield return other;
                    }
                }
            }
        }

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
    }
}
