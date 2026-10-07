using HarmonyLib;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// How tall a Mímir's Chest is: always two empty rows below its lowest item, at least four rows, at most 32 (256
    /// stacks: the chest's whole inventory travels in one ZDO value, so a ceiling keeps it a size the network carries).
    /// Worked out from the contents after every change of the inventory, on every machine alike: the game loads items
    /// below the grid's height as they were saved, so no machine needs to be told the height. The chest's inventory is
    /// known by its name (<see cref="MimirPrefab.ContainerName"/>); the container panel follows the new height on its own.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "Changed")]
    public static class MimirRows
    {
        public const int MinRows = 4;
        public const int SpareRows = 2;
        public const int MaxRows = 32;

        [HarmonyPostfix]
        private static void Postfix(Inventory __instance)
        {
            if (__instance.m_name == MimirPrefab.ContainerName)
                Fit(__instance);
        }

        public static void Fit(Inventory inventory)
        {
            int lowest = -1;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.m_gridPos.y > lowest)
                    lowest = item.m_gridPos.y;
            }
            int rows = System.Math.Min(System.Math.Max(lowest + 1 + SpareRows, MinRows), MaxRows);
            rows = System.Math.Max(rows, lowest + 1);
            if (inventory.m_height != rows)
                inventory.m_height = rows;
            if (inventory.m_width != MimirPrefab.Width)
                inventory.m_width = MimirPrefab.Width;
        }
    }
}
