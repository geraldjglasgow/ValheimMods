using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Swamp
{
    /// <summary>Zone-owner vanilla spawn entries. No extra client spawns, crypt replacements or boss progression keys.</summary>
    [HarmonyPatch(typeof(SpawnSystem), "Awake")]
    public static class SwampSpawns
    {
        private static SpawnSystemList? list;
        private static readonly Dictionary<SwampKind, SpawnSystem.SpawnData> Entries = new Dictionary<SwampKind, SpawnSystem.SpawnData>();

        public static void Refresh()
        {
            foreach (SwampKind kind in SwampKind.All)
            {
                if (!Entries.TryGetValue(kind, out SpawnSystem.SpawnData entry))
                    Entries[kind] = entry = Make(kind);
                SwampSettings settings = SwampSettings.For(kind);
                entry.m_prefab = kind.Prefab;
                entry.m_enabled = settings.Enabled && kind.Prefab != null;
                entry.m_spawnChance = settings.Chance;
                entry.m_spawnInterval = settings.Interval;
            }
            if (list != null)
            {
                list.m_spawners.Clear();
                list.m_spawners.AddRange(Entries.Values);
            }
        }

        private static SpawnSystem.SpawnData Make(SwampKind kind) => new SpawnSystem.SpawnData
        {
            m_name = kind.Creature, m_biome = Heightmap.Biome.Swamp,
            m_biomeArea = Heightmap.BiomeArea.Median, m_maxSpawned = kind.Jarl ? 1 : 2,
            m_spawnDistance = kind.Jarl ? 180f : 45f, m_groupSizeMin = 1, m_groupSizeMax = 1,
            m_minLevel = 1, m_maxLevel = kind.Jarl ? 1 : 2,
            m_spawnAtDay = !kind.Night, m_spawnAtNight = true,
            m_huntPlayer = false, m_groundOffset = kind.Night ? 1.5f : 0f,
        };

        public static void Join(SpawnSystem system)
        {
            if (list == null)
            {
                var holder = new GameObject("ECP_Swamp_spawns");
                holder.transform.SetParent(PrefabBench.Root, false);
                list = holder.AddComponent<SpawnSystemList>();
                list.m_spawners.AddRange(Entries.Values);
            }
            if (!system.m_spawnLists.Contains(list)) system.m_spawnLists.Add(list);
        }

        private static void Postfix(SpawnSystem __instance) => SafeCall.Run("swamp spawn list", () => Join(__instance));
    }
}
