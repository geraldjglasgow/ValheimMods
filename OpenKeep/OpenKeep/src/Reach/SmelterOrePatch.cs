using System;
using HarmonyLib;

namespace OpenKeep.Reach
{
    /// <summary>
    /// The smelter's add ore switch (<c>Smelter.OnAddOre</c>, also kilns, blast furnaces, spinning wheels,
    /// windmills and sap extractors). With an empty hand and no ore in the inventory one unit is borrowed from
    /// storage before the game looks; the game's own checks (allowed item, queue cap) and RPC do the rest.
    /// </summary>
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
    public static class SmelterOrePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Smelter __instance, Humanoid user, ItemDrop.ItemData item, ref StationFeed.Loan __state, ref bool __result)
        {
            __state = null;
            if (item != null || !StationFeed.Wanted(user))
                return true;
            Func<ItemDrop.ItemData, bool> accepts = StationAccepts.SmelterOre(__instance);
            if (StationFeed.PullHeld)
            {
                __result = StationFeed.Pull(user, accepts);
                return false;
            }
            if (__instance.GetQueueSize() >= __instance.m_maxOre || __instance.FindCookableItem(user.GetInventory()) != null)
                return true;
            __state = StationFeed.Borrow(user, accepts);
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(Smelter __instance, Switch sw, Humanoid user, ItemDrop.ItemData item, StationFeed.Loan __state, bool __result)
        {
            StationFeed.Settle(user, __state);
            if (item == null && __result && StationFeed.Wanted(user))
                StationFeed.Fill(__instance.m_nview, __instance.m_maxOre, () => __instance.OnAddOre(sw, user, null));
        }
    }
}
