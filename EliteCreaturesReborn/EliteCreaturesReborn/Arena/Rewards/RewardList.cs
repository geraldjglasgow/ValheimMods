using System;
using System.Collections.Generic;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// One trial's reward, as the core's owner plans it (<see cref="RewardPlan"/>) and each player of the trial receives
    /// it (<see cref="RewardGrant"/>): items by prefab name and count, each of a kind (an idol or other named item, the
    /// coins, a rune, essence), the factor of the skill XP bonus, and the trial it is for. It travels as one package.
    /// </summary>
    internal sealed class RewardList
    {
        private const int Format = 1;

        public enum Kind
        {
            Item = 0,
            Coins = 1,
            Rune = 2,
            Essence = 3,
        }

        /// <summary>One item of the reward.</summary>
        public readonly struct Entry
        {
            public Entry(string prefab, int count, Kind kind)
            {
                Prefab = prefab;
                Count = count;
                Type = kind;
            }

            public string Prefab { get; }

            public int Count { get; }

            public Kind Type { get; }
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>The trial the reward is for: the skill XP bonus applies to what was gained in it.</summary>
        public long Trial { get; set; }

        /// <summary>The skill XP bonus: each skill's gain in the trial again, times this.</summary>
        public float XpFactor { get; set; }

        public bool Empty => _entries.Count == 0 && XpFactor <= 0f;

        /// <summary>Adds <paramref name="count"/> of an item; one already listed grows.</summary>
        public void Add(string prefab, int count, Kind kind)
        {
            if (count <= 0 || string.IsNullOrEmpty(prefab))
            {
                return;
            }
            int at = _entries.FindIndex(e => e.Prefab == prefab);
            Entry entry = new Entry(prefab, count + (at >= 0 ? _entries[at].Count : 0), kind);
            if (at >= 0)
            {
                _entries[at] = entry;
            }
            else
            {
                _entries.Add(entry);
            }
        }

        /// <summary>A copy, for one player's own additions.</summary>
        public RewardList Copy()
        {
            RewardList copy = new RewardList { Trial = Trial, XpFactor = XpFactor };
            copy._entries.AddRange(_entries);
            return copy;
        }

        public ZPackage Pack()
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(Format);
            pkg.Write(Trial);
            pkg.Write(XpFactor);
            pkg.Write(_entries.Count);
            foreach (Entry entry in _entries)
            {
                pkg.Write(entry.Prefab);
                pkg.Write(entry.Count);
                pkg.Write((int)entry.Type);
            }
            return pkg;
        }

        /// <summary>The reward in a package; null for one this version cannot read.</summary>
        public static RewardList? Unpack(ZPackage pkg)
        {
            try
            {
                if (pkg.ReadInt() != Format)
                {
                    return null;
                }
                RewardList list = new RewardList { Trial = pkg.ReadLong(), XpFactor = pkg.ReadSingle() };
                for (int n = pkg.ReadInt(); n > 0; n--)
                {
                    list.Add(pkg.ReadString(), pkg.ReadInt(), (Kind)pkg.ReadInt());
                }
                return list;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
