namespace EliteCrafting.Effects
{
    /// <summary>
    /// The Phase 3 effects (features/effects-phase3.md): the inscriptions added with item classes and tier ladders.
    /// Each area registers its own ids in its own partial file; an area not built yet registers nothing.
    /// </summary>
    internal static partial class EffectCatalog
    {
        private static void RegisterPhase3()
        {
            RegisterPhase3Combat();
            RegisterPhase3Survival();
            RegisterPhase3Gathering();
            RegisterPhase3Throwing();
        }

        static partial void RegisterPhase3Combat();

        static partial void RegisterPhase3Survival();

        static partial void RegisterPhase3Gathering();

        static partial void RegisterPhase3Throwing();
    }
}
