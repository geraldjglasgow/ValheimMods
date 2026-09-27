using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// What a craft at a kitchen crafting station used up. InventoryGui.DoCrafting pays after the crafted item is
    /// added: Player.ConsumeResources calls Inventory.RemoveItem(name, amount, -1) per requirement, and a
    /// single-ingredient recipe calls it directly with its stack's quality. While <see cref="Recording"/> is on,
    /// <see cref="IngredientTakeOrder"/> performs those removals and notes every lot it takes. Stacks taken by reference
    /// with Inventory.RemoveItem(item, amount), which is how OpenKeep pays a shortfall from nearby chests, are noted by
    /// the patch below. The lots give the ingredients' average stars for the roll and the pool the ingredient-save perk
    /// draws from.
    /// </summary>
    public static class CraftRecord
    {
        /// <summary>Units taken from one stack: a copy of the stack (quality, world level, crafter) and how many.</summary>
        public sealed class Lot
        {
            public ItemDrop.ItemData Item;
            public int Count;
        }

        private static readonly List<Lot> lots = new List<Lot>();

        public static bool Recording { get; private set; }

        public static List<Lot> Lots => lots;

        public static void Start()
        {
            lots.Clear();
            Recording = true;
        }

        public static void Stop() => Recording = false;

        public static void Clear()
        {
            Recording = false;
            lots.Clear();
        }

        /// <summary>Notes that <paramref name="count"/> units of this stack were used up, while recording.</summary>
        public static void Note(ItemDrop.ItemData stack, int count)
        {
            if (!Recording || stack == null || count <= 0)
                return;
            ItemDrop.ItemData copy = stack.Clone();
            copy.m_stack = count;
            copy.m_equipped = false;
            lots.Add(new Lot { Item = copy, Count = count });
        }

        /// <summary>The mean stars of the used-up units that can carry stars; 0 when there were none.</summary>
        public static float AverageStars()
        {
            int units = 0;
            int stars = 0;
            foreach (Lot lot in lots)
            {
                if (!Kitchen.IsKitchenItem(lot.Item))
                    continue;
                units += lot.Count;
                stars += Stars.Get(lot.Item) * lot.Count;
            }
            return units == 0 ? 0f : (float)stars / units;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(ItemDrop.ItemData), typeof(int) })]
        private static class StackTaken
        {
            [HarmonyPrefix]
            private static void Prefix(Inventory __instance, ItemDrop.ItemData item, int amount)
            {
                if (Recording && item != null && __instance.ContainsItem(item))
                    Note(item, Mathf.Min(item.m_stack, amount));
            }
        }
    }
}
