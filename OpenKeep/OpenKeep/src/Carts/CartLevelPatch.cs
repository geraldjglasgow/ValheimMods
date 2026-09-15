using System;
using HarmonyLib;

namespace OpenKeep.Carts
{
    /// <summary>A cart's station reports Cart Station Level plus its extensions (<c>CraftingStation.GetLevel</c> is 1 plus extensions).</summary>
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetLevel))]
    public static class CartLevelPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CraftingStation __instance, ref int __result)
        {
            if (!CartsSettings.CartWorkbench.Value || !CartStation.IsCart(__instance))
                return;
            __result += Math.Max(1, CartsSettings.CartStationLevel.Value) - 1;
        }
    }
}
