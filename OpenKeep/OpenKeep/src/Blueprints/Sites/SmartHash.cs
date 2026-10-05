using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// A blueprint's pieces bucketed by footprint centre on a 4 m grid, so the pieces near a point (floors under a
    /// piece, boxes that may touch it) are found without walking the whole blueprint.
    /// </summary>
    internal sealed class SmartHash
    {
        private const float Cell = 4f;

        private readonly Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>();
        private readonly float longest;

        public SmartHash(SmartBox[] boxes)
        {
            for (int i = 0; i < boxes.Length; i++)
            {
                long key = Key(Bucket(boxes[i].X), Bucket(boxes[i].Z));
                if (!cells.TryGetValue(key, out List<int> list))
                    cells[key] = list = new List<int>();
                list.Add(i);
                longest = Mathf.Max(longest, boxes[i].Reach);
            }
        }

        /// <summary>Every piece whose footprint may come within <paramref name="radius"/> of the frame point.</summary>
        public IEnumerable<int> Near(float x, float z, float radius)
        {
            int span = Mathf.CeilToInt((radius + longest) / Cell);
            int cx = Bucket(x), cz = Bucket(z);
            for (int ix = cx - span; ix <= cx + span; ix++)
            {
                for (int iz = cz - span; iz <= cz + span; iz++)
                {
                    if (cells.TryGetValue(Key(ix, iz), out List<int> list))
                        foreach (int index in list)
                            yield return index;
                }
            }
        }

        private static int Bucket(float v) => Mathf.FloorToInt(v / Cell);

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
    }
}
