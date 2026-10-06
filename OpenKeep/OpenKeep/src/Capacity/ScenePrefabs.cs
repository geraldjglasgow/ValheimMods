using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// One walk over the network scene's prefabs for both lists the Capacity module and the documentation use: the
    /// container prefabs (<see cref="ContainerPrefabs"/>) and the station prefabs (<see cref="StationPrefabs"/>). Kept
    /// while the scene is the same and its prefab list has not grown (a mod registering prefabs later), so a world load
    /// walks the few thousand prefabs once instead of once per list and per user (the templates, the sizes, the caps,
    /// the documentation files). The tables are shared and handed out read-only.
    /// </summary>
    internal static class ScenePrefabs
    {
        private static readonly Dictionary<string, Container> containers = new Dictionary<string, Container>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Smelter> stations = new Dictionary<string, Smelter>(StringComparer.OrdinalIgnoreCase);
        private static ZNetScene walkedScene;
        private static int walkedCount = -1;

        public static IReadOnlyDictionary<string, Container> Containers
        {
            get
            {
                Walk();
                return containers;
            }
        }

        public static IReadOnlyDictionary<string, Smelter> Stations
        {
            get
            {
                Walk();
                return stations;
            }
        }

        private static void Walk()
        {
            ZNetScene scene = ZNetScene.instance;
            int count = scene != null && scene.m_prefabs != null ? scene.m_prefabs.Count : -1;
            if (ReferenceEquals(scene, walkedScene) && count == walkedCount)
                return;
            containers.Clear();
            stations.Clear();
            walkedScene = scene;
            walkedCount = count;
            if (count > 0)
            {
                foreach (GameObject prefab in scene.m_prefabs)
                    Add(prefab);
            }
        }

        /// <summary>A Container or Smelter on the prefab or on a child (ships and carts keep their storage on a child).</summary>
        private static void Add(GameObject prefab)
        {
            if (prefab == null)
                return;
            Container container = prefab.GetComponentInChildren<Container>(true);
            if (container != null && !containers.ContainsKey(prefab.name))
                containers[prefab.name] = container;
            Smelter station = prefab.GetComponentInChildren<Smelter>(true);
            if (station != null && !stations.ContainsKey(prefab.name))
                stations[prefab.name] = station;
        }
    }
}
