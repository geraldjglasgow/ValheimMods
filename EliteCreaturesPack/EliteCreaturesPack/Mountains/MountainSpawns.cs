using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Mountains
{
    /// <summary>The vanilla zone owner performs all spawn rolls; each peer only installs the same table.</summary>
    [HarmonyPatch(typeof(SpawnSystem), "Awake")]
    public static class MountainSpawns
    {
        private static SpawnSystemList? list;
        private static readonly Dictionary<MountainKind, SpawnSystem.SpawnData> Entries = new Dictionary<MountainKind, SpawnSystem.SpawnData>();
        public static void Refresh()
        {
            foreach (MountainKind kind in MountainKind.All)
            {
                if (!Entries.TryGetValue(kind, out SpawnSystem.SpawnData entry)) Entries[kind] = entry = Make(kind);
                MountainSettings settings = MountainSettings.For(kind);
                entry.m_prefab = kind.Prefab;
                entry.m_enabled = settings.Enabled && kind.Prefab != null;
                entry.m_spawnChance = settings.Chance;
                entry.m_spawnInterval = settings.Interval;
            }
        }
        private static SpawnSystem.SpawnData Make(MountainKind kind) => new SpawnSystem.SpawnData
        {
            m_name = kind.Creature, m_biome = Heightmap.Biome.Mountain,
            m_biomeArea = Heightmap.BiomeArea.Median, m_maxSpawned = kind.Boss ? 1 : 2,
            m_spawnDistance = kind.Boss ? 300f : 60f, m_groupSizeMin = 1, m_groupSizeMax = 1,
            m_minLevel = 1, m_maxLevel = kind.Boss ? 1 : 2,
            m_spawnAtDay = !kind.Night, m_spawnAtNight = true,
            m_huntPlayer = false, m_groundOffset = kind.Flying ? 8f : 0f,
        };
        public static void Join(SpawnSystem system)
        {
            Refresh();
            if (list == null)
            {
                var holder = new GameObject("ECP_Mountain_spawns");
                holder.transform.SetParent(PrefabBench.Root, false);
                list = holder.AddComponent<SpawnSystemList>();
                list.m_spawners.AddRange(Entries.Values);
            }
            if (!system.m_spawnLists.Contains(list)) system.m_spawnLists.Add(list);
        }
        private static void Postfix(SpawnSystem __instance) => SafeCall.Run("mountain spawn list", () => Join(__instance));
    }
}
