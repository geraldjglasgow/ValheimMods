using System;

namespace HighSeat
{
    /// <summary>
    /// The crowd part of the update rate (<see cref="Throttle"/>), worked out for one player's send: of the objects they
    /// already have that changed and wait to go, the nearest <see cref="EveryRound"/> may go every round, the next
    /// <see cref="EverySecondRound"/> at most every 2nd round and the rest at most every 4th, however close they all are.
    /// By distance alone a crowd inside 32 m got everything every round: 50 players in a 30 m circle took about 370 KB/s
    /// each and 19 MB/s in all (DevBridge's fake players, 2026-10-31). Distances are squared, from the player's reference
    /// position; an object held back since an earlier round counts at the distance it was held at (<see cref="Holds"/>).
    /// The two thresholds are found by selection (no sort, nothing allocated after the buffer has grown). One per thread
    /// that prepares sends (<see cref="SendPrep"/>), so players prepared at the same time never share one.
    /// </summary>
    internal sealed class CrowdRank
    {
        private const int EveryRound = 10;
        private const int EverySecondRound = 20;

        private float[] _d2 = new float[256];
        private int _count;
        private float _full = float.PositiveInfinity;
        private float _half = float.PositiveInfinity;

        /// <summary>Starts a player's send: no distances yet.</summary>
        public void Clear() => _count = 0;

        /// <summary>One object the player has that changed and could wait (an owner change never waits).</summary>
        public void Add(float distance2)
        {
            if (_count == _d2.Length)
            {
                Array.Resize(ref _d2, _count * 2);
            }
            _d2[_count++] = distance2;
        }

        /// <summary>The thresholds from the distances added since <see cref="Clear"/>.</summary>
        public void Measure()
        {
            int both = EveryRound + EverySecondRound;
            _full = _count > EveryRound ? Select(EveryRound - 1, 0) : float.PositiveInfinity;
            _half = _count > both ? Select(both - 1, EveryRound) : float.PositiveInfinity;
        }

        /// <summary>Rounds between two sends of an object at this squared distance: 1, 2 or 4.</summary>
        public int Every(float distance2) => distance2 <= _full ? 1 : distance2 <= _half ? 2 : 4;

        // The k-th smallest of the distances (Hoare's selection): afterwards none before k is larger and none after it is
        // smaller, so a second call for a larger k can start at the first call's k + 1 (from).
        private float Select(int k, int from)
        {
            int lo = from;
            int hi = _count - 1;
            while (lo < hi)
            {
                float pivot = _d2[(lo + hi) >> 1];
                int i = lo;
                int j = hi;
                Partition(pivot, ref i, ref j);
                if (k <= j)
                {
                    hi = j;
                }
                else if (k >= i)
                {
                    lo = i;
                }
                else
                {
                    break;
                }
            }
            return _d2[k];
        }

        private void Partition(float pivot, ref int i, ref int j)
        {
            while (i <= j)
            {
                while (_d2[i] < pivot)
                {
                    i++;
                }
                while (_d2[j] > pivot)
                {
                    j--;
                }
                if (i <= j)
                {
                    (_d2[i], _d2[j]) = (_d2[j], _d2[i]);
                    i++;
                    j--;
                }
            }
        }
    }
}
