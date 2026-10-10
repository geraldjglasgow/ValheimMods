using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using DevBridge.Swap;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The studio's Workshop tab: the workshop's bundles (<see cref="WorkshopFiles"/>), each opened by loading it from its
    /// file as /bundle?load= does (again when the file was rebuilt since) and listed as its models and effects, which the
    /// booth draws like the game's items. A bundle a mod embeds is loaded already under its name and cannot load twice:
    /// its models are then read from the mod's copy, which only a rebuild of the mod changes.
    /// </summary>
    internal static class StudioWorkshop
    {
        private static readonly string[] Order = { "Creatures", "Pieces", "Items", "Rigged models", "Models", "Effects" };
        private static readonly Dictionary<string, string> Categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // of the bundles opened

        internal static Dictionary<string, object> List() => new Dictionary<string, object>
        {
            ["folder"] = WorkshopFiles.Folder(),
            ["bundles"] = WorkshopFiles.All().Select(Line).ToList(),
        };

        /// <summary>Loads the bundle (or reads the mod's copy) and lists its models in groups.</summary>
        internal static Dictionary<string, object> Open(string name, bool reload)
        {
            WorkshopFile file = WorkshopFiles.Find(name);
            Categories[file.Name] = file.Category;
            AssetBundle held = Loaded(file.Name) == null ? Held(file.Name) : null;
            if (held) return Models(file, held, "mod", new Dictionary<string, object>());
            Dictionary<string, object> result = Load(file, reload, out LoadedBundle loaded);
            return Models(file, loaded.Bundle, "workshop", result);
        }

        /// <summary>A model of a bundle the studio opened, from its own load or a mod's copy.</summary>
        internal static GameObject Prefab(string bundle, string prefab)
        {
            if (Loaded(bundle) != null) return Bundles.Asset<GameObject>(prefab, bundle, out _);
            AssetBundle held = Held(bundle) ?? throw new BridgeException($"the bundle {bundle} is not loaded: open it in the studio's Workshop tab");
            GameObject found = held.LoadAsset<GameObject>(prefab);
            return found ? found : throw new BridgeException($"no model {prefab} in {bundle}");
        }

        /// <summary>The look a model of an opened bundle gets unless asked (<see cref="BoothLook.For"/>).</summary>
        internal static string LookFor(string bundle, GameObject prefab) =>
            BoothLook.For(Kind(prefab), Categories.TryGetValue(bundle, out string category) ? category : null);

        private static Dictionary<string, object> Line(WorkshopFile file)
        {
            LoadedBundle loaded = Loaded(file.Name);
            return new Dictionary<string, object>
            {
                ["name"] = file.Name,
                ["file"] = file.Relative,
                ["category"] = file.Category,
                ["built"] = Stamp(file.Built),
                ["copies"] = file.Copies,
                ["loaded"] = loaded != null,
                ["rebuilt"] = loaded != null && Newer(file, loaded),
                ["mod"] = loaded == null && Held(file.Name),
            };
        }

        private static Dictionary<string, object> Load(WorkshopFile file, bool reload, out LoadedBundle loaded)
        {
            loaded = Bundles.FromFile(file.Path);
            if (loaded != null) return reload || Newer(file, loaded) ? Reloads.Reload(loaded) : new Dictionary<string, object>();
            loaded = Bundles.Load(file.Path);
            return Swaps.Restore(loaded, new Dictionary<string, object>()); // swaps left waiting by a reload whose build failed go back on
        }

        private static Dictionary<string, object> Models(WorkshopFile file, AssetBundle bundle, string from, Dictionary<string, object> result)
        {
            List<Dictionary<string, object>> models = bundle.LoadAllAssets<GameObject>().Where(Shown).Select(p => Model(p, file.Category)).ToList();
            result["name"] = file.Name;
            result["file"] = file.Relative;
            result["category"] = file.Category;
            result["built"] = Stamp(file.Built);
            result["from"] = from;
            result["groups"] = models.GroupBy(model => (string)model["kind"]).OrderBy(group => Array.IndexOf(Order, group.Key))
                .Select(group => new Dictionary<string, object> { ["name"] = group.Key, ["items"] = group.OrderBy(m => (string)m["prefab"]).ToList() })
                .ToList();
            return result;
        }

        // Something the booth can draw: a mesh or particles.
        private static bool Shown(GameObject prefab) =>
            prefab.GetComponentsInChildren<Renderer>(true).Any(r => r is MeshRenderer || r is SkinnedMeshRenderer || r is ParticleSystemRenderer);

        private static Dictionary<string, object> Model(GameObject prefab, string category) => new Dictionary<string, object>
        {
            ["prefab"] = prefab.name,
            ["kind"] = Kind(prefab),
            ["look"] = BoothLook.For(Kind(prefab), category),
            ["triangles"] = AssetInfo.Triangles(prefab),
            ["materials"] = AssetInfo.Materials(prefab).Count,
        };

        /// <summary>The group a model lists under: Creatures, Pieces, Items, Rigged models, Models or Effects.</summary>
        internal static string Kind(GameObject prefab)
        {
            string game = GamePrefabs.Kind(prefab);
            if (game == "creature" || game == "piece" || game == "item") return char.ToUpperInvariant(game[0]) + game.Substring(1) + "s";
            if (!prefab.GetComponentsInChildren<Renderer>(true).Any(r => r is MeshRenderer || r is SkinnedMeshRenderer)) return "Effects";
            return prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) ? "Rigged models" : "Models";
        }

        private static LoadedBundle Loaded(string name) =>
            Bundles.All.FirstOrDefault(bundle => string.Equals(bundle.Name, name, StringComparison.OrdinalIgnoreCase));

        // Loaded under this name by something other than DevBridge: a mod that embeds it.
        private static AssetBundle Held(string name) =>
            AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(bundle => bundle && Same(bundle.name, name) && Bundles.All.All(own => own.Bundle != bundle));

        private static bool Same(string bundleName, string name) =>
            string.Equals(System.IO.Path.GetFileNameWithoutExtension(bundleName ?? ""), name, StringComparison.OrdinalIgnoreCase);

        private static bool Newer(WorkshopFile file, LoadedBundle loaded) => (file.Built - loaded.FileTime).TotalSeconds > 1;

        private static string Stamp(DateTime time) => time.ToString("yyyy-MM-dd HH:mm");
    }
}
