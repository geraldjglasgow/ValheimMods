using System;
using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Capacity
{
    /// <summary>
    /// Applies the configured sizes to the container prefabs (Container.m_width / m_height, copied into every new
    /// instance) and to every loaded container of those prefabs (the Container fields and its Inventory's
    /// m_width / m_height, which the container grid reads every frame). Prefabs that leave the file or are
    /// switched off go back to vanilla. A container is never shrunk below a row or column that holds an item.
    /// </summary>
    public static class ContainerSizes
    {
        // Prefab name to the size this module currently keeps it at.
        private static readonly Dictionary<string, ContainerSize> managed = new Dictionary<string, ContainerSize>(StringComparer.OrdinalIgnoreCase);

        public static void ApplyAll()
        {
            if (ZNetScene.instance == null)
                return;
            Dictionary<string, ContainerSize> wanted = Wanted();
            Dictionary<string, ContainerSize> restore = new Dictionary<string, ContainerSize>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, Container> prefab in ContainerPrefabs.All())
            {
                ContainerSize vanilla = VanillaSizes.Remember(prefab.Key, prefab.Value);
                if (wanted.TryGetValue(prefab.Key, out ContainerSize size))
                {
                    SetPrefab(prefab.Value, size);
                    managed[prefab.Key] = size;
                }
                else if (managed.Remove(prefab.Key))
                {
                    SetPrefab(prefab.Value, vanilla);
                    restore[prefab.Key] = vanilla;
                }
            }
            foreach (Container container in ContainerScan.All())
                ApplyLoaded(container, restore);
        }

        /// <summary>The sizes the file asks for, none when the module is off.</summary>
        private static Dictionary<string, ContainerSize> Wanted()
        {
            ContainersModel model = CapacityModule.Set != null ? CapacityModule.Set.Current as ContainersModel : null;
            if (!CapacitySettings.Enabled.Value || model == null)
                return new Dictionary<string, ContainerSize>(StringComparer.OrdinalIgnoreCase);
            return model.Sizes;
        }

        private static void SetPrefab(Container prefab, ContainerSize size)
        {
            prefab.m_width = size.Width;
            prefab.m_height = size.Height;
        }

        private static void ApplyLoaded(Container container, Dictionary<string, ContainerSize> restore)
        {
            string name = ContainerScan.PrefabName(container);
            if (managed.TryGetValue(name, out ContainerSize size) || restore.TryGetValue(name, out size))
                Resize(container, name, size);
        }

        /// <summary>Container.Awake: a container instantiated before the first apply gets its configured size.</summary>
        public static void ApplyTo(Container container)
        {
            if (container == null || !CapacitySettings.Enabled.Value)
                return;
            string name = ContainerScan.PrefabName(container);
            if (managed.TryGetValue(name, out ContainerSize size))
                Resize(container, name, size);
        }

        private static void Resize(Container container, string name, ContainerSize size)
        {
            Inventory inventory = container.GetInventory();
            if (inventory == null)
                return;
            bool same = inventory.GetWidth() == size.Width && inventory.GetHeight() == size.Height
                && container.m_width == size.Width && container.m_height == size.Height;
            if (same)
                return;
            if (HoldsItemOutside(inventory, size))
            {
                Plugin.Log.LogWarning($"OpenKeep: {name} keeps its size {inventory.GetWidth()}x{inventory.GetHeight()}: shrinking it to {size} would hide items. Move them first.");
                return;
            }
            container.m_width = size.Width;
            container.m_height = size.Height;
            inventory.m_width = size.Width;
            inventory.m_height = size.Height;
            inventory.Changed();
        }

        private static bool HoldsItemOutside(Inventory inventory, ContainerSize size)
        {
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.m_gridPos.x >= size.Width || item.m_gridPos.y >= size.Height)
                    return true;
            }
            return false;
        }
    }
}
