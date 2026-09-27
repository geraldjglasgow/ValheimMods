using System;
using System.Collections.Generic;

namespace GrindstoneSkills
{
    /// <summary>
    /// Composting, on the bin's owner: one unit of the first compostable item in the bin becomes one point of compost,
    /// until the bin is full. Compostable: anything with food value, anything that carries stars (crops, seeds, dishes,
    /// flour), and the items named in Compost Items. Taking the unit changes the container's inventory, which the game
    /// saves to the bin's ZDO on its owner (Container.OnContainerChanged), so everyone sees it go.
    /// </summary>
    public static class CompostDigest
    {
        private static readonly HashSet<string> extra = new HashSet<string>(StringComparer.Ordinal);
        private static string extraText;

        public static void Step(CompostBin bin)
        {
            Inventory inventory = bin.Container != null ? bin.Container.GetInventory() : null;
            if (inventory == null || bin.Points >= CompostBin.Capacity)
                return;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (!Compostable(item))
                    continue;
                inventory.RemoveItem(item, 1);
                bin.AddPoints(1f);
                return;
            }
        }

        public static bool Compostable(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
                return false;
            if (Kitchen.Value(item.m_shared) > 0f || Kitchen.IsKitchenItem(item))
                return true;
            return item.m_dropPrefab != null && Extra().Contains(item.m_dropPrefab.name);
        }

        /// <summary>The Compost Items setting as prefab names, parsed once per change of its text.</summary>
        private static HashSet<string> Extra()
        {
            string text = CompostSettings.ExtraItems.Value ?? "";
            if (text == extraText)
                return extra;
            extraText = text;
            extra.Clear();
            foreach (string part in text.Split(','))
            {
                if (part.Trim().Length > 0)
                    extra.Add(part.Trim());
            }
            return extra;
        }
    }
}
