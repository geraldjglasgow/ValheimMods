namespace EliteEquipment.Casters
{
    /// <summary>
    /// The spell last picked from the spell ring with a wand (user, 2026-11-02: "You can click RMB quick to recast the last
    /// spell you selected"), saved on the wand itself (item custom data <see cref="Key"/>: one spell key), so two wands
    /// remember their own. A quick right mouse tap and middle mouse cast it again (<see cref="SpellRing"/>). A new wand
    /// has none. A wand from before the ring, with no <see cref="Key"/> but the two slots it held (<see cref="OldKey"/>),
    /// takes its first slot; the old key goes the first time a spell is picked. The spell casts only while its page is in
    /// the spellbook in the wielder's left hand (<see cref="BookPages"/>).
    /// </summary>
    public static class LastSpell
    {
        public const string Key = "EliteEquipment.lastSpell";
        public const string OldKey = "EliteEquipment.loadout";

        /// <summary>The saved spell key ("" for none), compared to see whether it changed.</summary>
        public static string KeyOf(ItemDrop.ItemData wand)
        {
            if (wand.m_customData.TryGetValue(Key, out string saved))
                return saved;
            if (!wand.m_customData.TryGetValue(OldKey, out string slots))
                return "";
            int comma = slots.IndexOf(',');
            return comma >= 0 ? slots.Substring(0, comma) : slots;
        }

        /// <summary>The saved spell, whether or not the held book has its page; null for none.</summary>
        public static Spell Saved(ItemDrop.ItemData wand) => wand != null ? Spells.Find(KeyOf(wand)) : null;

        /// <summary>The saved spell if the player can cast it (its page in the held book), or null.</summary>
        public static Spell In(Player player, ItemDrop.ItemData wand)
        {
            Spell spell = Saved(wand);
            return BookPages.Castable(player, spell) ? spell : null;
        }

        public static void Set(ItemDrop.ItemData wand, Spell spell)
        {
            wand.m_customData[Key] = spell.Key;
            wand.m_customData.Remove(OldKey);
        }
    }
}
