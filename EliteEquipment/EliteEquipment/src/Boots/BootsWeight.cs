using HarmonyLib;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// The weight half of the 80/20 split, taken where the game reads an item's weight rather than written into it: a
    /// split leggings weighs 80% of its own while boots are on (<c>GetWeight</c>, <c>GetNonStackedWeight</c>: inventory
    /// totals and tooltips; <c>GetEquipmentWeight</c>, which reads the field). The leggings' <c>m_weight</c> stays the
    /// game's, or whatever a stack and weight mod (OpenKeep's Stacks) writes, so both apply. The boots were made at 20% of
    /// their leggings' weight (<see cref="BootsItem"/>) and are an ordinary item to such mods.
    /// </summary>
    public static class BootsWeight
    {
        /// <summary>A split leggings while boots are on, by its name token (every live copy carries it).</summary>
        public static bool Splits(ItemDrop.ItemData item) =>
            BootsSettings.On && item != null && item.m_shared != null && BootSets.ByLegsToken(item.m_shared.m_name) != null;

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeight))]
        private static class Stacked
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (Splits(__instance))
                    __result *= BootsStats.LegsShare;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetNonStackedWeight))]
        private static class Single
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (Splits(__instance))
                    __result *= BootsStats.LegsShare;
            }
        }

        /// <summary>The game's equipment sum reads the field: the leggings' fifth comes off it (the pair's own is added in <see cref="BootsStatPatches"/>).</summary>
        public static float LegsShareOff(Humanoid humanoid) =>
            Splits(humanoid.m_legItem) ? humanoid.m_legItem.m_shared.m_weight * BootsStats.BootsShare : 0f;
    }
}
