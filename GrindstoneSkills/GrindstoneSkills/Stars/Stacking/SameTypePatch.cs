using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stacks never mix star counts. ItemData.IsSameType compares name and world level, and quality only for items that
    /// can be upgraded (m_maxQuality above 1); every dish has a maximum of 1, so a 1-star stack dropped onto a 3-star
    /// one would merge into it. IsSameType decides the merge in Inventory.AddItem(item, amount, x, y) (drag and drop,
    /// dropping a split stack, the first pass of MoveAll) and the touch drop highlight in
    /// InventoryGui.CanDropDragOntoItem. For kitchen items a different quality now makes a different type.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsSameType))]
    public static class SameTypePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ItemDrop.ItemData __instance, ItemDrop.ItemData other, ref bool __result)
        {
            // A true result means other is not null and has the same name.
            if (__result && __instance.m_quality != other.m_quality && Kitchen.IsKitchenItem(__instance))
                __result = false;
        }
    }
}
