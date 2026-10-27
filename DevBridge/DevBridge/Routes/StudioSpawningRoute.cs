using DevBridge.Server;
using DevBridge.Studio;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>
    /// The studio's Spawning tab (also usable from curl): find any networked prefab by the name shown in game or its
    /// prefab name, put copies in front of the player, and take the last spawn back.
    /// </summary>
    internal static class StudioSpawningRoute
    {
        private const string Pad = "\n                       ";

        internal static void Register(Router router)
        {
            router.Add("/studio/catalog", "/studio/catalog?filter=troll&most=300" + Pad +
                "the networked prefabs (creatures, items, pieces, plants, rocks, spawners, effects) whose shown name, prefab" + Pad +
                "or group has every word, in groups, with what a copy can take (stars, taming, stack, item level)", Catalog);
            router.Add("/studio/spawn/world", "/studio/spawn/world?prefab=Troll&amount=1&level=1&tamed=0&distance=3" + Pad +
                "copies in front of you, facing you: creatures at a level (stars + 1, up to 10), tamed or not; items" + Pad +
                "an amount in stacks at an item level; at most 50 copies", Spawn);
            router.Add("/studio/spawn/undo", "/studio/spawn/undo     remove the copies of the last /studio/spawn/world still there (20 kept)",
                request => request.Json(WorldSpawn.Undo()));
        }

        private static void Catalog(BridgeRequest request) =>
            request.Json(SpawnCatalog.Search(request.Get("filter"), Mathf.Clamp(request.Int("most", 300), 1, 3000)));

        private static void Spawn(BridgeRequest request) => request.Json(WorldSpawn.Spawn(new WorldSpawn.Order
        {
            Prefab = request.Require("prefab"),
            Amount = request.Int("amount", 1),
            Level = request.Int("level", 1),
            Tamed = request.Flag("tamed"),
            Distance = request.Float("distance", 3f),
        }));
    }
}
