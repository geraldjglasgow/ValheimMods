using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// The station prefabs of the scene: every ZNetScene prefab with the game's Smelter component on it or on a
    /// child (smelters, kilns, blast furnaces, refineries, spinning wheels, windmills, hot tubs, the battering ram
    /// and any other mod's station built the same way), by prefab name. Found in the scene's one shared walk
    /// (<see cref="ScenePrefabs"/>), shared and read-only.
    /// </summary>
    public static class StationPrefabs
    {
        public static IReadOnlyDictionary<string, Smelter> All() => ScenePrefabs.Stations;

        /// <summary>The prefab name of a station's net object (the game's own m_nview, which may sit on a parent), without "(Clone)".</summary>
        public static string NameOf(Smelter station)
        {
            if (station == null)
                return "";
            ZNetView view = station.m_nview != null ? station.m_nview : station.GetComponentInParent<ZNetView>();
            return Utils.GetPrefabName(view != null ? view.gameObject : station.gameObject);
        }
    }
}
