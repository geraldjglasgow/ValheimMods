namespace EliteCrafting.Affixes
{
    /// <summary>
    /// The chain of format migrations (item-data.md section 8), each a pure step over the parsed state, run in memory
    /// on read. The dictionary is untouched until the item is next written for its own reasons, which then writes the
    /// current form. Format 1 is the first: the chain is empty. A new step ships with a test holding a frozen v(n)
    /// dictionary and its expected v(n+1) form.
    /// </summary>
    internal static class ItemMigrations
    {
        public static StateData Upgrade(StateData state)
        {
            string? rarity = RenamedRarity(state.RarityId);
            if (state.Newer || (state.Format >= ItemKeys.CurrentFormat && rarity == state.RarityId))
            {
                return state;
            }
            StateData upgraded = state.Copy();
            upgraded.Format = ItemKeys.CurrentFormat;
            upgraded.RarityId = rarity;
            return upgraded;
        }

        /// <summary>
        /// The six built-in rarities before the runes (2026-10-02) in today's three: Uncommon is Magic, Epic,
        /// Legendary and Mythic are Rare, Common is Normal (never stored). Any other id is kept as it is.
        /// </summary>
        private static string? RenamedRarity(string? id) => id switch
        {
            "common" => null,
            "uncommon" => "magic",
            "epic" or "legendary" or "mythic" => "rare",
            _ => id,
        };
    }
}
