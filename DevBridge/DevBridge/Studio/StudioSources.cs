using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using DevBridge.Ui;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// Particle effects the studio can attach: those in workshop bundles DevBridge loaded (bundle:ecp_frost_fx/ecp_frost_impact,
    /// opened in the Workshop tab or with /bundle?load=), the game's own (game:vfx_...) and the effect templates mods keep
    /// inside inactive holders in the scene (template:BundlePrefabs_bench/ecf_rune_vortex_1EFF00), which are what a mod's
    /// code instantiates when it plays an effect of its own.
    /// </summary>
    internal static class StudioSources
    {
        private const int Most = 80;
        private const string Game = "game:";
        private const string Template = "template:";
        private const string Bundle = "bundle:";

        internal static List<Dictionary<string, object>> List(string filter)
        {
            IEnumerable<Dictionary<string, object>> workshop = Bundles.All.SelectMany(b => Effects(b).Where(e => Matches(e.name, filter))
                .Select(e => Line(Bundle + b.Name + "/" + e.name, e.name, "workshop effect")));
            IEnumerable<Dictionary<string, object>> templates = Templates().Where(t => Matches(t.name, filter))
                .Select(t => Line(Template + ScenePaths.PathOf(t), t.name, "mod template"));
            IEnumerable<Dictionary<string, object>> game = GamePrefabs.All().Where(p => Matches(p.name, filter) && GamePrefabs.Kind(p) == "vfx")
                .OrderBy(p => p.name).Select(p => Line(Game + p.name, p.name, "game effect"));
            return workshop.Concat(templates).Concat(game).Take(Most).ToList();
        }

        internal static GameObject Resolve(string source)
        {
            if (source.StartsWith(Game)) return GamePrefabs.Require(source.Substring(Game.Length));
            if (source.StartsWith(Template)) return ScenePaths.Require(source.Substring(Template.Length));
            if (BundleOf(source) is string bundle) return Bundles.Asset<GameObject>(source.Substring(Bundle.Length + bundle.Length + 1), bundle, out _);
            throw new BridgeException("source= starts with bundle:<name>/, game: or template: (list them with /studio/sources)");
        }

        /// <summary>The workshop bundle a bundle: source names, or null for any other source.</summary>
        internal static string BundleOf(string source)
        {
            if (!source.StartsWith(Bundle)) return null;
            int slash = source.IndexOf('/', Bundle.Length);
            return slash > Bundle.Length ? source.Substring(Bundle.Length, slash - Bundle.Length) : null;
        }

        // A bundle's prefabs with particles: its effects.
        private static IEnumerable<GameObject> Effects(LoadedBundle bundle) =>
            bundle.Bundle.LoadAllAssets<GameObject>().Where(prefab => prefab.GetComponentInChildren<ParticleSystem>(true)).OrderBy(p => p.name);

        // The children of inactive scene roots that hold particle systems: mods' effect templates, never DevBridge's own.
        private static IEnumerable<Transform> Templates() =>
            ScenePaths.Roots().Where(root => !root.gameObject.activeSelf)
                .SelectMany(ScenePaths.Children)
                .Where(child => child.GetComponentInChildren<ParticleSystem>(true));

        private static bool Matches(string name, string filter) =>
            string.IsNullOrEmpty(filter) || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        private static Dictionary<string, object> Line(string source, string name, string kind) => new Dictionary<string, object>
        {
            ["source"] = source,
            ["name"] = name,
            ["kind"] = kind,
        };
    }
}
