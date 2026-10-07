using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// The stars of each active food, kept in the player's own custom data (Player.m_customData, saved with the
    /// character) under <see cref="EatingKeys.ActiveFood"/> plus the food's prefab name (Player.Food.m_name). The game
    /// saves only each active food's name and time left, and Player.Load rebuilds the food from the item prefab, so the
    /// eaten dish's quality, where its stars live, is gone after a relog. A food without a key has 0 stars. The game
    /// keeps at most one food per shared name, so one key per prefab name is enough.
    /// </summary>
    public static class FoodStars
    {
        private static readonly Dictionary<string, string> keys = new Dictionary<string, string>();
        private static readonly List<string> stale = new List<string>();

        /// <summary>The stars of one of the player's active foods, 0 when it has none.</summary>
        public static int Get(Player player, Player.Food food)
        {
            if (player.m_customData.Count == 0 || string.IsNullOrEmpty(food?.m_name))
                return 0;
            if (!player.m_customData.TryGetValue(Key(food.m_name), out string text))
                return 0;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stars) ? Mathf.Clamp(stars, 0, Stars.Max) : 0;
        }

        /// <summary>Records the stars of the active food with this prefab name; 0 stars removes the key.</summary>
        public static void Set(Player player, string foodName, int stars)
        {
            if (string.IsNullOrEmpty(foodName))
                return;
            stars = Mathf.Clamp(stars, 0, Stars.Max);
            if (stars > 0)
                player.m_customData[Key(foodName)] = stars.ToString(CultureInfo.InvariantCulture);
            else
                player.m_customData.Remove(Key(foodName));
        }

        /// <summary>
        /// Removes the key of every food the player no longer has. Only call it once the player's foods are loaded:
        /// on a player whose foods are not loaded yet it would drop the stars of every food.
        /// </summary>
        public static void Prune(Player player)
        {
            if (player == null || player.m_customData.Count == 0)
                return;
            foreach (string key in player.m_customData.Keys)
            {
                if (key.StartsWith(EatingKeys.ActiveFood, StringComparison.Ordinal) && !IsActive(player, key))
                    stale.Add(key);
            }
            foreach (string key in stale)
                player.m_customData.Remove(key);
            stale.Clear();
        }

        private static bool IsActive(Player player, string key)
        {
            foreach (Player.Food food in player.m_foods)
            {
                if (!string.IsNullOrEmpty(food.m_name) && Key(food.m_name) == key)
                    return true;
            }
            return false;
        }

        /// <summary>The custom-data key of a food, built once per prefab name.</summary>
        private static string Key(string foodName)
        {
            if (!keys.TryGetValue(foodName, out string key))
                keys[foodName] = key = EatingKeys.ActiveFood + foodName;
            return key;
        }
    }
}
