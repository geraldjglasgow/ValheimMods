using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevBridge.Events;
using DevBridge.Server;
using DevBridge.Stage;
using DevBridge.Swap;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>
    /// /swap: a real prefab (a mod's creature, item or piece, or the game's) takes the look of a freshly built bundle
    /// prefab while the game runs - meshes, textures, clips - on the prefab and every copy of it here, on this machine only.
    /// </summary>
    internal static class SwapRoute
    {
        internal static void Register(Router router) => router.Add("/swap",
            "/swap?prefab=<prefab>&asset=<bundle prefab>&bundle=<name>|load=<file>&materials=textures|replace|keep&clips=1&watch=1\n" +
            "       &map=<bundle renderer>:<live renderer>,...\n" +
            "                       the prefab, its live and stage copies, the build ghost and (an item) the visuals worn or on stands take\n" +
            "                       the bundle prefab's meshes, renderers paired by path, then name, then mesh name, skinned ones rebound\n" +
            "                       by bone names; materials=textures (default) puts the bundle's textures on copies of the live materials;\n" +
            "                       clips=1 overrides the animators' clips of the same names; watch=1 applies it again whenever the bundle\n" +
            "                       file is rebuilt. This machine only; build the preview under its own bundle name (build.ps1 -Bundle\n" +
            "                       <name>_preview)\n" +
            "/swap?prefab=<prefab>&revert=1   the original look back everywhere (prefab=all for every swap)\n" +
            "/swap?list=1           what is swapped (also /swap alone); watch=1|0&bundle=<name> starts or stops watching (all without bundle=)",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (ZNet.instance && ZNet.instance.IsDedicated())
                throw new BridgeException("a swap changes what this machine draws, and a dedicated server draws nothing: swap on a client");
            if (request.Get("revert") == "all") request.Json(Revert("all"));
            else if (request.Flag("revert")) request.Json(Revert(request.Require("prefab")));
            else if (request.Has("prefab")) request.Json(Start(request));
            else if (request.Has("watch")) request.Json(Watch(request));
            else request.Json(List());
        }

        private static Dictionary<string, object> Start(BridgeRequest request)
        {
            GameObject prefab = GamePrefabs.Require(request.Require("prefab"));
            LoadedBundle loaded = Source(request, out string asset);
            var entry = new SwapEntry
            {
                Prefab = prefab, PrefabName = prefab.name, Hash = prefab.name.GetStableHashCode(), Bundle = loaded.Name,
                Asset = asset ?? Default(loaded, prefab.name), Materials = Mode(request.Get("materials")), Clips = request.Flag("clips"),
                Map = request.Get("map"), At = DateTime.Now,
            };
            Swaps.Start(entry, loaded);
            if (request.Flag("watch")) Watches.Start(loaded);
            EventLog.Add("swap", SwapReport.Event(entry, "swapped"));
            return SwapReport.Full(entry);
        }

        /// <summary>The bundle from load= (loaded, or reloaded when the file is newer), bundle=, or whichever loaded bundle has asset=.</summary>
        private static LoadedBundle Source(BridgeRequest request, out string asset)
        {
            asset = request.Get("asset");
            if (request.Has("load")) return Load(request.Require("load"));
            if (request.Has("bundle")) return Named(request.Require("bundle"));
            if (asset == null) throw new BridgeException("give asset=<prefab in the bundle> and bundle=<loaded bundle> or load=<file>");
            Bundles.Asset<GameObject>(asset, null, out LoadedBundle from);
            return from;
        }

        private static LoadedBundle Load(string file)
        {
            LoadedBundle already = Bundles.FromFile(file);
            if (already == null) return Loaded(Bundles.Load(file));
            if (File.Exists(already.Path) && File.GetLastWriteTime(already.Path) != already.FileTime) Reloads.Reload(already);
            return already;
        }

        // A swap left waiting by a reload that failed goes back on when its bundle loads again.
        private static LoadedBundle Loaded(LoadedBundle loaded)
        {
            Swaps.Restore(loaded, new Dictionary<string, object>());
            return loaded;
        }

        private static LoadedBundle Named(string name)
        {
            if (Bundles.All.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase))) return Bundles.Get(name);
            if (AssetBundle.GetAllLoadedAssetBundles().Any(b => string.Equals(b.name, name, StringComparison.OrdinalIgnoreCase)))
                throw new BridgeException($"{name} is loaded by a mod that embeds it, not from a file, so it cannot be swapped from or reloaded. " +
                    $"A bundle name loads only once: build the preview under its own name (build.ps1 -Asset <asset> -Bundle {name}_preview) and give load=<that file>");
            return Bundles.Get(name);
        }

        /// <summary>Without asset=: the bundle prefab named like the live one, if there is one.</summary>
        private static string Default(LoadedBundle loaded, string prefab)
        {
            Swaps.Source(loaded, prefab);
            return prefab;
        }

        private static MaterialMode Mode(string text)
        {
            switch ((text ?? "textures").ToLowerInvariant())
            {
                case "textures": return MaterialMode.Textures;
                case "replace": return MaterialMode.Replace;
                case "keep": return MaterialMode.Keep;
                default: throw new BridgeException($"materials= is textures (the default), replace or keep, not {text}");
            }
        }

        private static object Revert(string prefab)
        {
            List<SwapEntry> doomed = prefab == "all" ? Swaps.All.ToList() : new List<SwapEntry> { Swaps.Get(prefab) };
            foreach (SwapEntry entry in doomed)
            {
                Swaps.Take(entry);
                EventLog.Add("swap", SwapReport.Event(entry, "reverted"));
            }
            return new Dictionary<string, object> { ["reverted"] = doomed.Select(e => e.PrefabName).ToList(), ["still swapped"] = Swaps.Names() };
        }

        private static object Watch(BridgeRequest request)
        {
            string bundle = request.Get("bundle");
            if (!request.Flag("watch")) return new Dictionary<string, object> { ["stopped"] = Watches.Stop(bundle), ["watching"] = Watches.Names };
            IEnumerable<string> names = bundle != null ? new[] { Bundles.Get(bundle).Name } : Swaps.All.Where(e => e.On).Select(e => e.Bundle).Distinct();
            foreach (string name in names.ToList()) Watches.Start(Bundles.Get(name));
            return new Dictionary<string, object> { ["watching"] = Watches.Names };
        }

        private static object List() => new Dictionary<string, object>
        {
            ["swaps"] = Swaps.All.Select(SwapReport.Brief).ToList(),
            ["watching"] = Watches.Names,
        };
    }
}
