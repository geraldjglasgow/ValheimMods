using System;
using System.Collections.Generic;
using Splatform;

namespace HighSeat
{
    /// <summary>
    /// An index over the world's player history (World.m_playerHistory: every player who ever joined, in the order they
    /// first came, saved with the world), so the server finds a player in it in one step instead of reading the whole
    /// list. Two views: each id's first place (what the game's FindIndex on the id gives), and for each display name the
    /// places holding it, in order (a chain through <see cref="Next"/>), which is what the game's unique name count reads.
    /// It follows the list itself: <see cref="Sync"/> builds it again whenever the list is another one (a new world) or its
    /// change count moved (<see cref="ListVersion{T}"/>) since it was last brought up to date, so a change by the game's
    /// own code or another mod is never missed; the history patch here reports its own changes as it makes them
    /// (<see cref="Appended"/>, <see cref="Replaced"/>), so its own work never costs a rebuild. A rebuild reads the list
    /// once and makes no garbage once the tables have grown to the list's size. Main thread only, like the game's own
    /// use of the list; server only (the patches that use it run on the server).
    /// </summary>
    internal static class HistoryIndex
    {
        private struct Chain
        {
            public int First;
            public int Last;
        }

        private static readonly Chain None = new Chain { First = -1, Last = -1 };
        private static readonly Dictionary<PlatformUserID, int> ById = new Dictionary<PlatformUserID, int>(IdKeys.Comparer);
        private static readonly Dictionary<string, Chain> ByName = new Dictionary<string, Chain>(StringComparer.Ordinal);
        private static readonly List<int> Next = new List<int>();
        private static Chain _nullName = None;
        private static List<ZNet.CrossNetworkUserInfo> _list;
        private static int _version;

        public static void Sync(List<ZNet.CrossNetworkUserInfo> list)
        {
            if (!ReferenceEquals(list, _list) || ListVersion<ZNet.CrossNetworkUserInfo>.Of(list) != _version)
            {
                Rebuild(list);
            }
        }

        /// <summary>The first place holding this id, -1 when none does: the game's FindIndex by id.</summary>
        public static int IndexOf(PlatformUserID id) => ById.TryGetValue(id, out int at) ? at : -1;

        /// <summary>The first place whose display name is this one (ordinal, as the game's ==), -1 when none.</summary>
        public static int FirstNamed(string name) => Get(name).First;

        /// <summary>The next place after this one with the same display name, -1 at the last.</summary>
        public static int NextNamed(int at) => Next[at];

        /// <summary>The list's last entry was just added by the history patch.</summary>
        public static void Appended()
        {
            int at = _list.Count - 1;
            ZNet.CrossNetworkUserInfo entry = _list[at];
            Next.Add(-1);
            if (!ById.ContainsKey(entry.m_id))
            {
                ById.Add(entry.m_id, at);
            }
            Link(at, entry.m_displayName);
            _version = ListVersion<ZNet.CrossNetworkUserInfo>.Of(_list);
        }

        /// <summary>The entry at <paramref name="at"/> was just replaced by one with the same id; it had this name.</summary>
        public static void Replaced(int at, string before)
        {
            string name = _list[at].m_displayName;
            if (!string.Equals(name, before))
            {
                Unlink(at, before);
                Link(at, name);
            }
            _version = ListVersion<ZNet.CrossNetworkUserInfo>.Of(_list);
        }

        private static void Rebuild(List<ZNet.CrossNetworkUserInfo> list)
        {
            _list = list;
            ById.Clear();
            ByName.Clear();
            Next.Clear();
            _nullName = None;
            for (int i = 0; i < list.Count; i++)
            {
                ZNet.CrossNetworkUserInfo entry = list[i];
                Next.Add(-1);
                if (!ById.ContainsKey(entry.m_id))
                {
                    ById.Add(entry.m_id, i);
                }
                Link(i, entry.m_displayName);
            }
            _version = ListVersion<ZNet.CrossNetworkUserInfo>.Of(list);
        }

        // Puts a place into its name's chain, in place order: at the end in one step (a rebuild, a new entry).
        private static void Link(int at, string name)
        {
            Chain chain = Get(name);
            if (chain.First < 0)
            {
                chain = new Chain { First = at, Last = at };
            }
            else if (at > chain.Last)
            {
                Next[chain.Last] = at;
                chain.Last = at;
            }
            else if (at < chain.First)
            {
                Next[at] = chain.First;
                chain.First = at;
            }
            else
            {
                int before = Before(chain.First, at);
                Next[at] = Next[before];
                Next[before] = at;
            }
            Set(name, chain);
        }

        private static void Unlink(int at, string name)
        {
            Chain chain = Get(name);
            if (chain.First == at)
            {
                chain.First = Next[at];
                chain.Last = chain.First < 0 ? -1 : chain.Last;
            }
            else
            {
                int before = chain.First < 0 ? -1 : Before(chain.First, at);
                if (before < 0 || Next[before] != at)
                {
                    throw new InvalidOperationException($"player history place {at} is not under its old name");
                }
                Next[before] = Next[at];
                chain.Last = chain.Last == at ? before : chain.Last;
            }
            Next[at] = -1;
            Set(name, chain);
        }

        // The last place in the chain from `first` that comes before `at`.
        private static int Before(int first, int at)
        {
            int place = first;
            while (Next[place] >= 0 && Next[place] < at)
            {
                place = Next[place];
            }
            return place;
        }

        private static Chain Get(string name)
        {
            if (name == null)
            {
                return _nullName;
            }
            return ByName.TryGetValue(name, out Chain chain) ? chain : None;
        }

        private static void Set(string name, Chain chain)
        {
            if (name == null)
            {
                _nullName = chain;
            }
            else if (chain.First < 0)
            {
                ByName.Remove(name);
            }
            else
            {
                ByName[name] = chain;
            }
        }
    }
}
