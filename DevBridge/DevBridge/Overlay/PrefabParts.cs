using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// Components a networked object may carry on a child (a SpawnArea finds its network view in a parent), found without
    /// searching every object's children each redraw: whether its prefab has one is looked up once per prefab.
    /// </summary>
    internal static class PrefabParts
    {
        private static readonly Dictionary<(int, Type), bool> Known = new Dictionary<(int, Type), bool>();

        internal static T[] Of<T>(ZNetView view) where T : Component
        {
            var key = (view.GetZDO().GetPrefab(), typeof(T));
            if (!Known.TryGetValue(key, out bool has))
            {
                GameObject prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(key.Item1) : null;
                Known[key] = has = !prefab || prefab.GetComponentInChildren<T>(true);
            }
            return has ? view.GetComponentsInChildren<T>() : Array.Empty<T>();
        }
    }
}
