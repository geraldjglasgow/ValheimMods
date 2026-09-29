using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Routes
{
    /// <summary>/bundle: load a workshop asset bundle from its file, list what is in it, reload it after a rebuild, unload it.</summary>
    internal static class BundleRoute
    {
        internal static void Register(Router router) => router.Add("/bundle",
            "/bundle                 the bundles loaded from files, with their assets counted by type\n" +
            "/bundle?load=<file>     load an asset bundle (e.g. .../AssetWorkshop/out/bundles/ecp_swamp.windows; a folder or a path\n" +
            "                       without .windows finds the Windows build); loading a file already loaded reloads it\n" +
            "/bundle?reload=<name>   after a rebuild: destroy what was placed from it, unload it with everything loaded from it,\n" +
            "                       read the file again and put each placed asset back (same place, id and dress)\n" +
            "/bundle?unload=<name>|all   remove what was placed from it, then unload it\n" +
            "/bundle?assets=<name>&type=GameObject&filter=text   its assets by type: triangles, textures, clip lengths",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (request.Has("load")) request.Json(Load(request.Require("load")));
            else if (request.Has("reload")) request.Json(Reload(Bundles.Get(request.Require("reload"))));
            else if (request.Get("unload") == "all") request.Json(Bundles.All.Select(Reloads.Unload).ToList());
            else if (request.Has("unload")) request.Json(Reloads.Unload(Bundles.Get(request.Require("unload"))));
            else if (request.Has("assets")) request.Json(Assets(Bundles.Get(request.Require("assets")), request.Get("type"), request.Get("filter")));
            else request.Json(Bundles.All.Select(Summary).ToList());
        }

        private static Dictionary<string, object> Load(string file)
        {
            LoadedBundle already = Bundles.FromFile(file);
            if (already != null) return Reload(already);
            LoadedBundle loaded = Bundles.Load(file);
            Dictionary<string, object> summary = Summary(loaded);
            summary["assets"] = Assets(loaded, null, null);
            return summary;
        }

        private static Dictionary<string, object> Reload(LoadedBundle loaded)
        {
            Dictionary<string, object> result = Reloads.Reload(loaded);
            foreach (KeyValuePair<string, object> pair in Summary(loaded)) result[pair.Key] = pair.Value;
            result["reloaded"] = true;
            return result;
        }

        private static Dictionary<string, object> Summary(LoadedBundle loaded) => new Dictionary<string, object>
        {
            ["name"] = loaded.Name,
            ["file"] = loaded.Path,
            ["bytes"] = loaded.Bytes,
            ["built"] = loaded.FileTime.ToString("yyyy-MM-dd HH:mm:ss"),
            ["loaded"] = loaded.LoadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            ["counts"] = loaded.Bundle.LoadAllAssets().GroupBy(a => a.GetType().Name).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => g.Count()),
            ["placed"] = Placements.All.Count(p => string.Equals(p.Bundle, loaded.Name, StringComparison.OrdinalIgnoreCase)),
        };

        /// <summary>Asset lines grouped by type name, optionally one type or names containing the filter.</summary>
        private static SortedDictionary<string, List<string>> Assets(LoadedBundle loaded, string type, string filter)
        {
            IEnumerable<Object> assets = loaded.Bundle.LoadAllAssets()
                .Where(a => type == null || string.Equals(a.GetType().Name, type, StringComparison.OrdinalIgnoreCase))
                .Where(a => filter == null || a.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            var grouped = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (IGrouping<string, Object> group in assets.GroupBy(a => a.GetType().Name))
                grouped[group.Key] = group.OrderBy(a => a.name).Select(AssetInfo.Line).ToList();
            return grouped;
        }
    }
}
