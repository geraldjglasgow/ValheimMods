namespace EliteCraftingLink
{
    /// <summary>
    /// The prefab names of EliteCrafting's items, as it publishes them (its <c>features/prefabs.md</c> section 3 and
    /// <c>Table/EssenceItem</c>: <c>ECF_</c> plus the rune's PascalCase id; names and hashes never change between
    /// versions, so existing stacks carry over), for a mod that hands them out or counts them. Plain constants: nothing
    /// of EliteCrafting is touched. Look each up in the game's <c>ObjectDB</c> before use and skip one it does not have
    /// (EliteCrafting absent, or a rune it no longer makes); <see cref="CraftingLink.Present"/> says whether it is
    /// installed at all.
    /// </summary>
    public static class CraftingPrefabs
    {
        /// <summary>Awakening Rune: Normal to Magic, two inscriptions.</summary>
        public const string Awakening = "ECF_Awakening";

        /// <summary>Recasting Rune: a Magic item's two inscriptions rolled again.</summary>
        public const string Recasting = "ECF_Recasting";

        /// <summary>Ascension Rune: Magic to Rare, a third inscription.</summary>
        public const string Ascension = "ECF_Ascension";

        /// <summary>Cleansing Rune: back to Normal.</summary>
        public const string Cleansing = "ECF_Cleansing";

        /// <summary>Sealed Rune (id <c>serpent</c>): corrupts and seals an item.</summary>
        public const string Sealed = "ECF_Serpent";

        /// <summary>Essence, the Rune Table's pool as an item (stacks to 100).</summary>
        public const string Essence = "ECF_Essence";
    }
}
