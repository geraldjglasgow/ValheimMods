using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The near objects the server has not made yet and may make now, handed out one at a time in the game's own order
    /// (ZNetScene.ZDOCompare): the higher object type first (terrain, then solid, then prioritized, then default), and
    /// within a type the nearest to the server's own position first (ZNet.GetReferencePosition: the host on a hosted
    /// game, the world's middle on a dedicated server). The game sorts every one of them each pass and makes the first
    /// few; with every player's area listed, thousands can wait at once (a crowd arriving, a far jump into a big base),
    /// and sorting them all every pass costs more than making the few. A binary heap hands out the same first few for
    /// much less: about two comparisons an object to build it, about twice the log of the count for each one handed out.
    /// Each object's key is written into its ZDO as the game writes it (m_tempSortValue, the squared distance) and kept
    /// here beside its type, so comparing reads no ZDO. Two objects of one type at the same distance may come in either
    /// order, as in the game's sort, which is not stable either. Nothing is kept from one pass to the next:
    /// <see cref="Clear"/> lets go of every ZDO left.
    /// </summary>
    internal static class CreateOrder
    {
        private struct Entry
        {
            public ZDO Zdo;
            public float Key;
            public int Type;
        }

        private static Entry[] _heap = new Entry[256];
        private static int _count;

        /// <summary>The objects not handed out yet.</summary>
        public static int Count => _count;

        /// <summary>
        /// Takes every near object not made yet whose zone is ready for its kind (ZoneSystem.IsZoneReadyForType: no
        /// location or dungeon of a later kind still loading there), keyed as the game keys it, by its distance from
        /// <paramref name="from"/>. Left out here, one waiting on its zone costs nothing more; handed out, thousands of them
        /// were walked through every pass before anything could be made (12 ms a pass with 50 players' areas loaded,
        /// measured 2026-11-01).
        /// </summary>
        public static void Fill(List<ZDO> near, Vector3 from)
        {
            Clear();
            ZoneSystem zones = ZoneSystem.instance;
            for (int i = 0; i < near.Count; i++)
            {
                ZDO zdo = near[i];
                if (!zdo.Created && zones.IsZoneReadyForType(zdo.GetSector(), zdo.Type))
                {
                    zdo.m_tempSortValue = Utils.DistanceSqr(from, zdo.GetPosition());
                    Add(zdo);
                }
            }
            for (int i = _count / 2 - 1; i >= 0; i--)
            {
                SiftDown(i);
            }
        }

        /// <summary>The next object in the game's order; asked only while <see cref="Count"/> is above 0.</summary>
        public static ZDO Next()
        {
            ZDO first = _heap[0].Zdo;
            _count--;
            _heap[0] = _heap[_count];
            _heap[_count] = default;
            if (_count > 0)
            {
                SiftDown(0);
            }
            return first;
        }

        /// <summary>Lets go of every object not handed out.</summary>
        public static void Clear()
        {
            Array.Clear(_heap, 0, _count);
            _count = 0;
        }

        private static void Add(ZDO zdo)
        {
            if (_count == _heap.Length)
            {
                Array.Resize(ref _heap, _heap.Length * 2);
            }
            _heap[_count++] = new Entry { Zdo = zdo, Key = zdo.m_tempSortValue, Type = (int)zdo.Type };
        }

        // Moves the entry at the index down until neither of its children comes before it.
        private static void SiftDown(int index)
        {
            Entry moving = _heap[index];
            int child = 2 * index + 1;
            while (child < _count)
            {
                if (child + 1 < _count && Before(in _heap[child + 1], in _heap[child]))
                {
                    child++;
                }
                if (!Before(in _heap[child], in moving))
                {
                    break;
                }
                _heap[index] = _heap[child];
                index = child;
                child = 2 * index + 1;
            }
            _heap[index] = moving;
        }

        // ZNetScene.ZDOCompare: the higher type first; within a type the smaller key (as Utils.CompareFloats orders them).
        private static bool Before(in Entry a, in Entry b) => a.Type != b.Type ? a.Type > b.Type : a.Key < b.Key;
    }
}
