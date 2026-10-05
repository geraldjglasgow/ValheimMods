using HarmonyLib;
using PackPanel.Backpacks;
using PackPanel.Core;

namespace PackPanel.Worn
{
    /// <summary>
    /// Equipping and unequipping on the local player. EquipItem wears a second to fifth utility beside the game's one
    /// (<see cref="ExtraUtilities"/>) and moves what was put on into its worn slot; UnequipItem forgets an extra utility
    /// and moves what came off out of its slot. The game's "is it equipped" questions answer yes for the extras, so its
    /// own UnequipItem, drag and use paths treat them like the game's utility. A backpack is worn by PackPanel too
    /// (<see cref="BackpackEquip"/>), never as the game's utility. Unequip all and the death drop take them off too; a
    /// piece the game takes off because it broke stays in its slot.
    /// </summary>
    public static class EquipPatches
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public static class Equip
        {
            [HarmonyPrefix]
            public static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, ref bool __result)
            {
                WornPlacement.BeginEquip();
                if (BackpackEquip.Takes(item))
                    __result = BackpackEquip.Wear(__instance, item, triggerEquipEffects);
                else if (ExtraUtilities.Takes(__instance, item))
                    __result = ExtraUtilities.Wear(__instance, item, triggerEquipEffects);
                else
                    return true;
                return false;
            }

            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result)
            {
                if (__result)
                    WornPlacement.OnWorn(__instance, item);
            }

            [HarmonyFinalizer]
            public static void Finalizer() => WornPlacement.EndEquip();
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        public static class Unequip
        {
            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
            {
                if (item == null || item.m_equipped)
                    return;
                ExtraUtilities.Forget(item);
                WornPlacement.OnTakenOff(__instance, item);
            }
        }

        /// <summary>A piece that breaks while worn stays in its slot (<see cref="WornPlacement.BeginBreak"/>).</summary>
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DrainEquipedItemDurability))]
        public static class Break
        {
            [HarmonyPrefix]
            public static void Prefix() => WornPlacement.BeginBreak();

            [HarmonyFinalizer]
            public static void Finalizer() => WornPlacement.EndBreak();
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
        public static class IsEquipped
        {
            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
            {
                if (!__result && InventoryState.IsLocal(__instance))
                    __result = ExtraUtilities.IsWorn(item) || BackpackEquip.IsWorn(__instance, item);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemTypeEquiped))]
        public static class IsTypeEquipped
        {
            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
            {
                if (__result || item == null || !InventoryState.IsLocal(__instance))
                    return;
                foreach (ItemDrop.ItemData worn in ExtraUtilities.Worn)
                {
                    if (worn.m_shared.m_name == item.m_shared.m_name)
                        __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
        public static class UnequipAll
        {
            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance)
            {
                if (!InventoryState.IsLocal(__instance))
                    return;
                ExtraUtilities.TakeOffAll(__instance);
                BackpackEquip.TakeOffAll(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UnequipDeathDropItems))]
        public static class DeathDrop
        {
            [HarmonyPostfix]
            public static void Postfix(Player __instance)
            {
                if (!InventoryState.IsLocal(__instance))
                    return;
                ExtraUtilities.TakeOffAll(__instance);
                BackpackEquip.TakeOffAll(__instance);
            }
        }
    }
}
