using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// A quiet set: natural spawning off (world spawns, spawner points, spawn areas, raids) so only the director puts
    /// creatures in the world, and a purge of what is lying about, loaded or not: tombstones, items on the ground, wild
    /// creatures (tamed ones and the cast are kept).
    /// </summary>
    internal static class WorldTidy
    {
        internal static bool Quiet { get; set; }

        internal static Dictionary<string, int> Purge(IEnumerable<string> kinds)
        {
            var removed = new Dictionary<string, int>();
            foreach (string kind in kinds)
                removed[kind] = Remove(Test(kind.Trim().ToLowerInvariant()));
            return removed;
        }

        /// <summary>Clears a strip for a shot: loaded rocks, bushes, trees, logs, pickables and loose items within radius.</summary>
        internal static int ClearAround(Vector3 centre, float radius)
        {
            ZNetScene scene = ZNetScene.instance ? ZNetScene.instance : throw new BridgeException("no world loaded");
            List<ZNetView> doomed = scene.m_instances.Values
                .Where(v => v && Vector3.Distance(Flat(v.transform.position), Flat(centre)) <= radius && Clutter(v.gameObject)).ToList();
            foreach (ZNetView view in doomed)
            {
                view.GetZDO()?.SetOwner(ZDOMan.GetSessionID());
                scene.Destroy(view.gameObject);
            }
            return doomed.Count;
        }

        private static bool Clutter(GameObject go) =>
            !Cast.IsActor(go) && !go.GetComponent<Character>() && !go.GetComponent<Piece>() &&
            (go.GetComponent<Destructible>() || go.GetComponent<MineRock>() || go.GetComponent<MineRock5>() || go.GetComponent<TreeBase>()
             || go.GetComponent<TreeLog>() || go.GetComponent<Pickable>() || go.GetComponent<ItemDrop>());

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static Func<ZDO, GameObject, bool> Test(string kind)
        {
            switch (kind)
            {
                case "tombstones": return (zdo, prefab) => prefab.GetComponent<TombStone>();
                case "items": return (zdo, prefab) => prefab.GetComponent<ItemDrop>() && !prefab.GetComponent<Character>();
                case "creatures": return (zdo, prefab) => Wild(zdo, prefab);
                case "extras": return (zdo, prefab) => Extra(zdo, prefab);
                default: throw new BridgeException($"purge takes tombstones, items, creatures, extras; not {kind}");
            }
        }

        private static bool Wild(ZDO zdo, GameObject prefab)
        {
            Character character = prefab.GetComponent<Character>();
            if (!character || character is Player || zdo.GetBool(ZDOVars.s_tamed)) return false;
            ZNetView view = ZNetScene.instance.FindInstance(zdo);
            return !view || !Cast.IsActor(view.gameObject);
        }

        /// <summary>A player object that is not the local player's and not in the cast: an extra left by an earlier take.</summary>
        private static bool Extra(ZDO zdo, GameObject prefab)
        {
            if (!prefab.GetComponent<Player>()) return false;
            ZNetView view = ZNetScene.instance.FindInstance(zdo);
            return view && view.gameObject != (Player.m_localPlayer ? Player.m_localPlayer.gameObject : null) && !Cast.IsActor(view.gameObject)
                && view.GetZDO().GetLong(ZDOVars.s_playerID) == 0L;
        }

        /// <summary>Destroys every ZDO the test picks: loaded ones through the scene, the rest by taking and dropping them.</summary>
        private static int Remove(Func<ZDO, GameObject, bool> test)
        {
            ZNetScene scene = ZNetScene.instance ? ZNetScene.instance : throw new BridgeException("no world loaded");
            List<ZDO> doomed = ZDOMan.instance.m_objectsByID.Values.Where(zdo => Picks(scene, zdo, test)).ToList();
            foreach (ZDO zdo in doomed)
            {
                zdo.SetOwner(ZDOMan.GetSessionID());
                ZNetView view = scene.FindInstance(zdo);
                if (view) scene.Destroy(view.gameObject);
                else ZDOMan.instance.DestroyZDO(zdo);
            }
            return doomed.Count;
        }

        private static bool Picks(ZNetScene scene, ZDO zdo, Func<ZDO, GameObject, bool> test)
        {
            GameObject prefab = scene.GetPrefab(zdo.GetPrefab());
            return prefab && test(zdo, prefab);
        }
    }

    [HarmonyPatch(typeof(SpawnSystem), "UpdateSpawning")]
    internal static class QuietWorldSpawns
    {
        private static bool Prefix() => !WorldTidy.Quiet;
    }

    [HarmonyPatch(typeof(CreatureSpawner), "UpdateSpawner")]
    internal static class QuietSpawnerPoints
    {
        private static bool Prefix() => !WorldTidy.Quiet;
    }

    [HarmonyPatch(typeof(SpawnArea), "UpdateSpawn")]
    internal static class QuietSpawnAreas
    {
        private static bool Prefix() => !WorldTidy.Quiet;
    }

    [HarmonyPatch(typeof(RandEventSystem), "UpdateRandomEvent")]
    internal static class QuietRaids
    {
        private static bool Prefix() => !WorldTidy.Quiet;
    }
}
