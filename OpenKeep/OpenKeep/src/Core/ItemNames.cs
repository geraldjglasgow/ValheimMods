using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Core
{
    /// <summary>
    /// Prefab name, display name and stacking identity of an item. A drop prefab's name is read once and kept (reading
    /// a Unity object's name makes a new string every time, and the marks on the grid and the sorting ask per item).
    /// </summary>
    public static class ItemNames
    {
        private static readonly Dictionary<GameObject, string> prefabNames = new Dictionary<GameObject, string>();

        /// <summary>The drop prefab's name; for items without one (never dropped) the shared name token.</summary>
        public static string PrefabName(ItemDrop.ItemData item)
        {
            if (item == null)
                return "";
            if (item.m_dropPrefab != null)
                return PrefabName(item.m_dropPrefab);
            return item.m_shared != null ? item.m_shared.m_name : "";
        }

        private static string PrefabName(GameObject prefab)
        {
            if (prefabNames.TryGetValue(prefab, out string name))
                return name;
            name = Utils.GetPrefabName(prefab);
            if (prefabNames.Count > 8192)
                prefabNames.Clear();
            prefabNames[prefab] = name;
            return name;
        }

        /// <summary>The localized shared name.</summary>
        public static string DisplayName(ItemDrop.ItemData item)
        {
            return item != null && item.m_shared != null ? Language.Localize(item.m_shared.m_name) : "";
        }

        /// <summary>Same shared name: the identity the game uses when stacking.</summary>
        public static bool SameItem(ItemDrop.ItemData a, ItemDrop.ItemData b)
        {
            return a != null && b != null && a.m_shared != null && b.m_shared != null && a.m_shared.m_name == b.m_shared.m_name;
        }
    }
}
