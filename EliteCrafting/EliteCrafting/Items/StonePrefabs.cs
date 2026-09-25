using System;
using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Items
{
    /// <summary>
    /// The stone prefabs: the 43 built-in ones (27 stones and sigils, 16 essences), the 16 reserved <c>ECF_CustomNN</c>
    /// (prefabs.md sections 1-3, 5) and the 5 salvage shards (salvage.md section 5, items that are not stones).
    /// Built once per process from code alone, whatever the YAML says, so every peer (server, host, clients, main
    /// menu) has the same prefab names and hashes before any inventory or ZDO arrives. Registration into the game's
    /// databases is done by <see cref="StoneRegistrationPatches"/>; this class is the registry other areas read.
    /// </summary>
    public static class StonePrefabs
    {
        private static readonly List<StoneEntry> Entries = new List<StoneEntry>();
        private static readonly Dictionary<string, StoneEntry> ByName = new Dictionary<string, StoneEntry>(StringComparer.Ordinal);
        private static readonly Dictionary<GameObject, StoneEntry> ByObject = new Dictionary<GameObject, StoneEntry>();
        private static GameObject? _holder;

        /// <summary>Raised once, right after the prefabs were built (every peer). The Items area applies the rules to them.</summary>
        internal static event Action? Built;

        public static bool IsBuilt => Entries.Count > 0;

        /// <summary>Whether a prefab of this name is one of ours (and has been built).</summary>
        public static bool IsRegistered(string? prefabName) => prefabName != null && ByName.ContainsKey(prefabName);

        /// <summary>
        /// The prefab of a stone id: the running definition's prefab (built-in or reserved), else the built-in
        /// prefab for a built-in id. Null for an unknown id or before the prefabs exist.
        /// </summary>
        public static GameObject? Get(string? stoneId)
        {
            if (stoneId == null)
            {
                return null;
            }
            string? prefab = ActiveRules.Current.Economy.Stone(stoneId)?.Prefab;
            if (prefab == null && StoneCatalog.IsBuiltIn(stoneId))
            {
                prefab = StoneCatalog.PrefabFor(stoneId);
            }
            return prefab != null && ByName.TryGetValue(prefab, out StoneEntry entry) ? entry.Prefab : null;
        }

        /// <summary>The prefab of a shard id (<c>shard_ascension</c> → <c>ECF_ShardAscension</c>), or null.</summary>
        public static GameObject? GetShard(string? shardId) =>
            StoneCatalog.IsShard(shardId) ? GetByPrefabName(StoneCatalog.PrefabFor(shardId!)) : null;

        /// <summary>
        /// Whether the prefab is one of the stone prefabs (built-in or reserved) - a shard is registered here too but
        /// is no stone: it has no definition and no verb, and the stone click take-over leaves it to the game.
        /// </summary>
        public static bool IsStonePrefab(string? prefabName) =>
            prefabName != null && ByName.TryGetValue(prefabName, out StoneEntry entry) && !entry.IsShard;

        /// <summary>The prefab by its name (<c>ECF_Awakening</c>, <c>ECF_Custom03</c>, <c>ECF_ShardAscension</c>), or null.</summary>
        public static GameObject? GetByPrefabName(string? prefabName) =>
            prefabName != null && ByName.TryGetValue(prefabName, out StoneEntry entry) ? entry.Prefab : null;

        internal static IReadOnlyList<StoneEntry> All => Entries;

        internal static StoneEntry? EntryOf(GameObject? prefab) =>
            prefab != null && ByObject.TryGetValue(prefab, out StoneEntry entry) ? entry : null;

        /// <summary>
        /// Builds every prefab the first time a base item is reachable in either list (the object database's items or
        /// the net scene's prefabs). False while no base exists yet (the main menu's first, empty ObjectDB Awake).
        /// </summary>
        internal static bool EnsureBuilt(IList<GameObject>? first, IList<GameObject>? second)
        {
            if (IsBuilt)
            {
                return true;
            }
            if (!StoneBases.AnyPresent(first, second))
            {
                return false;
            }
            BuildAll(first, second);
            Log.Info($"{Entries.Count} stone prefabs built");
            Built?.Invoke();
            return true;
        }

        private static void BuildAll(IList<GameObject>? first, IList<GameObject>? second)
        {
            _holder = new GameObject("EliteCrafting_StonePrefabs");
            _holder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(_holder);
            GameObject?[] bases = ResolveBases(first, second);
            foreach (string id in StoneCatalog.BuiltInIds)
            {
                Add(new StoneEntry(StoneCatalog.PrefabFor(id), id, StoneBases.GroupOf(id)), bases);
            }
            for (int n = 1; n <= StoneCatalog.CustomPrefabCount; n++)
            {
                Add(new StoneEntry(StoneCatalog.CustomPrefab(n), null, StoneBases.GroupOfCustom(n)), bases);
            }
            foreach (string id in StoneCatalog.ShardIds)
            {
                Add(new StoneEntry(StoneCatalog.PrefabFor(id), null, StoneBases.GroupOf(id)) { ShardId = id }, bases);
            }
        }

        // One lookup (and at most one warning) per group, not per stone.
        private static GameObject?[] ResolveBases(IList<GameObject>? first, IList<GameObject>? second)
        {
            StoneGroup[] groups = (StoneGroup[])Enum.GetValues(typeof(StoneGroup));
            GameObject?[] bases = new GameObject?[groups.Length];
            foreach (StoneGroup group in groups)
            {
                bases[(int)group] = StoneBases.Resolve(group, first, second);
            }
            return bases;
        }

        private static void Add(StoneEntry entry, GameObject?[] bases)
        {
            GameObject? basePrefab = bases[(int)entry.Group];
            if (basePrefab == null)
            {
                Log.Error($"no base item for stone prefab {entry.PrefabName}; it is not registered");
                return;
            }
            StoneCloner.Build(entry, basePrefab, _holder!.transform);
            Entries.Add(entry);
            ByName[entry.PrefabName] = entry;
            ByObject[entry.Prefab] = entry;
        }

        /// <summary>Appends every stone prefab whose name the list does not hold yet. True when anything was added.</summary>
        internal static bool AddMissing(List<GameObject>? list)
        {
            if (list == null || Entries.Count == 0)
            {
                return false;
            }
            HashSet<string> present = new HashSet<string>(StringComparer.Ordinal);
            foreach (GameObject go in list)
            {
                if (go != null)
                {
                    present.Add(go.name);
                }
            }
            int before = list.Count;
            foreach (StoneEntry entry in Entries)
            {
                if (!present.Contains(entry.PrefabName))
                {
                    list.Add(entry.Prefab);
                }
            }
            return list.Count != before;
        }

        /// <summary>
        /// Points a freshly woken stone item at its prefab's shared data (ItemDrop.Awake, every peer). Outside the editor
        /// every instantiated item gets its own copy of the shared data; linking them all to the prefab's one
        /// instance means a YAML change (name, stack, weight, icon) reaches every existing stack at once.
        /// </summary>
        internal static void LinkShared(ItemDrop.ItemData? item)
        {
            if (Entries.Count == 0 || item?.m_dropPrefab == null)
            {
                return;
            }
            if (ByObject.TryGetValue(item.m_dropPrefab, out StoneEntry entry))
            {
                item.m_shared = entry.Shared;
            }
        }
    }
}
