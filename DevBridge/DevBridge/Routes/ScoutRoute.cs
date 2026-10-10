using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/scout: where things are in this world, for choosing a place to film or test.</summary>
    internal static class ScoutRoute
    {
        internal static void Register(Router router) => router.Add("/scout",
            "/scout?location=<prefab text>|biome=<Meadows|BlackForest|Swamp|Mountain|Plains|Ocean|Mistlands|AshLands|DeepNorth>\n" +
            "       &near=x,z&radius=3000&step=48&min=&max=&limit=10\n" +
            "                       the world's generated locations whose prefab contains the text (Crypt, TrollCave, ...), or\n" +
            "                       sampled points of a biome with their ground height (min=/max= keep heights; Ocean max=10 is\n" +
            "                       deep water), nearest first from the player or near=",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (!ZoneSystem.instance || WorldGenerator.instance == null) throw new BridgeException("no world loaded");
            Vector2 near = Near(request.Get("near"));
            int limit = request.Int("limit", 10);
            if (request.Has("component")) request.Json(Components(request.Get("component"), limit));
            else if (request.Has("location")) request.Json(Locations(request.Get("location"), near, limit));
            else if (request.Has("biome")) request.Json(Biome(request, near, limit));
            else throw new BridgeException("give location=<text> or biome=<name>");
        }

        private static Vector2 Near(string spec)
        {
            if (spec != null) { float[] n = Fmt.Numbers(spec, 2, "near"); return new Vector2(n[0], n[1]); }
            Player player = Player.m_localPlayer;
            return player ? new Vector2(player.transform.position.x, player.transform.position.z) : Vector2.zero;
        }

        private static List<Dictionary<string, object>> Locations(string text, Vector2 near, int limit) =>
            ZoneSystem.instance.m_locationInstances.Values
                .Where(l => l.m_location != null && l.m_location.m_prefabName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(l => (l, d: Vector2.Distance(near, new Vector2(l.m_position.x, l.m_position.z))))
                .OrderBy(x => x.d).Take(limit)
                .Select(x => new Dictionary<string, object>
                {
                    ["prefab"] = x.l.m_location.m_prefabName, ["position"] = Fmt.V3(x.l.m_position), ["distance"] = Mathf.RoundToInt(x.d), ["placed"] = x.l.m_placed,
                }).ToList();

        /// <summary>Live scene objects carrying a component type (by short or full name), with their path and place.</summary>
        private static List<Dictionary<string, object>> Components(string typeName, int limit)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetTypes().FirstOrDefault(t => t.Name == typeName || t.FullName == typeName))
                .FirstOrDefault(t => t != null && typeof(Component).IsAssignableFrom(t)) ?? throw new BridgeException($"no component type {typeName}");
            return UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None).Cast<Component>().Take(limit)
                .Select(c => new Dictionary<string, object> { ["path"] = Path(c.transform), ["position"] = Fmt.V3(c.transform.position), ["active"] = c.gameObject.activeInHierarchy })
                .ToList();
        }

        private static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;

        private static List<Dictionary<string, object>> Biome(BridgeRequest request, Vector2 near, int limit)
        {
            Heightmap.Biome biome = (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), request.Get("biome"), true);
            float radius = request.Float("radius", 3000f), step = Mathf.Max(8f, request.Float("step", 48f));
            float min = request.Float("min", float.MinValue), max = request.Float("max", float.MaxValue);
            var found = new List<(Vector3 p, float d)>();
            for (float x = -radius; x <= radius; x += step)
                for (float z = -radius; z <= radius; z += step)
                {
                    float wx = near.x + x, wz = near.y + z, d = Mathf.Sqrt(x * x + z * z);
                    if (d > radius || WorldGenerator.instance.GetBiome(wx, wz) != biome) continue;
                    float h = WorldGenerator.instance.GetHeight(wx, wz);
                    if (h >= min && h <= max) found.Add((new Vector3(wx, h, wz), d));
                }
            return found.OrderBy(f => f.d).Take(limit)
                .Select(f => new Dictionary<string, object> { ["position"] = Fmt.V3(f.p), ["distance"] = Mathf.RoundToInt(f.d) }).ToList();
        }
    }
}
