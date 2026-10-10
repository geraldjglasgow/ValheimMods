using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The game's items as the studio's Items tab lists them: every item in the object database that has an icon and a
    /// mesh, with its name, group (<see cref="ItemGroups"/>), tier and biome (<see cref="ItemTiers"/>) and the station
    /// that makes it. A search is words, each found in the name, the prefab, the group or its words, or the biome.
    /// Worked out once per world and item count.
    /// </summary>
    internal static class StudioItems
    {
        private sealed class Entry
        {
            internal string Prefab;
            internal string Name;
            internal string Group;
            internal int GroupRank;
            internal Tier? Tier;
            internal string Station;
            internal string Text;
        }

        private static List<Entry> entries;
        private static ObjectDB builtFor;
        private static int builtCount;

        /// <summary>
        /// The items matching every word, in groups: first a group the search names ("shield" puts Shields first), then
        /// weapons, armour and the rest; in each group by tier, then name.
        /// </summary>
        internal static Dictionary<string, object> Search(string filter, int most)
        {
            string[] words = (filter ?? "").ToLowerInvariant().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<Entry> found = Entries().Where(entry => words.All(entry.Text.Contains)).ToList();
            List<Entry> shown = found.OrderBy(e => Named(e.Group, words) ? 0 : 1).ThenBy(e => e.GroupRank)
                .ThenBy(e => e.Tier?.Rank ?? 99).ThenBy(e => e.Name).Take(most).ToList();
            return new Dictionary<string, object>
            {
                ["found"] = found.Count,
                ["shown"] = shown.Count,
                ["groups"] = shown.GroupBy(entry => entry.Group).Select(Group).ToList(),
            };
        }

        private static bool Named(string group, string[] words)
        {
            string named = ItemGroups.Words(group).ToLowerInvariant();
            return words.Any(named.Contains);
        }

        private static Dictionary<string, object> Group(IGrouping<string, Entry> group) => new Dictionary<string, object>
        {
            ["name"] = group.Key,
            ["items"] = group.Select(Line).ToList(),
        };

        private static Dictionary<string, object> Line(Entry entry) => new Dictionary<string, object>
        {
            ["prefab"] = entry.Prefab,
            ["name"] = entry.Name,
            ["group"] = entry.Group,
            ["tier"] = entry.Tier?.Rank + 1,
            ["biome"] = entry.Tier?.Biome,
            ["station"] = entry.Station,
        };

        private static List<Entry> Entries()
        {
            ObjectDB db = ObjectDB.instance;
            if (!db || !ZNetScene.instance) throw new BridgeException("no world loaded: items are listed from the game's object database");
            if (entries != null && builtFor == db && builtCount == db.m_items.Count) return entries;
            Dictionary<string, Tier> tiers = ItemTiers.Compute();
            Dictionary<string, Recipe> recipes = db.m_recipes.Where(r => r && r.m_enabled && r.m_item)
                .GroupBy(r => TierSources.Name(r.m_item.gameObject)).ToDictionary(g => g.Key, g => g.First());
            entries = db.m_items.Where(Listed).Select(prefab => Make(prefab, tiers, recipes)).ToList();
            (builtFor, builtCount) = (db, db.m_items.Count);
            return entries;
        }

        // An item a player can hold and see: it has an icon and a mesh (not a creature's attack or a hidden helper).
        private static bool Listed(GameObject prefab)
        {
            ItemDrop item = prefab ? prefab.GetComponent<ItemDrop>() : null;
            if (!item || item.m_itemData?.m_shared == null || item.m_itemData.m_shared.m_icons.Length == 0) return false;
            return prefab.GetComponentsInChildren<Renderer>(true).Any(r => r is MeshRenderer || r is SkinnedMeshRenderer);
        }

        private static Entry Make(GameObject prefab, Dictionary<string, Tier> tiers, Dictionary<string, Recipe> recipes)
        {
            ItemDrop.ItemData.SharedData shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
            string name = Localized(shared.m_name) ?? prefab.name, group = ItemGroups.Of(shared);
            Tier? tier = tiers.TryGetValue(prefab.name, out Tier found) ? found : (Tier?)null;
            int rank = ItemGroups.Rank(group);
            return new Entry
            {
                Prefab = prefab.name, Name = name, Group = group, GroupRank = rank < 0 ? int.MaxValue : rank, Tier = tier,
                Station = Station(recipes.TryGetValue(prefab.name, out Recipe recipe) ? recipe : null),
                Text = $"{name} {prefab.name} {ItemGroups.Words(group)} {tier?.Biome}".ToLowerInvariant(),
            };
        }

        // "Forge level 2", "Workbench", "By hand"; null for an item no recipe makes.
        private static string Station(Recipe recipe)
        {
            if (recipe == null) return null;
            if (!recipe.m_craftingStation) return "By hand";
            string station = Localized(recipe.m_craftingStation.m_name) ?? recipe.m_craftingStation.name;
            return recipe.m_minStationLevel > 1 ? $"{station} level {recipe.m_minStationLevel}" : station;
        }

        private static string Localized(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            string text = Localization.instance != null ? Localization.instance.Localize(token) : token;
            return string.IsNullOrEmpty(text) || text.Contains("$") ? null : text;
        }
    }
}
