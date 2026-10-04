using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Tune
{
    /// <summary>A prefab's component, and the same component on each of its live networked instances.</summary>
    internal static class LiveParts
    {
        /// <summary>The GameObject itself, its own component of the type, or failing that one on a child; null when it has none.</summary>
        internal static object Part(GameObject go, Type type)
        {
            if (type == typeof(GameObject)) return go;
            Component part = go.GetComponent(type);
            if (!part) part = go.GetComponentInChildren(type, true);
            return part ? part : null;
        }

        /// <summary>Every loaded instance whose ZDO was made from the prefab.</summary>
        internal static List<TuneRoot> Instances(GameObject prefab, Type type)
        {
            ZNetScene scene = ZNetScene.instance ? ZNetScene.instance : throw new BridgeException("no world loaded");
            int hash = prefab.name.GetStableHashCode();
            var found = new List<TuneRoot>();
            foreach (ZNetView view in scene.m_instances.Values)
            {
                if (!view || !view.IsValid() || view.GetZDO().GetPrefab() != hash) continue;
                object part = Part(view.gameObject, type);
                if (part != null) found.Add(new TuneRoot(part, "live"));
            }
            return found;
        }

        internal static string Names(GameObject prefab) =>
            string.Join(", ", prefab.GetComponents<Component>().Where(part => part).Select(part => part.GetType().Name).Distinct());
    }
}
