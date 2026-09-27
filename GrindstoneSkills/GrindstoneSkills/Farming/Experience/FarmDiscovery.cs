using System;

namespace GrindstoneSkills
{
    /// <summary>
    /// The kinds of crop a character has picked, for the discovery bonus: pickable prefab names, comma-separated, in the
    /// player's own Player.m_customData under <see cref="Keys.HarvestedCrops"/>, saved with the character. Only the
    /// picker's own client reads or writes it.
    /// </summary>
    public static class FarmDiscovery
    {
        private const char Separator = ',';

        public static bool HasPicked(Player player, string kind)
        {
            if (player == null || string.IsNullOrEmpty(kind) || !player.m_customData.TryGetValue(Keys.HarvestedCrops, out string picked))
                return false;
            return Array.IndexOf((picked ?? "").Split(Separator), kind) >= 0;
        }

        /// <summary>Records the kind as picked. True when this was the character's first one.</summary>
        public static bool TryRecord(Player player, string kind)
        {
            if (player == null || string.IsNullOrEmpty(kind) || kind.IndexOf(Separator) >= 0 || HasPicked(player, kind))
                return false;
            player.m_customData.TryGetValue(Keys.HarvestedCrops, out string picked);
            player.m_customData[Keys.HarvestedCrops] = string.IsNullOrEmpty(picked) ? kind : picked + Separator + kind;
            return true;
        }
    }
}
