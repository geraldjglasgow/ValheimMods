using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Core
{
    /// <summary>
    /// Prefab names without a new string per call: Unity's <c>name</c> (and the game's <c>Utils.GetPrefabName</c> on top
    /// of it) copies the name out of the engine every time, and the held tool and the selected piece are asked for theirs
    /// many times a frame. Cached per object by instance id; prefabs keep their names, and the cache is emptied when it
    /// grows past a limit, so objects that come and go cannot make it grow without end.
    /// </summary>
    public static class PrefabNames
    {
        private const int Limit = 512;

        private static readonly Dictionary<int, string> names = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> prefabNames = new Dictionary<int, string>();

        /// <summary>The object's own name (<c>name</c>), or null.</summary>
        public static string Raw(GameObject go) => Cached(go, names, false);

        /// <summary>The object's prefab name (<c>Utils.GetPrefabName</c>: without "(Clone)"), or null.</summary>
        public static string Of(GameObject go) => Cached(go, prefabNames, true);

        /// <summary>The prefab name of an item's drop prefab (as <c>m_dropPrefab.name</c>), or null.</summary>
        public static string OfItem(ItemDrop.ItemData item) => item != null && item.m_dropPrefab != null ? Raw(item.m_dropPrefab) : null;

        private static string Cached(GameObject go, Dictionary<int, string> cache, bool prefab)
        {
            if (go == null)
                return null;
            int id = go.GetInstanceID();
            if (cache.TryGetValue(id, out string name))
                return name;
            if (cache.Count >= Limit)
                cache.Clear();
            name = prefab ? Utils.GetPrefabName(go) : go.name;
            cache[id] = name;
            return name;
        }
    }
}
