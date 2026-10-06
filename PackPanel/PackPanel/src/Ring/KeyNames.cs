using System.Collections.Generic;

namespace PackPanel.Ring
{
    /// <summary>
    /// The ring keys by the shared name their items carry ($item_cryptkey), each with its ring numbers, made once per key
    /// list and item database instead of every key compared with every item: the ring's count runs over the whole
    /// inventory. A key the item database does not have (a misspelt prefab, a mod not installed) is looked up again only
    /// when the database gains items or a new one loads.
    /// </summary>
    public static class KeyNames
    {
        private static readonly Dictionary<string, List<int>> numbers = new Dictionary<string, List<int>>();
        private static IReadOnlyList<string> madeFor;
        private static ObjectDB madeFrom;
        private static int madeItems = -1;

        /// <summary>Grows each time the lookup is made again, so a reader keeping counts knows to count again.</summary>
        public static int Version { get; private set; }

        /// <summary>Shared name to ring numbers (1 for the first key; two keys may share a name), up to date.</summary>
        public static Dictionary<string, List<int>> Get()
        {
            IReadOnlyList<string> prefabs = KeyRing.Prefabs;
            ObjectDB db = ObjectDB.instance;
            int items = db != null ? db.m_items.Count : -1;
            if (ReferenceEquals(prefabs, madeFor) && ReferenceEquals(db, madeFrom) && items == madeItems)
                return numbers;
            Make(prefabs.Count);
            madeFor = prefabs;
            madeFrom = db;
            madeItems = items;
            Version++;
            return numbers;
        }

        private static void Make(int keys)
        {
            numbers.Clear();
            for (int number = 1; number <= keys; number++)
            {
                string name = KeyRing.Key(number)?.m_shared.m_name;
                if (name == null)
                    continue;
                if (!numbers.TryGetValue(name, out List<int> list))
                    numbers[name] = list = new List<int>();
                list.Add(number);
            }
        }
    }
}
