namespace EliteCrafting.Core
{
    /// <summary>
    /// The two ways an affix tier is numbered, and the only conversion between them. Players, tooltips, console
    /// commands and the YAML count down: T1 is an affix's strongest row, T7 its weakest. Everything inside (rolling,
    /// the tier window, stone floors, stored item data) uses the strength grade instead, which runs the other way and
    /// matches the item and biome tiers: grade 1 is Meadows strength, grade 7 Ashlands strength. Stored items keep the
    /// grade, so changing how tiers are shown never touches an item.
    /// </summary>
    public static class AffixTierNumbers
    {
        /// <summary>Affix tiers shipped; the grade of the strongest row.</summary>
        public const int Count = 7;

        /// <summary>The tier players see (1 strongest) for an internal strength grade (1 weakest).</summary>
        public static int Shown(int grade) => Count + 1 - grade;

        /// <summary>The internal strength grade (1 weakest) for a tier as players and the YAML write it (1 strongest).</summary>
        public static int Grade(int shown) => Count + 1 - shown;
    }
}
