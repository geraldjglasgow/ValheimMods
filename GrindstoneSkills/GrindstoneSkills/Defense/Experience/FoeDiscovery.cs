using System;

namespace GrindstoneSkills
{
    /// <summary>
    /// The kinds of creature a character has blocked, for the first-block bonus: name tokens ("$enemy_greydwarf", so
    /// every greydwarf counts once, however it is spawned), comma-separated, in the player's own Player.m_customData
    /// under <see cref="Keys.BlockedFoes"/>. Player.Save writes m_customData into the character file and Player.Load
    /// reads it back, so the list survives relogs and follows the character between worlds. Only the blocker's own
    /// client reads or writes it. A new entry floats "First block: Greydwarf" over the creature.
    /// </summary>
    public static class FoeDiscovery
    {
        private const char Separator = ',';

        /// <summary>Records the creature's kind as blocked. True when this was the character's first block against it.</summary>
        public static bool TryRecord(Player player, Character foe)
        {
            string identity = foe != null && !foe.IsPlayer() ? foe.m_name : null;
            if (player == null || string.IsNullOrEmpty(identity) || identity.IndexOf(Separator) >= 0 || HasBlocked(player, identity))
                return false;
            player.m_customData.TryGetValue(Keys.BlockedFoes, out string blocked);
            player.m_customData[Keys.BlockedFoes] = string.IsNullOrEmpty(blocked) ? identity : blocked + Separator + identity;
            DefenseCallout.Show(foe.GetTopPoint(), $"First block: {Localization.instance.Localize(identity)}");
            return true;
        }

        public static bool HasBlocked(Player player, string identity)
        {
            if (!player.m_customData.TryGetValue(Keys.BlockedFoes, out string blocked))
                return false;
            return Array.IndexOf((blocked ?? "").Split(Separator), identity) >= 0;
        }
    }
}
