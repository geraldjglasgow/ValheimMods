namespace EliteCrafting.Affixes
{
    /// <summary>
    /// Every custom-data key the mod writes on an item (item-data.md section 3). The mod only ever reads, writes or
    /// removes these, never clears the dictionary, and leaves every other key (other mods') exactly as it was.
    /// </summary>
    public static class ItemKeys
    {
        public const string Prefix = "ecf_";

        /// <summary>Format version, integer; present whenever any other key is.</summary>
        public const string Version = "ecf_v";

        /// <summary>Rarity id; absent = Normal. Never written as the base rarity.</summary>
        public const string Rarity = "ecf_rarity";

        /// <summary><c>id:tier:value;id:tier:value</c>, the item's inscriptions in display order.</summary>
        public const string Affixes = "ecf_inscriptions";

        /// <summary>The same list under its name before 2026-10-01: read when the new key is absent, removed on write.</summary>
        public const string LegacyAffixes = "ecf_affixes";

        /// <summary>Sealed reason id (<see cref="SealedSerpent"/>); any value = sealed.</summary>
        public const string Sealed = "ecf_sealed";

        /// <summary>Reserved, never written (item-tier.md computes the ceiling); preserved when present.</summary>
        public const string Tier = "ecf_tier";

        public const int CurrentFormat = 1;

        public const char EntrySeparator = ';';
        public const char FieldSeparator = ':';

        public const string SealedSerpent = "serpent";

        /// <summary>The keys that make an item carry state (all but the version itself).</summary>
        public static readonly string[] StateKeys = { Rarity, Affixes, LegacyAffixes, Sealed, Tier };

        /// <summary>
        /// Keys of the systems the runes replaced on 2026-10-02 (binding, honing and tempering, sigils, sockets, gems,
        /// catalysts): never read, removed from an item the next time it is written.
        /// </summary>
        public static readonly string[] RetiredKeys = { "ecf_bound", "ecf_refine", "ecf_sigil", "ecf_sockets", "ecf_gems", "ecf_catalyst" };
    }
}
