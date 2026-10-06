using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Batch
{
    /// <summary>
    /// The slow part of <see cref="BatchAmount.CanMake"/> (the game's HaveRequirements, which with Reach counts the
    /// containers, and the room check), remembered per amount of one recipe: the stepper asks for the amount and the
    /// amount + 1 every frame. The answers are dropped when the recipe or the station changes, when any inventory changes
    /// (<see cref="InventoryChanges"/>: a craft, a pickup, a chest's change arriving) and after <see cref="MaxAge"/>
    /// seconds, so a container coming into reach, a station upgraded or a cheat switched on counts within a quarter second.
    /// </summary>
    internal static class BatchCheck
    {
        private const float MaxAge = 0.25f;

        private static readonly Dictionary<int, bool> answers = new Dictionary<int, bool>();
        private static Recipe forRecipe;
        private static CraftingStation forStation;
        private static Player forPlayer;
        private static int forChange = -1;
        private static float since = float.MinValue;

        /// <summary>The materials for that many crafts are there (or crafting is free) and what is made fits the inventory.</summary>
        public static bool HaveAndFit(Player player, Recipe made, int amount)
        {
            Expire(player, made);
            if (answers.TryGetValue(amount, out bool yes))
                return yes;
            yes = Ask(player, made, amount);
            answers[amount] = yes;
            return yes;
        }

        private static bool Ask(Player player, Recipe made, int amount)
        {
            bool free = player.NoCostCheat() || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost));
            if (!free && !player.HaveRequirements(made, false, 1, amount))
                return false;
            return player.GetInventory().CanAddItem(made.m_item.gameObject, made.m_amount * amount);
        }

        private static void Expire(Player player, Recipe made)
        {
            CraftingStation station = player.GetCurrentCraftingStation();
            float now = Time.unscaledTime;
            if (made == forRecipe && station == forStation && player == forPlayer && InventoryChanges.Count == forChange && now - since < MaxAge)
                return;
            answers.Clear();
            forRecipe = made;
            forStation = station;
            forPlayer = player;
            forChange = InventoryChanges.Count;
            since = now;
        }
    }
}
