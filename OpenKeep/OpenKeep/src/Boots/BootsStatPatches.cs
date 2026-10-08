using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace OpenKeep.Boots
{
    /// <summary>
    /// Every game sum that reads only the equipment fields also counts the worn pair (<see cref="WornBoots"/>): body
    /// armour (status effects' armour bonuses apply to the boots' part as to the rest), resistances, the armour piece a hit
    /// wears down, set counts, equipment weight, the equipment modifiers, eitr regen, a broken pair falling off, and the
    /// armour difference a tooltip shows. Damage and these sums run on the player's own client, so nothing is sent.
    /// </summary>
    public static class BootsStatPatches
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
        private static class Armor
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, ref float __result)
            {
                ItemDrop.ItemData pair = WornBoots.Of(__instance);
                if (pair == null)
                    return;
                // The game adds every piece, then lets each status effect add and multiply: the same affine bonus on the boots' part.
                float none = 0f, one = 1f;
                __instance.m_seman.ApplyArmorMods(ref none);
                __instance.m_seman.ApplyArmorMods(ref one);
                __result += pair.GetArmor() * (one - none);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ApplyArmorDamageMods))]
        private static class Resistances
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, ref HitData.DamageModifiers mods)
            {
                ItemDrop.ItemData pair = WornBoots.Of(__instance);
                if (pair != null && pair.m_shared.m_damageModifiers != null && pair.m_shared.m_damageModifiers.Count > 0)
                    mods.Apply(pair.m_shared.m_damageModifiers);
            }
        }

        /// <summary>The game wears down one armour piece a hit, picked at random: the pair is one of the pieces.</summary>
        [HarmonyPatch(typeof(Player), nameof(Player.DamageArmorDurability))]
        private static class Durability
        {
            [HarmonyPrefix]
            private static bool Prefix(Player __instance, HitData hit)
            {
                ItemDrop.ItemData pair = WornBoots.Of(__instance);
                if (pair == null)
                    return true;
                var pieces = new List<ItemDrop.ItemData> { pair };
                foreach (ItemDrop.ItemData piece in new[] { __instance.m_chestItem, __instance.m_legItem, __instance.m_helmetItem, __instance.m_shoulderItem })
                {
                    if (piece != null)
                        pieces.Add(piece);
                }
                float damage = hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage();
                if (damage <= 0f)
                    return false;
                ItemDrop.ItemData worn = pieces[Random.Range(0, pieces.Count)];
                worn.m_durability = Mathf.Max(0f, worn.m_durability - damage);
                return false;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetSetCount))]
        private static class SetCount
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, string setName, ref int __result)
            {
                ItemDrop.ItemData pair = WornBoots.Of(__instance);
                if (pair != null && !string.IsNullOrEmpty(setName) && pair.m_shared.m_setName == setName)
                    __result++;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetEquipmentWeight))]
        private static class Weight
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, ref float __result)
            {
                ItemDrop.ItemData pair = WornBoots.Of(__instance);
                if (pair != null)
                    __result += pair.m_shared.m_weight;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers))]
        private static class Modifiers
        {
            private static FieldInfo[] madeFrom;
            private static AccessTools.FieldRef<SharedData, float>[] fields = new AccessTools.FieldRef<SharedData, float>[0];

            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                ItemDrop.ItemData pair = WornBoots.Of(__instance);
                if (pair == null || Player.s_equipmentModifierSourceFields == null)
                    return;
                AccessTools.FieldRef<SharedData, float>[] getters = Getters();
                float[] values = __instance.m_equipmentModifierValues;
                for (int i = 0; i < values.Length && i < getters.Length; i++)
                    values[i] += getters[i](pair.m_shared);
            }

            /// <summary>Typed getters in the game's order (it reads them boxed with GetValue, which makes garbage every step).</summary>
            private static AccessTools.FieldRef<SharedData, float>[] Getters()
            {
                FieldInfo[] source = Player.s_equipmentModifierSourceFields;
                if (ReferenceEquals(source, madeFrom))
                    return fields;
                var made = new AccessTools.FieldRef<SharedData, float>[source.Length];
                for (int i = 0; i < source.Length; i++)
                    made[i] = AccessTools.FieldRefAccess<SharedData, float>(source[i]);
                madeFrom = source;
                return fields = made;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
        private static class EitrRegen
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, ref float __result)
            {
                ItemDrop.ItemData pair = WornBoots.Of(__instance);
                if (pair != null)
                    __result += pair.m_shared.m_eitrRegenModifier;
            }
        }

        /// <summary>As the game does for every armour piece each frame: a pair worn down to nothing breaks and comes off.</summary>
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
        private static class Breaking
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, float dt)
            {
                ItemDrop.ItemData pair = __instance.IsPlayer() ? WornBoots.Of(__instance) : null;
                if (pair != null && pair.m_shared.m_useDurability)
                    __instance.DrainEquipedItemDurability(pair, dt);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TryGetArmorDifference))]
        private static class ArmorDifference
        {
            [HarmonyPrefix]
            private static bool Prefix(Player __instance, ItemDrop.ItemData item, ref float difference, ref bool __result)
            {
                if (!BootSets.Is(item))
                    return true;
                ItemDrop.ItemData worn = WornBoots.Of(__instance);
                float armor = item.m_shared.m_armor;
                difference = worn == null ? armor : worn == item ? -armor : armor - worn.m_shared.m_armor;
                __result = true;
                return false;
            }
        }
    }
}
