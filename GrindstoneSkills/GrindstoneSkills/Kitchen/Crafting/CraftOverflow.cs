using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Keeps a kitchen craft from losing items when the inventory is nearly full. DoCrafting checks room with
    /// Inventory.CanAddItem, which counts free space in stacks of any quality, so starred stacks of the dish make it
    /// think 0-star units fit when they do not. Inventory.AddItem(name, ...) then falls back to spawning one plain copy
    /// of the prefab at the player and the rest of that stack is lost. While the craft records, the prefix trims the
    /// game's add of the dish to what fits at 0 stars and hands the rest to <see cref="CraftSplit"/>, which rolls it
    /// with the others and drops what does not fit. When nothing fits it skips the add and returns a copy of the
    /// dish, so the game still counts the craft as made and pays for it.
    /// </summary>
    public static class CraftOverflow
    {
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem),
            new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool) })]
        private static class CraftedAdd
        {
            [HarmonyPrefix]
            private static bool Prefix(Inventory __instance, string name, ref int stack, int quality, ref ItemDrop.ItemData __result, bool __runOriginal)
            {
                KitchenCraftContext craft = KitchenCraft.Current;
                if (!__runOriginal)
                    return false;
                if (!CraftRecord.Recording || craft == null || !craft.Grades || name != craft.Prefab || quality != Stars.ToQuality(0)
                    || __instance != craft.Player.GetInventory())
                    return true;
                int room = CraftSplit.Room(__instance, craft.Shared, quality);
                if (stack <= room)
                    return true;
                craft.Overflow += stack - room;
                stack = room;
                if (room > 0)
                    return true;
                __result = craft.Recipe.m_item.m_itemData.Clone();
                return false;
            }
        }
    }
}
