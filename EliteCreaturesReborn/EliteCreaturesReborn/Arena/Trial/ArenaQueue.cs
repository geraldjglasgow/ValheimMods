using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The wave's creatures still to come, kept in the core's ZDO (<see cref="ArenaKeys.Queue"/>) so a machine that takes
    /// the core over sends exactly the creatures planned, as the raids keep their wave plan (<see cref="Raids.WavePlan"/>).
    /// A run is a kind (its prefab's hash), the wave it belongs to (its stars and mutation chance), the archway it comes
    /// out of (0 to 3; <see cref="BossLane"/> for a boss, who comes on the rune circle) and how many of it are left. Each
    /// archway sends its own creatures, the kinds mixed by how many of each are left; the archways with any left are the
    /// open portals (<see cref="LaneMask"/>). Written by the core's owner as a wave is planned and as creatures leave it;
    /// read every step, so the last decoded queue is kept against the very array it came from.
    /// </summary>
    internal sealed class ArenaQueue
    {
        /// <summary>The lane of a boss: no archway, the rune circle.</summary>
        public const int BossLane = -1;

        private const int Format = 2;
        private static readonly int Key = ArenaKeys.Queue.GetStableHashCode();

        private static byte[]? _cachedFrom;
        private static ArenaQueue? _cached;

        private readonly List<Run> _runs = new List<Run>();

        /// <summary>One kind of one wave out of one archway, still to come.</summary>
        private struct Run
        {
            public int Prefab;
            public int Wave;
            public int Lane;
            public int Count;
        }

        /// <summary>Every creature still to come.</summary>
        public int Count { get; private set; }

        /// <summary>True when a boss is still to come.</summary>
        public bool BossDue => In(BossLane) > 0;

        /// <summary>The archways (0 to 3) with creatures still to come, one bit each.</summary>
        public int LaneMask()
        {
            int mask = 0;
            foreach (Run run in _runs)
            {
                if (run.Count > 0 && run.Lane >= 0 && run.Lane < 31)
                {
                    mask |= 1 << run.Lane;
                }
            }
            return mask;
        }

        /// <summary>The creatures still to come out of one lane.</summary>
        public int In(int lane)
        {
            int count = 0;
            foreach (Run run in _runs)
            {
                count += run.Lane == lane ? run.Count : 0;
            }
            return count;
        }

        /// <summary>Plans <paramref name="count"/> of a kind for a wave out of a lane; nothing past
        /// <see cref="ArenaTable.MaxQueued"/>. A kind already planned there grows.</summary>
        public void Add(int prefab, int wave, int lane, int count)
        {
            int room = Mathf.Min(count, ArenaTable.MaxQueued - Count);
            if (room <= 0)
            {
                return;
            }
            int at = IndexOf(prefab, wave, lane);
            Run run = at >= 0 ? _runs[at] : new Run { Prefab = prefab, Wave = wave, Lane = lane };
            run.Count += room;
            Count += room;
            if (at >= 0)
            {
                _runs[at] = run;
            }
            else
            {
                _runs.Add(run);
            }
        }

        /// <summary>The next creature out of a lane leaves the queue, its kind drawn by how many of each are left there.
        /// False when the lane has none left.</summary>
        public bool Take(int lane, out int prefab, out int wave)
        {
            prefab = 0;
            wave = 0;
            int left = In(lane);
            int roll = left > 0 ? UnityEngine.Random.Range(0, left) : 0;
            for (int i = 0; i < _runs.Count && left > 0; i++)
            {
                Run run = _runs[i];
                if (run.Lane != lane || run.Count <= 0 || (roll -= run.Count) >= 0)
                {
                    continue;
                }
                prefab = run.Prefab;
                wave = run.Wave;
                Spend(i, run);
                return true;
            }
            return false;
        }

        /// <summary>Nothing more comes.</summary>
        public void Clear()
        {
            _runs.Clear();
            Count = 0;
        }

        private int IndexOf(int prefab, int wave, int lane)
        {
            for (int i = 0; i < _runs.Count; i++)
            {
                if (_runs[i].Prefab == prefab && _runs[i].Wave == wave && _runs[i].Lane == lane)
                {
                    return i;
                }
            }
            return -1;
        }

        private void Spend(int at, Run run)
        {
            run.Count--;
            Count--;
            if (run.Count > 0)
            {
                _runs[at] = run;
            }
            else
            {
                _runs.RemoveAt(at);
            }
        }

        /// <summary>The queue in the core's ZDO; an empty one when it holds none or one this version cannot read.</summary>
        public static ArenaQueue Read(ZDO core)
        {
            byte[]? bytes = core.GetByteArray(Key);
            if (bytes != null && ReferenceEquals(bytes, _cachedFrom) && _cached != null)
            {
                return _cached;
            }
            ArenaQueue queue = bytes != null ? Decode(bytes) : new ArenaQueue();
            _cachedFrom = bytes;
            _cached = queue;
            return queue;
        }

        /// <summary>Writes the queue into the core's ZDO. Core's owner only.</summary>
        public void Write(ZDO core)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(Format);
            pkg.Write(_runs.Count);
            foreach (Run run in _runs)
            {
                pkg.Write(run.Prefab);
                pkg.Write(run.Wave);
                pkg.Write(run.Lane);
                pkg.Write(run.Count);
            }
            byte[] bytes = pkg.GetArray();
            core.Set(Key, bytes);
            _cachedFrom = bytes;
            _cached = this;
        }

        private static ArenaQueue Decode(byte[] bytes)
        {
            ArenaQueue queue = new ArenaQueue();
            try
            {
                ZPackage pkg = new ZPackage(bytes);
                if (pkg.ReadInt() != Format)
                {
                    return queue; // an older queue (a trial saved by an earlier version) sends nothing more
                }
                for (int runs = pkg.ReadInt(); runs > 0; runs--)
                {
                    queue.Add(pkg.ReadInt(), pkg.ReadInt(), pkg.ReadInt(), Mathf.Max(0, pkg.ReadInt()));
                }
            }
            catch (Exception)
            {
                queue.Clear(); // a damaged queue sends nothing more rather than failing every step
            }
            return queue;
        }
    }
}
