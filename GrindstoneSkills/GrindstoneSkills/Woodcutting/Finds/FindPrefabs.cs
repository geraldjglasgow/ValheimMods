using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The item prefabs GrindstoneSkills.Finds.yml names, looked up when a find drops (the file parses before the item
    /// database exists, and on a dedicated server before any world is loaded). ObjectDB.GetItemPrefab looks a name up
    /// by its stable hash, so the exact spelling; a miss is retried against ObjectDB.m_items ignoring case. A name that
    /// matches no item is logged once (again after the file is reloaded) and skipped, so a typo loses one item, not the
    /// find.
    /// </summary>
    public static class FindPrefabs
    {
        private static readonly HashSet<string> warned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The item's ItemDrop on its prefab; null for an unknown name or before the item database exists.</summary>
        public static ItemDrop Item(string prefabName)
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null || string.IsNullOrEmpty(prefabName))
                return null;
            GameObject prefab = db.GetItemPrefab(prefabName);
            if (prefab == null)
                prefab = IgnoringCase(db, prefabName);
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (item == null && warned.Add(prefabName))
                GrindstoneSkills.Log.LogWarning($"Finds: there is no item named '{prefabName}' (GrindstoneSkills.Finds.yml); it is skipped.");
            return item;
        }

        /// <summary>Lets unknown names be warned about again after the file changed.</summary>
        public static void ResetWarnings() => warned.Clear();

        private static GameObject IgnoringCase(ObjectDB db, string prefabName)
        {
            foreach (GameObject prefab in db.m_items)
            {
                if (prefab != null && string.Equals(prefab.name, prefabName, StringComparison.OrdinalIgnoreCase))
                    return prefab;
            }
            return null;
        }
    }
}
