using EliteCreaturesReborn.Display;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Draws the world tier under the weight on the inventory screen (<see cref="TierPlate"/>). The game writes the weight
    /// every frame the inventory is open and only then, so the postfix runs exactly while the plate can be seen. It never
    /// throws, so it can never stop the inventory updating.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "UpdateInventoryWeight")]
    public static class InventoryTierPatch
    {
        private static void Postfix(InventoryGui __instance) =>
            Guard.Run("InventoryGui world tier", static gui => TierPlate.Refresh(gui), __instance);
    }
}
