using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Backpacks;
using PackPanel.Core;

namespace PackPanel.Worn
{
    /// <summary>
    /// Every game rule that reads the one utility field also counts the extra utilities (<see cref="ExtraUtilities"/>):
    /// the equipment status effects, set counts, eitr regen, the equipment modifiers (movement, heat resistance, jump,
    /// attack stamina and the rest of the game's list), equipment weight and durability drain. Local player only; the
    /// lists are empty for everyone else. The worn backpack's level speed joins the movement modifier here too, one
    /// postfix on the hot method.
    /// </summary>
    public static class UtilityEffectPatches
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
        public static class StatusEffects
        {
            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance) => ExtraEffects.Sync(__instance);
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetSetCount))]
        public static class SetCount
        {
            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance, string setName, ref int __result)
            {
                IReadOnlyList<ItemDrop.ItemData> worn = ExtraUtilities.Worn;
                if (worn.Count == 0 || !InventoryState.IsLocal(__instance))
                    return;
                for (int i = 0; i < worn.Count; i++)
                {
                    if (worn[i].m_shared.m_setName == setName)
                        __result++;
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
        public static class EitrRegen
        {
            [HarmonyPostfix]
            public static void Postfix(Player __instance, ref float __result)
            {
                IReadOnlyList<ItemDrop.ItemData> worn = ExtraUtilities.Worn;
                if (worn.Count == 0 || !InventoryState.IsLocal(__instance))
                    return;
                for (int i = 0; i < worn.Count; i++)
                    __result += worn[i].m_shared.m_eitrRegenModifier;
            }
        }

        /// <summary>
        /// The extra utilities' modifiers, and the worn backpack's level speed (<see cref="BackpackLevels"/>) into the
        /// movement modifier, the game's first value (<c>GetEquipmentMovementModifier</c> reads index 0).
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers))]
        public static class Modifiers
        {
            [HarmonyPostfix]
            public static void Postfix(Player __instance)
            {
                if (Player.s_equipmentModifierSourceFields == null || !InventoryState.IsLocal(__instance))
                    return;
                float[] values = __instance.m_equipmentModifierValues;
                AddUtilities(values);
                if (values.Length > 0)
                    values[0] += BackpackLevels.WornSpeed(__instance);
            }

            private static void AddUtilities(float[] values)
            {
                IReadOnlyList<ItemDrop.ItemData> worn = ExtraUtilities.Worn;
                if (worn.Count == 0)
                    return;
                AccessTools.FieldRef<ItemDrop.ItemData.SharedData, float>[] fields = ModifierFields.Get();
                int count = System.Math.Min(values.Length, fields.Length);
                // Item by item, each value in the game's order: every value gets the same sum, in the same order, as before.
                for (int w = 0; w < worn.Count; w++)
                {
                    ItemDrop.ItemData.SharedData shared = worn[w].m_shared;
                    for (int i = 0; i < count; i++)
                        values[i] += fields[i](shared);
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetEquipmentWeight))]
        public static class Weight
        {
            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance, ref float __result)
            {
                IReadOnlyList<ItemDrop.ItemData> worn = ExtraUtilities.Worn;
                if (worn.Count == 0 || !InventoryState.IsLocal(__instance))
                    return;
                for (int i = 0; i < worn.Count; i++)
                    __result += worn[i].m_shared.m_weight;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
        public static class Durability
        {
            [HarmonyPostfix]
            public static void Postfix(Humanoid __instance, float dt)
            {
                if (ExtraUtilities.Worn.Count == 0 || !InventoryState.IsLocal(__instance))
                    return;
                // Backwards: a utility that breaks is taken off, which removes it from the list.
                for (int i = ExtraUtilities.Worn.Count - 1; i >= 0; i--)
                {
                    ItemDrop.ItemData item = ExtraUtilities.Worn[i];
                    if (item.m_shared.m_useDurability)
                        __instance.DrainEquipedItemDurability(item, dt);
                }
            }
        }
    }
}
