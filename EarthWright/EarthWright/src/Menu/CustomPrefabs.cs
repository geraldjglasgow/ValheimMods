using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// The prefabs of the custom entries, made by <see cref="EntryFactory"/> from a paint-only piece of the entry's tool.
    /// They are remade whenever the entry list changes or the object database is rebuilt, so every machine holds the
    /// entries of the current (server's) file; a player who had a custom entry selected keeps it by name.
    /// </summary>
    public static class CustomPrefabs
    {
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        /// <summary>A custom entry's prefab by its prefab name, or null.</summary>
        public static GameObject Get(string prefabName)
        {
            return prefabName != null && prefabs.TryGetValue(prefabName, out GameObject prefab) && prefab != null ? prefab : null;
        }

        /// <summary>Any custom entry's prefab name, current or removed, so tables drop the pieces of removed entries too.</summary>
        public static bool IsCustomName(string name) => name != null && name.StartsWith("ew_custom_");

        /// <summary>The custom entries of one tool with their prefabs, in file order.</summary>
        public static List<(GameObject Prefab, int? Position)> For(ToolFamily tool)
        {
            return CustomEntries.All.Where(e => e.Family == tool && Get(e.PrefabName) != null)
                .Select(e => (Get(e.PrefabName), e.Position)).ToList();
        }

        /// <summary>
        /// Destroys the old prefabs and makes one per current entry. Also run at every object database, so an icon
        /// borrowed from a piece that only exists in the world (not at the main menu) is found there.
        /// </summary>
        public static void Rebuild()
        {
            foreach (GameObject old in prefabs.Values)
            {
                if (old != null)
                    Object.Destroy(old);
            }
            prefabs.Clear();
            foreach (CustomEntry entry in CustomEntries.All)
                Build(entry);
        }

        private static void Build(CustomEntry entry)
        {
            GameObject prefab = EntryFactory.Clone(entry.BasePrefab, entry.PrefabName);
            if (prefab == null)
                return;
            Sprite icon = EntryIcons.Resolve(entry.Icon) ?? EntryIcons.Own("custom");
            EntryFactory.Setup(prefab.GetComponent<Piece>(), entry.Name, entry.Description ?? "", icon, special: true);
            prefabs[entry.PrefabName] = prefab;
        }
    }
}
