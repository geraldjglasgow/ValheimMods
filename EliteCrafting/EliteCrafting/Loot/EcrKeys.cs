namespace EliteCrafting.Loot
{
    /// <summary>
    /// Elite Creatures Reborn's creature ZDO keys that EliteCrafting reads (ecr-integration.md section 3). ECR is our own
    /// mod in this repository; the names were read from its source (<c>Traits/TraitKeys.cs</c>, <c>Traits/AspectStore.cs</c>)
    /// and are the whole contract between the two mods: no ECR type or assembly is referenced, and a renamed key only
    /// degrades to "no ECR data" (section 8). ECR writes them with the string overloads of <c>ZDO.Set</c>, which hash the
    /// name with <c>GetStableHashCode</c>, so the hashes below address the same values. Read only, never written.
    /// </summary>
    internal static class EcrKeys
    {
        /// <summary>Bool: ECR has rolled this creature's traits (<c>TraitStore.Save</c>).</summary>
        public const string Resolved = "ecr_resolved";

        /// <summary>Int: ECR's star count, 0 to its ceiling (<c>TraitStore.Save</c>).</summary>
        public const string Stars = "ecr_stars";

        /// <summary>Bool: the Cloven twin or a Phantom husk, which ECR strips of loot (<c>AspectStore.MarkTwin</c>/<c>MarkHusk</c>).</summary>
        public const string Worthless = "ecr_asp_worthless";

        /// <summary>
        /// Int 0-7: the world tier the creature was rolled at. <b>Not written by ECR yet</b> (DECISIONS ECR-6, decided by
        /// the user 2026-09-24: ECR writes it when it builds world tiers): until an ECR release writes it, it reads as
        /// absent and the tier terms stay inert. If ECR picks another name, this constant is the only thing that changes.
        /// </summary>
        public const string Tier = "ecr_tier";

        public static readonly int ResolvedHash = Resolved.GetStableHashCode();
        public static readonly int StarsHash = Stars.GetStableHashCode();
        public static readonly int WorthlessHash = Worthless.GetStableHashCode();
        public static readonly int TierHash = Tier.GetStableHashCode();
    }
}
