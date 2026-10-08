using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Core
{
    /// <summary>
    /// The pack's creatures that spawn in place of a game creature (the arsenal skeletons and the crossbowman for the
    /// Skeleton, the slinger for the Greydwarf), by prefab hash. The game's spawners cap what they spawn by name or
    /// prefab, so a stand-in must be counted as the creature it replaced (<see cref="StandInSpawnAreaPatch"/>,
    /// <see cref="StandInWildCountPatch"/>): uncounted, a bone pile or greydwarf nest keeps spawning until the room is
    /// full, and a wild spawn passes its cap. Registered once each when built, identically on every peer.
    /// </summary>
    public static class StandIns
    {
        /// <summary>A stand-in's hash to the hashes of the game creatures it may replace.</summary>
        private static readonly Dictionary<int, List<int>> replaces = new Dictionary<int, List<int>>();

        /// <summary>A game creature's hash to the hashes of its stand-ins.</summary>
        private static readonly Dictionary<int, List<int>> standIns = new Dictionary<int, List<int>>();

        public static void Add(GameObject? standIn, params string[] games)
        {
            if (standIn == null)
            {
                return;
            }
            int hash = standIn.name.GetStableHashCode();
            foreach (string game in games)
            {
                AddOnce(replaces, hash, game.GetStableHashCode());
                AddOnce(standIns, game.GetStableHashCode(), hash);
            }
        }

        /// <summary>Whether this creature is an untamed stand-in for one of the spawn area's own creatures.</summary>
        public static bool CountsFor(SpawnArea area, GameObject creature)
        {
            ZNetView view = creature.GetComponent<ZNetView>();
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null || !replaces.TryGetValue(zdo.GetPrefab(), out List<int> games))
            {
                return false;
            }
            Character character = creature.GetComponent<Character>();
            if (character != null && character.IsTamed())
            {
                return false;
            }
            foreach (SpawnArea.SpawnData data in area.m_prefabs)
            {
                if (data.m_prefab != null && games.Contains(data.m_prefab.name.GetStableHashCode()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The stand-ins for this game creature among the objects, event creatures only when asked.</summary>
        public static int Count(GameObject prefab, List<ZDO> zdos, bool eventCreaturesOnly)
        {
            if (standIns.Count == 0 || !standIns.TryGetValue(prefab.name.GetStableHashCode(), out List<int> theirs))
            {
                return 0;
            }
            int count = 0;
            foreach (ZDO zdo in zdos)
            {
                if (theirs.Contains(zdo.GetPrefab()) && (!eventCreaturesOnly || zdo.GetBool(ZDOVars.s_eventCreature)))
                {
                    count++;
                }
            }
            return count;
        }

        private static void AddOnce(Dictionary<int, List<int>> map, int key, int value)
        {
            if (!map.TryGetValue(key, out List<int> values))
            {
                map[key] = values = new List<int>();
            }
            if (!values.Contains(value))
            {
                values.Add(value);
            }
        }
    }
}
