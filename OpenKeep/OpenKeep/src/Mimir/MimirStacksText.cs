using HarmonyLib;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// A Mímir's Chest slot shows its real count: the game writes "count/normal stack" (50/50) on every stacking item, which
    /// for a big stack would read 5000/50; in a Mímir grid the amount is the count alone (5000), which fits the slot where
    /// 5000/9999 would not. The game rewrites the text every frame, so this postfix does too, from cached number strings.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class MimirStacksText
    {
        private static readonly string[] numbers = new string[MimirStacks.MaxStack + 1];

        [HarmonyPostfix]
        private static void Postfix(InventoryGrid __instance)
        {
            Inventory inventory = __instance.m_inventory;
            if (!MimirStacks.IsMimir(inventory))
                return;
            int width = inventory.GetWidth();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (!MimirStacks.Stacks(item) || item.m_gridPos.x < 0 || item.m_gridPos.y < 0)
                    continue;
                InventoryElement element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, width);
                if (element != null && element.m_amount != null)
                    element.m_amount.text = Number(item.m_stack);
            }
        }

        private static string Number(int count)
        {
            if (count < 0 || count >= numbers.Length)
                return count.ToString();
            return numbers[count] ?? (numbers[count] = count.ToString());
        }
    }
}
