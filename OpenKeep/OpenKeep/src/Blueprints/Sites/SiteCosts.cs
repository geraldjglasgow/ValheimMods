using System.Collections.Generic;
using System.Linq;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// What building costs on a construction site, in the store's terms (item prefab names, e.g. "Wood"): each piece
    /// prefab's building materials (read once per world from its <c>Piece.m_resources</c>; nothing when the world's "no
    /// build cost" key covers it, checked at use time as <see cref="MaterialBill"/> does), the materials of a set of
    /// pieces, taking a piece's materials out of a store, and the names players read.
    /// </summary>
    public static class SiteCosts
    {
        public const string StoneItem = "Stone";

        private static readonly Dictionary<string, List<KeyValuePair<string, int>>> costs = new Dictionary<string, List<KeyValuePair<string, int>>>();
        private static readonly Dictionary<string, string> sharedNames = new Dictionary<string, string>();
        private static readonly List<KeyValuePair<string, int>> Nothing = new List<KeyValuePair<string, int>>();
        private static ZNetScene scene;

        /// <summary>A piece prefab's materials by item prefab name; empty when it is free in this world or the game has no such piece.</summary>
        public static List<KeyValuePair<string, int>> Of(string piecePrefab)
        {
            ForgetOtherWorld();
            Piece piece = MaterialBill.PieceOf(piecePrefab);
            if (piece == null || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())))
                return Nothing;
            if (!costs.TryGetValue(piecePrefab, out List<KeyValuePair<string, int>> cost))
                costs[piecePrefab] = cost = Read(piece);
            return cost;
        }

        private static List<KeyValuePair<string, int>> Read(Piece piece)
        {
            List<KeyValuePair<string, int>> cost = new List<KeyValuePair<string, int>>();
            foreach (Piece.Requirement r in piece.m_resources)
            {
                if (r.m_resItem != null && r.m_amount > 0)
                    cost.Add(new KeyValuePair<string, int>(Utils.GetPrefabName(r.m_resItem.gameObject), r.m_amount));
            }
            return cost;
        }

        /// <summary>The materials of the given pieces that are not built yet (for the planner's queue rows).</summary>
        public static Dictionary<string, int> ForPieces(Blueprint bp, IEnumerable<int> pieces, bool[] built)
        {
            Dictionary<string, int> sum = new Dictionary<string, int>();
            foreach (int i in pieces)
            {
                if (i >= 0 && i < bp.Pieces.Count && (built == null || i >= built.Length || !built[i]))
                    AddTo(sum, Of(bp.Pieces[i].Prefab));
            }
            return sum;
        }

        public static void AddTo(Dictionary<string, int> sum, List<KeyValuePair<string, int>> cost)
        {
            foreach (KeyValuePair<string, int> c in cost)
                Add(sum, c.Key, c.Value);
        }

        public static void Add(Dictionary<string, int> sum, string item, int amount)
        {
            int now = Count(sum, item) + amount;
            if (now > 0)
                sum[item] = now;
            else
                sum.Remove(item);
        }

        public static int Count(Dictionary<string, int> store, string item) => store.TryGetValue(item, out int n) ? n : 0;

        /// <summary>Takes a piece's materials out of the store; false (and the store unchanged) when something is short.</summary>
        public static bool TryTake(Dictionary<string, int> store, string piecePrefab)
        {
            List<KeyValuePair<string, int>> cost = Of(piecePrefab);
            if (cost.Any(c => Count(store, c.Key) < c.Value))
                return false;
            foreach (KeyValuePair<string, int> c in cost)
                Add(store, c.Key, -c.Value);
            return true;
        }

        /// <summary>The item's name token ("$item_wood"), or null when the game has no such item.</summary>
        public static string SharedName(string itemPrefab)
        {
            ForgetOtherWorld();
            if (sharedNames.TryGetValue(itemPrefab, out string name))
                return name;
            if (ObjectDB.instance == null)
                return null;
            GameObject prefab = ObjectDB.instance.GetItemPrefab(itemPrefab);
            name = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name : null;
            sharedNames[itemPrefab] = name;
            return name;
        }

        /// <summary>Amounts as "Wood 120, Stone 40" (localized), biggest first, at most <paramref name="most"/> and "+N"; null when empty.</summary>
        public static string Describe(Dictionary<string, int> amounts, int most = 5)
        {
            if (amounts.Count == 0)
                return null;
            List<string> parts = amounts.OrderByDescending(a => a.Value).Take(most)
                .Select(a => $"{Language.Localize(SharedName(a.Key) ?? a.Key)} {a.Value}").ToList();
            if (amounts.Count > most)
                parts.Add($"+{amounts.Count - most}");
            return string.Join(", ", parts);
        }

        /// <summary>A new world has its own prefabs and item database: the tables are read again from them.</summary>
        private static void ForgetOtherWorld()
        {
            if (ZNetScene.instance == scene)
                return;
            scene = ZNetScene.instance;
            costs.Clear();
            sharedNames.Clear();
        }
    }
}
