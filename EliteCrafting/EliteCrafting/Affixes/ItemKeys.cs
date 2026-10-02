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

        /// <summary>Rarity id; absent = Common. Never written as the base rarity.</summary>
        public const string Rarity = "ecf_rarity";

        /// <summary><c>id:tier:value;id:tier:value</c>, the item's inscriptions in display order.</summary>
        public const string Affixes = "ecf_inscriptions";

        /// <summary>The same list under its name before 2026-10-01: read when the new key is absent, removed on write.</summary>
        public const string LegacyAffixes = "ecf_affixes";

        /// <summary>Id of the affix locked by the Stone of Binding.</summary>
        public const string Bound = "ecf_bound";

        /// <summary>Honed/tempered bonus in percent points (survives Unmaking).</summary>
        public const string Refine = "ecf_refine";

        /// <summary>Sealed reason id (<see cref="SealedSerpent"/>, <see cref="SealedReflection"/>); any value = sealed.</summary>
        public const string Sealed = "ecf_sealed";

        /// <summary>Stone id of the pending sigil.</summary>
        public const string Sigil = "ecf_sigil";

        /// <summary>Reserved, never written (item-tier.md computes the ceiling); preserved when present.</summary>
        public const string Tier = "ecf_tier";

        /// <summary>Socket count, integer (sockets.md section 2); absent = none.</summary>
        public const string Sockets = "ecf_sockets";

        /// <summary><c>gem:affix:grade:value;...</c>, the filled sockets oldest first (sockets.md section 2).</summary>
        public const string Gems = "ecf_gems";

        /// <summary><c>family:quality</c>, the catalyst on the item (sockets.md section 5).</summary>
        public const string Catalyst = "ecf_catalyst";

        public const int CurrentFormat = 1;

        public const char EntrySeparator = ';';
        public const char FieldSeparator = ':';

        public const string SealedSerpent = "serpent";
        public const string SealedReflection = "reflection";

        /// <summary>The keys that make an item carry state (all but the version itself).</summary>
        public static readonly string[] StateKeys = { Rarity, Affixes, LegacyAffixes, Bound, Refine, Sealed, Sigil, Tier, Sockets, Gems, Catalyst };
    }
}
