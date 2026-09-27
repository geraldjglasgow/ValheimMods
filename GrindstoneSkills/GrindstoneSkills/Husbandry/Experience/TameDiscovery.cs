using System;

namespace GrindstoneSkills
{
    /// <summary>
    /// The kinds of creature a character has tamed, for the discovery multiplier: prefab names, comma-separated, in the
    /// player's own Player.m_customData under <see cref="Keys.TamedKinds"/>, saved with the character like the dish
    /// discovery list.
    /// </summary>
    public static class TameDiscovery
    {
        private const char Separator = ',';

        public static bool HasTamed(Player player, string prefab)
        {
            if (player == null || string.IsNullOrEmpty(prefab) || !player.m_customData.TryGetValue(Keys.TamedKinds, out string tamed))
                return false;
            return Array.IndexOf((tamed ?? "").Split(Separator), prefab) >= 0;
        }

        /// <summary>Records the kind as tamed. True when this was the character's first of that kind.</summary>
        public static bool TryRecord(Player player, string prefab)
        {
            if (player == null || string.IsNullOrEmpty(prefab) || prefab.IndexOf(Separator) >= 0 || HasTamed(player, prefab))
                return false;
            player.m_customData.TryGetValue(Keys.TamedKinds, out string tamed);
            player.m_customData[Keys.TamedKinds] = string.IsNullOrEmpty(tamed) ? prefab : tamed + Separator + prefab;
            return true;
        }
    }
}
