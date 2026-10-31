using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The game's send order (ZDOMan.ServerSortSendZDOS with ServerSendCompare), each object's place worked out once
    /// instead of in every comparison: first creatures and players (the Prioritized type) that someone other than the
    /// receiver owns; then by type, terrain before building pieces (Solid) before the other Prioritized before the
    /// Default type; within each, by the distance from the receiver's reference position less 1.5 m for every second
    /// since they last got the object (up to 100 s, which is what an object they never got counts as), so the closest
    /// and the longest waiting go first. The game sorts every candidate on every send; here, when there are more than one
    /// round can carry (<see cref="Limit"/>), only that many of the best are picked (a quickselect) and sorted, and the
    /// rest wait in the pending set for the next round. ZDO.m_tempSortValue is left alone (the game's sort writes it).
    /// </summary>
    internal sealed class SendOrder
    {
        /// <summary>The most objects a round can carry: 25,600 bytes at about 40 bytes for the smallest record.</summary>
        public const int Limit = 640;

        private const double GroupStep = 1e6;

        private double[] _keys = new double[64];
        private ZDO[] _items = new ZDO[64];

        public int Count { get; private set; }

        /// <summary>The object's place: its group (see the class summary) first, then the game's sort value.</summary>
        public static double Key(long receiver, ZDO zdo, Vector3 refPos, float time, bool has, float syncTime)
        {
            float waited = has ? Mathf.Clamp(time - syncTime, 0f, 100f) : 100f;
            // A true distance, as the game's: the waiting time is taken off it in metres.
            float value = Vector3.Distance(zdo.GetPosition(), refPos) - waited * 1.5f;
            return Group(receiver, zdo) * GroupStep + value;
        }

        // ServerSendCompare: owned-by-someone-else Prioritized first, then the higher ObjectType first.
        private static int Group(long receiver, ZDO zdo)
        {
            if (zdo.Type == ZDO.ObjectType.Prioritized && zdo.HasOwner() && zdo.GetOwner() != receiver)
            {
                return 0;
            }
            return 4 - (int)zdo.Type;
        }

        public void Add(ZDO zdo, double key)
        {
            if (Count == _keys.Length)
            {
                Array.Resize(ref _keys, Count * 2);
                Array.Resize(ref _items, Count * 2);
            }
            _keys[Count] = key;
            _items[Count] = zdo;
            Count++;
        }

        /// <summary>Appends the best <see cref="Limit"/> (all of them when fewer) to the list, in order.</summary>
        public void TakeBest(List<ZDO> into)
        {
            int n = Math.Min(Count, Limit);
            if (Count > n)
            {
                Select(n - 1);
            }
            Array.Sort(_keys, _items, 0, n);
            for (int i = 0; i < n; i++)
            {
                into.Add(_items[i]);
            }
        }

        /// <summary>Empties it, dropping the references (ZDOs are pooled: none is kept past the frame).</summary>
        public void Clear()
        {
            Array.Clear(_items, 0, Count);
            Count = 0;
        }

        // Wirth's selection (FIND): afterwards the key at target is the one a full sort would put there, the keys before
        // it no larger and the keys after it no smaller. Linear on average, and safe with many equal keys.
        private void Select(int target)
        {
            int lo = 0;
            int hi = Count - 1;
            while (lo < hi)
            {
                int i = lo;
                int j = hi;
                Split(_keys[target], ref i, ref j);
                if (j < target)
                {
                    lo = i;
                }
                if (target < i)
                {
                    hi = j;
                }
            }
        }

        // Hoare's split around the pivot: afterwards the keys up to j are no larger than it and those from i on no smaller.
        private void Split(double pivot, ref int i, ref int j)
        {
            do
            {
                while (_keys[i] < pivot)
                {
                    i++;
                }
                while (pivot < _keys[j])
                {
                    j--;
                }
                if (i <= j)
                {
                    Swap(i++, j--);
                }
            }
            while (i <= j);
        }

        private void Swap(int a, int b)
        {
            double key = _keys[a];
            _keys[a] = _keys[b];
            _keys[b] = key;
            ZDO item = _items[a];
            _items[a] = _items[b];
            _items[b] = item;
        }
    }
}
