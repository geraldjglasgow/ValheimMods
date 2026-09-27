using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// Finds the game's items and crafting stations by the prefab names players write in the settings and in
    /// EarthWright.Costs.yml (Stone, piece_workbench). A name that matches nothing is logged once and then ignored,
    /// so a typo makes a cost disappear rather than block the player.
    /// </summary>
    internal static class ItemLookup
    {
        private static readonly HashSet<string> warned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The item prefab's ItemDrop, or null for an empty or unknown name (or before the item database exists).</summary>
        public static ItemDrop Item(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName) || ObjectDB.instance == null)
                return null;
            GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName.Trim());
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (item == null)
                WarnOnce("item", prefabName);
            return item;
        }

        /// <summary>
        /// The station's name as the game compares stations ("$piece_workbench"). A name starting with $ is taken as
        /// it is; otherwise the prefab is looked up. Null when unknown.
        /// </summary>
        public static string StationName(string prefabName, bool warn = true)
        {
            if (string.IsNullOrWhiteSpace(prefabName))
                return null;
            string name = prefabName.Trim();
            if (name.StartsWith("$"))
                return name;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            CraftingStation station = prefab != null ? prefab.GetComponentInChildren<CraftingStation>(true) : null;
            if (station != null)
                return station.m_name;
            if (warn)
                WarnOnce("crafting station", name);
            return null;
        }

        /// <summary>Lets names be warned about again after the YAML file or a setting changed.</summary>
        public static void ResetWarnings() => warned.Clear();

        private static void WarnOnce(string kind, string name)
        {
            if (warned.Add(kind + ":" + name))
                Plugin.Log.LogWarning($"Costs: there is no {kind} named '{name}'; that cost is ignored.");
        }
    }
}
