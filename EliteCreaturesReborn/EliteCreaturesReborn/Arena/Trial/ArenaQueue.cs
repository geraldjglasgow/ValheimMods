using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The arena creatures still to come, kept in the core's ZDO (<see cref="ArenaKeys.Queue"/>) so a machine that takes
    /// the core over sends exactly the creatures planned, as the raids keep their wave plan
    /// (<see cref="Raids.WavePlan"/>). A run is a kind (its prefab's hash), the wave it belongs to (its stars and
    /// mutation chance) and how many of it are left. The earliest wave's creatures come first, the kinds mixed by how many
    /// of each are left. Written by the core's owner as a wave is planned and as creatures leave it; read every step, so
    /// the last decoded queue is kept against the very array it came from.
    /// </summary>
    internal sealed class ArenaQueue
    {
        private const int Format = 1;
        private static readonly int Key = ArenaKeys.Queue.GetStableHashCode();

        private static byte[]? _cachedFrom;
        private static ArenaQueue? _cached;

        private readonly List<Run> _runs = new List<Run>();

        /// <summary>One kind of one wave, still to come.</summary>
        private struct Run
        {
            public int Prefab;
            public int Wave;
            public int Count;
        }

        /// <summary>Every creature still to come.</summary>
        public int Count { get; private set; }

        /// <summary>True when a boss is still to come.</summary>
        public bool BossDue
        {
            get
            {
                foreach (Run run in _runs)
                {
                    if (run.Count > 0 && ArenaTable.IsBossWave(run.Wave))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>Plans <paramref name="count"/> of a kind for a wave; nothing past
        /// <see cref="ArenaTable.MaxQueued"/>.</summary>
        public void Add(int prefab, int wave, int count)
        {
            int room = Mathf.Min(count, ArenaTable.MaxQueued - Count);
            if (room <= 0)
            {
                return;
            }
            _runs.Add(new Run { Prefab = prefab, Wave = wave, Count = room });
            Count += room;
        }

        /// <summary>The next creature leaves the queue: from the earliest wave waiting, its kind drawn by how many of each
        /// are left. False when none is left.</summary>
        public bool Take(out int prefab, out int wave)
        {
            prefab = 0;
            wave = 0;
            int earliest = EarliestWave(out int inIt);
            if (inIt <= 0)
            {
                return false;
            }
            int roll = UnityEngine.Random.Range(0, inIt);
            for (int i = 0; i < _runs.Count; i++)
            {
                Run run = _runs[i];
                if (run.Wave != earliest || run.Count <= 0 || (roll -= run.Count) >= 0)
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

        private int EarliestWave(out int inIt)
        {
            int earliest = int.MaxValue;
            inIt = 0;
            foreach (Run run in _runs)
            {
                if (run.Count <= 0 || run.Wave > earliest)
                {
                    continue;
                }
                inIt = run.Wave < earliest ? run.Count : inIt + run.Count;
                earliest = run.Wave;
            }
            return earliest;
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
                    return queue;
                }
                for (int runs = pkg.ReadInt(); runs > 0; runs--)
                {
                    queue.Add(pkg.ReadInt(), pkg.ReadInt(), Mathf.Max(0, pkg.ReadInt()));
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
