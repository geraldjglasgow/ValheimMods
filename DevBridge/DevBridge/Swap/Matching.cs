using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>
    /// Pairs each mesh renderer of the live prefab with the bundle prefab's renderer whose look it takes: one named in
    /// map=, else the same path from the root, else a path that ends the other (the model one level deeper or
    /// shallower), else the same renderer name, else the same mesh name (a body a mod put on a game creature's own
    /// renderer keeps the bundle mesh's name). Name and mesh pairs need the name to be unique in the bundle.
    /// </summary>
    internal static class Matching
    {
        internal static bool Swappable(Renderer renderer) =>
            renderer is SkinnedMeshRenderer || (renderer is MeshRenderer && renderer.GetComponent<MeshFilter>());

        /// <summary>A pair for every live renderer; `missing` gets the bundle renderers nothing took.</summary>
        internal static List<Pair> Pairs(GameObject prefab, GameObject source, string map, out List<string> missing)
        {
            Dictionary<Renderer, string> theirs = Renderers(source);
            Dictionary<string, Renderer> chosen = Chosen(map, theirs, source.name);
            List<Pair> pairs = Renderers(prefab).Select(live => Best(live.Key, live.Value, theirs, chosen)).ToList();
            var used = new HashSet<Renderer>(pairs.Where(p => p.Bundle).Select(p => p.Bundle));
            missing = theirs.Where(t => !used.Contains(t.Key))
                .Select(t => $"{Shown(t.Value, source.name)} ({Look.Triangles(t.Key)} triangles): nothing on {prefab.name} pairs with it").ToList();
            return pairs;
        }

        private static Dictionary<Renderer, string> Renderers(GameObject root)
        {
            var found = new Dictionary<Renderer, string>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true).Where(Swappable))
                found[renderer] = Paths.Of(renderer.transform, root.transform);
            return found;
        }

        private static Pair Best(Renderer live, string path, Dictionary<Renderer, string> theirs, Dictionary<string, Renderer> chosen)
        {
            var pair = new Pair { Path = path, Live = live };
            if (chosen.TryGetValue(path, out Renderer picked) || chosen.TryGetValue(live.name, out picked)) return pair.To(picked, "map");
            Renderer found = One(theirs.Where(t => t.Value == path));
            if (found) return pair.To(found, "path");
            found = One(theirs.Where(t => Ends(path, t.Value)));
            if (found) return pair.To(found, "path end");
            found = One(theirs.Where(t => t.Key.name == live.name));
            if (found) return pair.To(found, "name");
            Mesh mesh = Look.MeshOf(live);
            found = mesh ? One(theirs.Where(t => SameMesh(t.Key, mesh))) : null;
            return found ? pair.To(found, "mesh name") : pair;
        }

        private static Renderer One(IEnumerable<KeyValuePair<Renderer, string>> candidates)
        {
            List<KeyValuePair<Renderer, string>> list = candidates.Take(2).ToList();
            return list.Count == 1 ? list[0].Key : null;
        }

        private static bool Ends(string live, string bundle) =>
            live.Length > 0 && bundle.Length > 0 && (live.EndsWith("/" + bundle, StringComparison.Ordinal) || bundle.EndsWith("/" + live, StringComparison.Ordinal));

        private static bool SameMesh(Renderer renderer, Mesh mesh)
        {
            Mesh theirs = Look.MeshOf(renderer);
            return theirs && theirs.name == mesh.name;
        }

        /// <summary>map=bundleRenderer:liveRenderer,... (names or paths), by live path or name.</summary>
        private static Dictionary<string, Renderer> Chosen(string map, Dictionary<Renderer, string> theirs, string source)
        {
            var chosen = new Dictionary<string, Renderer>(StringComparer.Ordinal);
            foreach (string entry in (map ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0))
            {
                string[] parts = entry.Split(':');
                if (parts.Length != 2) throw new BridgeException($"map= takes bundleRenderer:liveRenderer pairs, not {entry}");
                Renderer bundle = One(theirs.Where(t => t.Value == parts[0].Trim())) ?? One(theirs.Where(t => t.Key.name == parts[0].Trim()))
                    ?? throw new BridgeException($"map: {source} has no single mesh renderer {parts[0]} (its renderers: {string.Join(", ", theirs.Values.Select(v => Shown(v, source)))})");
                chosen[parts[1].Trim()] = bundle;
            }
            return chosen;
        }

        private static string Shown(string path, string root) => path.Length == 0 ? root : path;
    }
}
