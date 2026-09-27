using System;

namespace GrindstoneSkills
{
    /// <summary>
    /// The dishes a character has made, for the discovery bonus: prefab names, comma-separated, in the player's own
    /// Player.m_customData under <see cref="Keys.MadeDishes"/>. Player.Save writes m_customData into the character
    /// file and Player.Load reads it back, so the list survives relogs and follows the character between worlds.
    /// </summary>
    public static class DishDiscovery
    {
        private const char Separator = ',';

        /// <summary>Whether the character has made this dish before.</summary>
        public static bool HasMade(Player player, string dishPrefab)
        {
            if (player == null || string.IsNullOrEmpty(dishPrefab) || !player.m_customData.TryGetValue(Keys.MadeDishes, out string made))
                return false;
            return Array.IndexOf((made ?? "").Split(Separator), dishPrefab) >= 0;
        }

        /// <summary>Records the dish as made. True when this was the character's first time.</summary>
        public static bool TryRecord(Player player, string dishPrefab)
        {
            if (player == null || string.IsNullOrEmpty(dishPrefab) || dishPrefab.IndexOf(Separator) >= 0 || HasMade(player, dishPrefab))
                return false;
            player.m_customData.TryGetValue(Keys.MadeDishes, out string made);
            player.m_customData[Keys.MadeDishes] = string.IsNullOrEmpty(made) ? dishPrefab : made + Separator + dishPrefab;
            return true;
        }
    }
}
