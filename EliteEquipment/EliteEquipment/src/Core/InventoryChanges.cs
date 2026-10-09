using HarmonyLib;

namespace EliteEquipment.Core
{
    /// <summary>
    /// A number that grows whenever any inventory changes: the game's <c>Inventory.Changed</c>, which every add, remove,
    /// move, sort and load from the ZDO ends in (the player's own, every container's, another player's change arriving
    /// by ZDO). Whatever is worked out from inventories and asked for every frame (the worn boots) keeps its answer while the
    /// number stays the same.
    /// </summary>
    public static class InventoryChanges
    {
        public static int Count { get; private set; }

        internal static void Mark() => Count++;
    }

    /// <summary>Every inventory change counts; a prefix, so it counts even when a change callback throws.</summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
    public static class InventoryChangedPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => InventoryChanges.Mark();
    }
}
