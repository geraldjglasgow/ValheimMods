namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// One creature a Summoner may call: its prefab name and the stars it comes with. The rule file's lists and the
    /// biome's fallback name no stars, so theirs is <see cref="OwnStars"/>: the aspect's own `stars` power, read as the
    /// wave is called. Another mod's list (<see cref="Registrations"/>) may name stars per creature.
    /// </summary>
    public readonly struct SummonPick
    {
        /// <summary>The stars that mean "the Summoner aspect's own `stars` power".</summary>
        public const int OwnStars = -1;

        public readonly string Prefab;
        public readonly int Stars;

        public SummonPick(string prefab, int stars)
        {
            Prefab = prefab;
            Stars = stars < 0 ? OwnStars : stars;
        }

        /// <summary>The stars this one comes with, given the aspect's own for a pick that names none.</summary>
        public int StarsOr(int own) => Stars < 0 ? own : Stars;
    }
}
