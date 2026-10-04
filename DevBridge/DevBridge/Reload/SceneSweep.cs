using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DevBridge.Reload
{
    /// <summary>
    /// The old copy's MonoBehaviours wherever they are loaded. On live scene objects (any scene, DontDestroyOnLoad and
    /// inactive ones included, world objects too) they are destroyed; a root object that holds nothing else goes whole,
    /// as do the DontDestroyOnLoad roots named after the mod. Kept: those on prefabs and assets (no scene) and under the
    /// inactive holders mods keep their prefab copies in (an inactive root, or in DontDestroyOnLoad any inactive parent
    /// outside a canvas, as Jotunn's _JotunnRoot/Prefabs), which the running world may still spawn from, and subclasses
    /// of the game's own components, since destroying one breaks the object it is part of. Whatever is destroyed is
    /// switched off first, so none of it runs for the rest of the frame. The plugin components are not touched here:
    /// they go a frame later, last, so their OnDestroy runs after everything else's.
    /// </summary>
    internal sealed class SceneSweep
    {
        private enum Kind
        {
            Destroy,
            Prefab,
            GameSubclass,
        }

        private readonly Assembly old;
        private readonly Report report;
        private readonly HashSet<GameObject> destroyed = new HashSet<GameObject>();
        private readonly Dictionary<Kind, List<string>> kept = new Dictionary<Kind, List<string>>();
        private int networked;

        /// <summary>Every prefab object above a kept old component, for counting the ZNetScene and ObjectDB entries it is in.</summary>
        internal readonly HashSet<GameObject> PrefabParts = new HashSet<GameObject>();

        private SceneSweep(Assembly old, Report report)
        {
            this.old = old;
            this.report = report;
        }

        internal static SceneSweep Run(Assembly old, ICollection<BaseUnityPlugin> plugins, IEnumerable<string> names, Report report)
        {
            var sweep = new SceneSweep(old, report);
            List<MonoBehaviour> mine = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .Where(b => b && b.GetType().Assembly == old && !plugins.Contains(b as BaseUnityPlugin)).ToList();
            foreach (MonoBehaviour behaviour in mine.OrderBy(b => Depth(b.transform)))
            {
                Kind kind = sweep.Classify(behaviour);
                if (kind == Kind.Destroy) sweep.Destroy(behaviour);
                else sweep.Keep(kind, behaviour);
            }
            sweep.NamedRoots(names.Where(n => n.Length >= 4).ToList());
            sweep.ReportKept();
            return sweep;
        }

        /// <summary>True when the object or one of its parents is being destroyed by this sweep.</summary>
        internal bool Going(GameObject go)
        {
            for (Transform at = go ? go.transform : null; at; at = at.parent)
                if (destroyed.Contains(at.gameObject)) return true;
            return false;
        }

        internal static void DestroyPlugins(IEnumerable<BaseUnityPlugin> plugins, Report report)
        {
            foreach (BaseUnityPlugin plugin in plugins.Where(p => p))
            {
                Object.Destroy(plugin);
                report.Count("plugin components");
            }
        }

        private Kind Classify(MonoBehaviour behaviour)
        {
            GameObject go = behaviour.gameObject;
            bool live = go.scene.IsValid() || (go.hideFlags & HideFlags.DontSaveInBuild) != 0;
            if (!live || InHolder(go)) return Kind.Prefab;
            return ExtendsGame(behaviour.GetType()) ? Kind.GameSubclass : Kind.Destroy;
        }

        /// <summary>
        /// Under an inactive parent that is not part of a canvas (a closed window is): the root anywhere, any ancestor
        /// in DontDestroyOnLoad, where mods and Jotunn keep their prefab copies; world objects are judged by the root.
        /// </summary>
        private static bool InHolder(GameObject go)
        {
            bool persistent = go.scene == DevBridgePlugin.Instance.gameObject.scene;
            for (Transform at = go.transform; at; at = at.parent)
            {
                if (at.parent && !persistent) continue;
                if (!at.gameObject.activeSelf && !at.GetComponentInParent<Canvas>(true)) return true;
            }
            return false;
        }

        private static bool ExtendsGame(Type type)
        {
            for (Type at = type.BaseType; at != null && at != typeof(MonoBehaviour); at = at.BaseType)
                if (at.Assembly.GetName().Name.StartsWith("assembly_", StringComparison.Ordinal)) return true;
            return false;
        }

        private void Destroy(MonoBehaviour behaviour)
        {
            GameObject go = behaviour.gameObject;
            if (Going(go)) return;
            if (behaviour.GetComponentInParent<ZNetView>(true)) networked++;
            if (!OwnRoot(go))
            {
                behaviour.StopAllCoroutines();
                behaviour.enabled = false;
                Object.Destroy(behaviour);
                report.Count("components");
                return;
            }
            Remove(go);
        }

        private void Remove(GameObject go)
        {
            destroyed.Add(go);
            go.SetActive(false);
            Object.Destroy(go);
            report.Count("objects");
            report.Destroyed.Add(go.name);
        }

        /// <summary>A root object made only of Transform and the old copy's components: the mod's own holder.</summary>
        private bool OwnRoot(GameObject go) =>
            go.transform.parent == null && go != Chainloader.ManagerObject
            && go.GetComponents<Component>().All(c => c is Transform || (c != null && c.GetType().Assembly == old));

        /// <summary>Active (or canvas) DontDestroyOnLoad roots named after the mod: windows and drivers it built.</summary>
        private void NamedRoots(List<string> names)
        {
            Scene persistent = DevBridgePlugin.Instance.gameObject.scene;
            if (!persistent.IsValid()) return;
            foreach (GameObject root in persistent.GetRootGameObjects())
            {
                if (root == Chainloader.ManagerObject || destroyed.Contains(root) || (!root.activeSelf && !root.GetComponent<Canvas>())) continue;
                if (names.Any(n => root.name.StartsWith(n, StringComparison.OrdinalIgnoreCase))) Remove(root);
            }
        }

        private void Keep(Kind kind, MonoBehaviour behaviour)
        {
            if (!kept.TryGetValue(kind, out List<string> types)) kept[kind] = types = new List<string>();
            types.Add(behaviour.GetType().Name);
            if (kind != Kind.Prefab) return;
            for (Transform at = behaviour.transform; at; at = at.parent) PrefabParts.Add(at.gameObject);
        }

        private void ReportKept()
        {
            foreach (KeyValuePair<Kind, List<string>> pair in kept)
                report.Leave($"{pair.Value.Count} components {Where(pair.Key)}: {Report.Tally(pair.Value)}");
            if (networked > 0)
                report.Leave($"{networked} destroyed components were on world objects (creatures, pieces, items): those get components " +
                    "again only as they load again, and from prefabs the old copy built the old ones, until the world reloads");
        }

        private static string Where(Kind kind)
        {
            switch (kind)
            {
                case Kind.Prefab: return "on prefabs, assets and inactive prefab holders (kept: objects spawned from them get them " +
                    "until the world reloads, from the game's own prefabs until a restart)";
                default: return "that extend the game's own components (kept, destroying them would break their objects)";
            }
        }

        private static int Depth(Transform at)
        {
            int depth = 0;
            for (; at.parent; at = at.parent) depth++;
            return depth;
        }
    }
}
