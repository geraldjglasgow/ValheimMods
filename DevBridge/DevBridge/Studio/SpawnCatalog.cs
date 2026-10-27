using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// What the studio's Spawning tab can put in the world: every networked prefab the game knows (the game's and mods'),
    /// but players and the game's own helpers (names starting "_"), with the name shown in game, its group (Bosses,
    /// Creatures, Items, Pieces, Plants and pickables, Trees and rocks, Spawners, Effects, Other) and what can be set on a
    /// copy (stars, taming, stack, item level). A search is words, each found in the shown name, the prefab or the group.
    /// Worked out once per world and prefab count.
    /// </summary>
    internal static class SpawnCatalog
    {
        internal static readonly string[] Groups =
            { "Bosses", "Creatures", "Items", "Pieces", "Plants and pickables", "Trees and rocks", "Spawners", "Effects", "Other" };

        internal sealed class Entry
        {
            internal GameObject Prefab;
            internal string Name;
            internal string Group;
            internal string Text;
        }

        private static List<Entry> entries;
        private static ZNetScene builtFor;
        private static int builtCount;

        internal static Dictionary<string, object> Search(string filter, int most)
        {
            string[] words = (filter ?? "").ToLowerInvariant().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string whole = string.Join(" ", words);
            List<Entry> found = Entries().Where(entry => words.All(entry.Text.Contains)).ToList();
            List<Entry> shown = found.OrderBy(e => Array.IndexOf(Groups, e.Group)).ThenBy(e => Closeness(e, whole))
                .ThenBy(e => e.Name ?? e.Prefab.name, StringComparer.OrdinalIgnoreCase).Take(most).ToList();
            return new Dictionary<string, object>
            {
                ["found"] = found.Count,
                ["shown"] = shown.Count,
                ["groups"] = shown.GroupBy(e => e.Group).Select(g => new Dictionary<string, object>
                {
                    ["name"] = g.Key, ["items"] = g.Select(Line).ToList(),
                }).ToList(),
            };
        }

        // The prefab or shown name the search is, then one starting with it, then the rest.
        private static int Closeness(Entry entry, string whole)
        {
            string prefab = entry.Prefab.name.ToLowerInvariant(), name = (entry.Name ?? "").ToLowerInvariant();
            if (prefab == whole || name == whole) return 0;
            return prefab.StartsWith(whole) || name.StartsWith(whole) ? 1 : 2;
        }

        private static Dictionary<string, object> Line(Entry entry)
        {
            GameObject prefab = entry.Prefab;
            ItemDrop item = prefab.GetComponent<ItemDrop>();
            var line = new Dictionary<string, object> { ["prefab"] = prefab.name, ["name"] = entry.Name, ["group"] = entry.Group };
            if (prefab.GetComponent<Character>()) line["creature"] = true;
            if (prefab.GetComponent<Tameable>()) line["tameable"] = true;
            if (item) line["stack"] = Mathf.Max(1, item.m_itemData.m_shared.m_maxStackSize);
            if (item) line["levels"] = Mathf.Max(1, item.m_itemData.m_shared.m_maxQuality);
            return line;
        }

        internal static List<Entry> Entries()
        {
            ZNetScene scene = ZNetScene.instance;
            if (!scene || !ObjectDB.instance) throw new BridgeException("no world loaded: spawning lists the world's prefabs");
            if (entries != null && builtFor == scene && builtCount == scene.m_prefabs.Count) return entries;
            entries = scene.m_prefabs.Where(Listed).Distinct().Select(Make).ToList();
            (builtFor, builtCount) = (scene, scene.m_prefabs.Count);
            return entries;
        }

        private static bool Listed(GameObject prefab) =>
            prefab && !prefab.name.StartsWith("_") && !prefab.GetComponent<Player>() && prefab.GetComponent<ZNetView>();

        private static Entry Make(GameObject prefab)
        {
            string name = ShownName(prefab), group = GroupOf(prefab);
            return new Entry { Prefab = prefab, Name = name, Group = group, Text = $"{name} {prefab.name} {group}".ToLowerInvariant() };
        }

        private static string GroupOf(GameObject prefab)
        {
            Character character = prefab.GetComponent<Character>();
            if (character) return character.m_boss ? "Bosses" : "Creatures";
            if (prefab.GetComponent<ItemDrop>()) return "Items";
            if (prefab.GetComponent<Piece>()) return "Pieces";
            if (prefab.GetComponent<Pickable>() || prefab.GetComponent<Plant>() || prefab.GetComponent<PickableItem>()) return "Plants and pickables";
            if (prefab.GetComponent<TreeBase>() || prefab.GetComponent<TreeLog>() || prefab.GetComponent<MineRock>()
                || prefab.GetComponent<MineRock5>() || prefab.GetComponent<Destructible>()) return "Trees and rocks";
            if (prefab.GetComponent<CreatureSpawner>() || prefab.GetComponent<SpawnArea>()) return "Spawners";
            return Effect(prefab.name) || Stage.GamePrefabs.Kind(prefab) == "sfx" ? "Effects" : "Other";
        }

        private static bool Effect(string name) =>
            new[] { "vfx_", "sfx_", "fx_" }.Any(start => name.StartsWith(start, StringComparison.OrdinalIgnoreCase));

        // The name the game shows: a creature's, an item's or a piece's, else the first m_name (or a pickable's item).
        private static string ShownName(GameObject prefab)
        {
            ItemDrop item = prefab.GetComponent<ItemDrop>();
            string token = prefab.GetComponent<Character>()?.m_name ?? item?.m_itemData.m_shared.m_name ?? prefab.GetComponent<Piece>()?.m_name;
            token = string.IsNullOrEmpty(token) ? AnyName(prefab) : token;
            Pickable pickable = prefab.GetComponent<Pickable>();
            if (string.IsNullOrEmpty(token) && pickable && pickable.m_itemPrefab)
                token = pickable.m_itemPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name;
            return Localized(token);
        }

        private static string AnyName(GameObject prefab)
        {
            foreach (MonoBehaviour behaviour in prefab.GetComponents<MonoBehaviour>())
            {
                if (!behaviour) continue;
                FieldInfo field = behaviour.GetType().GetField("m_name", BindingFlags.Public | BindingFlags.Instance)
                    ?? behaviour.GetType().GetField("m_overrideName", BindingFlags.Public | BindingFlags.Instance);
                if (field?.FieldType == typeof(string) && field.GetValue(behaviour) is string name && name.Length > 0) return name;
            }
            return null;
        }

        private static string Localized(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            string text = Localization.instance != null ? Localization.instance.Localize(token) : token;
            return string.IsNullOrEmpty(text) || text.Contains("$") || text.Contains("[") ? null : text;
        }
    }
}
