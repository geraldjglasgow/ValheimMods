using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Which objects a kept place holds: those that neither move nor think - building pieces, trees, rocks, pickables,
    /// locations, portals, terrain compilers. Left out is whatever moves (anything with a synced transform, creatures and
    /// players, items on the ground, ships, carts, projectiles, ragdolls, fish, birds, floating things, tombstones,
    /// felled logs, the leviathan) and whatever makes things on its own (zone spawners, spawn points, nests, trigger
    /// spawners, boss altars, wisp, cinder and loot spawners), so a kept place spawns nothing and its creatures unload as
    /// today. Decided once per prefab, by its components, so another mod's objects count too; a prefab the game does not
    /// know is never held (the game would destroy its object as invalid).
    /// </summary>
    internal static class StillKinds
    {
        private static readonly Dictionary<int, bool> Kinds = new Dictionary<int, bool>();

        public static bool Is(ZDO zdo)
        {
            if (ZNetScene.instance == null)
            {
                return false;
            }
            int prefab = zdo.GetPrefab();
            if (!Kinds.TryGetValue(prefab, out bool still))
            {
                GameObject go = ZNetScene.instance.GetPrefab(prefab);
                still = go != null && !Moves(go) && !Spawns(go);
                Kinds[prefab] = still;
            }
            return still;
        }

        private static bool Moves(GameObject go)
        {
            return go.GetComponent<ZSyncTransform>() != null || go.GetComponent<Character>() != null
                || go.GetComponent<BaseAI>() != null || go.GetComponent<ItemDrop>() != null
                || go.GetComponent<Ship>() != null || go.GetComponent<Vagon>() != null
                || go.GetComponent<Projectile>() != null || go.GetComponent<Ragdoll>() != null
                || go.GetComponent<Fish>() != null || go.GetComponent<RandomFlyingBird>() != null
                || go.GetComponent<Floating>() != null || go.GetComponent<TombStone>() != null
                || go.GetComponent<TreeLog>() != null || go.GetComponent<Leviathan>() != null;
        }

        private static bool Spawns(GameObject go)
        {
            return go.GetComponent<SpawnSystem>() != null || go.GetComponent<CreatureSpawner>() != null
                || go.GetComponent<SpawnArea>() != null || go.GetComponent<TriggerSpawner>() != null
                || go.GetComponent<OfferingBowl>() != null || go.GetComponent<WispSpawner>() != null
                || go.GetComponent<CinderSpawner>() != null || go.GetComponent<LootSpawner>() != null;
        }
    }
}
