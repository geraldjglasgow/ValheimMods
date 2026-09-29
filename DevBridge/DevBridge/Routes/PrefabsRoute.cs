using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/prefabs: find the game's own prefabs by name, to line up against or to borrow a look, effect or sound from.</summary>
    internal static class PrefabsRoute
    {
        internal static void Register(Router router) => router.Add("/prefabs",
            "/prefabs?filter=text&kind=creature|piece|item|sfx|vfx|other&limit=80\n" +
            "                       the game's prefabs (networked, local and items) whose name contains the text, with their kind",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            string filter = request.Get("filter"), kind = request.Get("kind");
            if (filter == null && kind == null) throw new BridgeException("give filter=<name text> and/or kind=");
            List<string> found = GamePrefabs.All()
                .Where(p => filter == null || p.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(p => new { p.name, Kind = GamePrefabs.Kind(p) })
                .Where(p => kind == null || string.Equals(p.Kind, kind, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.name, StringComparer.OrdinalIgnoreCase).Select(p => $"{p.name}  {p.Kind}").ToList();
            int limit = request.Int("limit", 80);
            string more = found.Count > limit ? $"\n... {found.Count - limit} more, narrow the filter or raise limit=" : "";
            request.Text(found.Count == 0 ? "(no prefab matches)" : string.Join("\n", found.Take(limit)) + more);
        }
    }
}
