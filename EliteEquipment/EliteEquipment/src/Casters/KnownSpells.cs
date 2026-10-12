using System.Collections.Generic;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The spells a character knows, saved on the character (player custom data <see cref="Key"/>: spell keys separated by
    /// commas; the game writes it into the character file). Mend, and any spell without an item, is always known.
    /// </summary>
    public static class KnownSpells
    {
        public const string Key = "EliteEquipment.spells";

        public static bool Knows(Player player, Spell spell)
        {
            if (spell == null)
                return false;
            return spell.KnownByAll || (player != null && Saved(player).Contains(spell.Key));
        }

        public static List<Spell> Of(Player player)
        {
            var known = new List<Spell>();
            foreach (Spell spell in Spells.All)
            {
                if (Knows(player, spell))
                    known.Add(spell);
            }
            return known;
        }

        /// <summary>Teaches the spell; false when the character already knows it.</summary>
        public static bool Learn(Player player, Spell spell)
        {
            if (player == null || Knows(player, spell))
                return false;
            List<string> saved = Saved(player);
            saved.Add(spell.Key);
            player.m_customData[Key] = string.Join(",", saved);
            return true;
        }

        private static List<string> Saved(Player player)
        {
            var keys = new List<string>();
            if (player.m_customData.TryGetValue(Key, out string text) && !string.IsNullOrEmpty(text))
            {
                foreach (string key in text.Split(','))
                {
                    if (key.Length > 0)
                        keys.Add(key);
                }
            }
            return keys;
        }
    }
}
