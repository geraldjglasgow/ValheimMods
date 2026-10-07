using HarmonyLib;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// What a craft at a kitchen crafting station used up, as the roll needs it: the units of star items and their stars
    /// (for <see cref="StarOdds.IngredientBonus"/>) and the highest fish level (for <see cref="StarOdds.FishBonus"/>; a
    /// fish's level is its quality, 1 to 5). InventoryGui.DoCrafting pays after adding the crafted item:
    /// Player.ConsumeResources calls Inventory.RemoveItem(name, amount, -1) per requirement, and a single-ingredient
    /// recipe (cleaning a fish) calls it with its stack's quality. While <see cref="Recording"/> is on, the by-name
    /// prefix walks the stacks the game is about to take from, in its own list order (<see cref="KitchenIngredientOrder"/>
    /// put the best first); it runs late, so a mod that pays part of a requirement from chests first (OpenKeep) has
    /// already lowered the amount. The by-reference prefix (Inventory.RemoveItem(item, amount), how OpenKeep takes from a
    /// chest) notes the stack itself. Any inventory counts. GrindstoneSkills records the same removals for its own
    /// ingredient save with patches of its own; both only read.
    /// </summary>
    public static class KitchenIngredientRecord
    {
        private static int starUnits;
        private static int starSum;

        public static bool Recording { get; private set; }

        /// <summary>The highest fish level used up, 0 when no fish was.</summary>
        public static int FishLevel { get; private set; }

        /// <summary>The mean stars of the used-up star items, 0 when there were none.</summary>
        public static float AverageStars => starUnits == 0 ? 0f : (float)starSum / starUnits;

        public static void Start()
        {
            Clear();
            Recording = true;
        }

        public static void Stop() => Recording = false;

        public static void Clear()
        {
            Recording = false;
            starUnits = 0;
            starSum = 0;
            FishLevel = 0;
        }

        private static void Note(ItemDrop.ItemData stack, int count)
        {
            if (stack?.m_shared == null || count <= 0)
                return;
            if (stack.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Fish)
                FishLevel = Mathf.Max(FishLevel, stack.m_quality);
            if (!Stars.IsStarItem(stack))
                return;
            starUnits += count;
            starSum += Stars.Get(stack) * count;
        }

        /// <summary>Notes what the game's RemoveItem by name will take, walking the stacks as it does.</summary>
        private static void NoteByName(Inventory inventory, string name, int amount, int quality, bool worldLevelBased)
        {
            foreach (ItemDrop.ItemData item in inventory.m_inventory)
            {
                if (amount <= 0)
                    break;
                if (item.m_shared.m_name != name || (quality >= 0 && item.m_quality != quality)
                    || (worldLevelBased && item.m_worldLevel < Game.m_worldLevel))
                    continue;
                int take = Mathf.Min(item.m_stack, amount);
                Note(item, take);
                amount -= take;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
        private static class NameTaken
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.Low)]
            private static void Prefix(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased, bool __runOriginal)
            {
                if (Recording && __runOriginal)
                    HookGuard.Run("kitchen ingredients", static args => NoteByName(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5),
                        (__instance, name, amount, itemQuality, worldLevelBased));
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(ItemDrop.ItemData), typeof(int) })]
        private static class StackTaken
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.Low)]
            private static void Prefix(Inventory __instance, ItemDrop.ItemData item, int amount, bool __runOriginal)
            {
                if (Recording && __runOriginal && item != null && __instance.ContainsItem(item))
                    HookGuard.Run("kitchen ingredients", static args => Note(args.Item1, Mathf.Min(args.Item1.m_stack, args.Item2)), (item, amount));
            }
        }
    }
}
