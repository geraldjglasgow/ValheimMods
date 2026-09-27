using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The pieces Build On Wood names: the setting split at commas and trimmed, each name looked up in the scene's
    /// prefabs (exact first, then ignoring case), keyed by the prefab's own name. Names without a piece prefab are
    /// logged and skipped. The setting is read on every call.
    /// </summary>
    public static class FirePrefabs
    {
        public static Dictionary<string, Piece> Listed()
        {
            Dictionary<string, Piece> pieces = new Dictionary<string, Piece>(StringComparer.Ordinal);
            foreach (string name in Names())
            {
                GameObject prefab = Find(name);
                Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
                if (piece == null)
                    Plugin.Log.LogWarning($"OpenKeep: Build On Wood names '{name}', which is not a piece prefab.");
                else
                    pieces[prefab.name] = piece;
            }
            return pieces;
        }

        /// <summary>The piece component of the prefab with exactly this name, or null.</summary>
        public static Piece Exact(string prefabName)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
            return prefab != null ? prefab.GetComponent<Piece>() : null;
        }

        private static IEnumerable<string> Names()
        {
            string value = FireSettings.BuildOnWood.Value ?? "";
            foreach (string part in value.Split(','))
            {
                string name = part.Trim();
                if (name.Length > 0)
                    yield return name;
            }
        }

        /// <summary>The scene's prefab of this name, exact first, then ignoring case; null when there is none. Needs the scene.</summary>
        public static GameObject Find(string name)
        {
            ZNetScene scene = ZNetScene.instance;
            GameObject prefab = scene.GetPrefab(name);
            if (prefab != null)
                return prefab;
            foreach (GameObject candidate in scene.m_namedPrefabs.Values)
            {
                if (candidate != null && string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
            return null;
        }
    }
}
