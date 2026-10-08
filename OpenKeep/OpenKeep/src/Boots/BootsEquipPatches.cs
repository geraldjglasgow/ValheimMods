using HarmonyLib;

namespace OpenKeep.Boots
{
    /// <summary>
    /// The game's equip questions for boots (<see cref="WornBoots"/>): EquipItem on boots wears them here and skips the
    /// game's method, which would put them into the leggings' field; IsItemEquiped and IsItemTypeEquiped answer yes for
    /// the worn pair, so the game's UnequipItem, toggle, drag and drop paths take them off as any armour; unequip all
    /// (death) takes them off too. PackPanel's Feet slot follows the same answers.
    /// </summary>
    public static class BootsEquipPatches
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        private static class Equip
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, ref bool __result)
            {
                if (!BootSets.Is(item))
                    return true;
                __result = WornBoots.Wear(__instance, item, triggerEquipEffects);
                return false;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
        private static class IsEquipped
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
            {
                if (!__result && item != null && item.m_equipped && BootSets.Is(item))
                    __result = WornBoots.Of(__instance) == item;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemTypeEquiped))]
        private static class IsTypeEquipped
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
            {
                if (__result || !BootSets.Is(item))
                    return;
                ItemDrop.ItemData worn = WornBoots.Of(__instance);
                __result = worn != null && worn.m_shared.m_name == item.m_shared.m_name;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        private static class Unequip
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop.ItemData item)
            {
                if (BootSets.Is(item))
                    WornBoots.Forget();
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
        private static class UnequipAll
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance) => WornBoots.TakeOffAll(__instance);
        }
    }
}
