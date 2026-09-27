using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Extras
{
    /// <summary>
    /// Tells wild pickables (berry bushes, mushrooms, thistle, branches, stones ...) from everything else. The game marks
    /// none of them, so it is read from the game's own data: wild means the world's vegetation spawns that prefab
    /// (<c>ZoneSystem.m_vegetation</c>). Pickables placed by locations (dragon eggs, core stands, remains, dvergr loot)
    /// are therefore never wild, nor are build pieces. A prefab a sapling also grows into (<c>Plant.m_grownPrefabs</c>,
    /// as magecap and jotun puffs are) counts as a crop while it stands on cultivated ground.
    /// </summary>
    public static class NaturalPickables
    {
        private static HashSet<string> grown;
        private static HashSet<string> wild;

        /// <summary>
        /// Wild pickables standing inside the footprint that are world objects of their own (with their own network
        /// object, so removing one never takes a larger object with it). Access rules are the caller's.
        /// </summary>
        public static List<Pickable> In(Footprint area)
        {
            List<Pickable> result = new List<Pickable>();
            HashSet<Pickable> seen = new HashSet<Pickable>();
            foreach (Collider collider in Physics.OverlapSphere(area.Center, area.Reach + 2f, ~0, QueryTriggerInteraction.Collide))
            {
                Pickable pickable = collider.GetComponentInParent<Pickable>();
                if (pickable == null || !seen.Add(pickable) || pickable.GetComponent<ZNetView>() == null)
                    continue;
                if (area.Contains(pickable.transform.position) && IsWild(pickable))
                    result.Add(pickable);
            }
            return result;
        }

        /// <summary>The pickable grew wild. False whenever that cannot be told (never uproots a crop by mistake).</summary>
        public static bool IsWild(Pickable pickable)
        {
            if (pickable.GetComponent<Piece>() != null || !EnsureSets())
                return false;
            string name = Utils.GetPrefabName(pickable.gameObject);
            if (!wild.Contains(name))
                return false;
            if (!grown.Contains(name))
                return true;
            Vector3 position = pickable.transform.position;
            Heightmap map = Heightmap.FindHeightmap(position);
            return map == null || !map.IsCultivated(position);
        }

        /// <summary>Reads the grown and wild prefab names once, when the net scene and the world's vegetation exist. False before that.</summary>
        private static bool EnsureSets()
        {
            if (grown != null)
                return true;
            if (ZNetScene.instance == null || ZoneSystem.instance == null || ZoneSystem.instance.m_vegetation.Count == 0)
                return false;
            wild = WildNames();
            grown = GrownNames();
            return true;
        }

        private static HashSet<string> GrownNames()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                Plant plant = prefab != null ? prefab.GetComponent<Plant>() : null;
                if (plant == null)
                    continue;
                foreach (GameObject grownPrefab in plant.m_grownPrefabs)
                {
                    if (grownPrefab != null)
                        names.Add(grownPrefab.name);
                }
            }
            return names;
        }

        private static HashSet<string> WildNames()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (ZoneSystem.ZoneVegetation vegetation in ZoneSystem.instance.m_vegetation)
            {
                if (vegetation?.m_prefab != null)
                    names.Add(vegetation.m_prefab.name);
            }
            return names;
        }
    }
}
