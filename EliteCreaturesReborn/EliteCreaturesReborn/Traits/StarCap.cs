using EliteCreaturesReborn.Rules;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// The most stars a creature can roll in a biome at a world tier: the difficulty preset's cap there, or under Custom the
    /// last star count its biome's `star chances` lists. A raid's extra stars stop at it.
    /// </summary>
    public static class StarCap
    {
        public static int For(RuleSet set, BiomeRules rules, int tier, Heightmap.Biome biome) =>
            set.Difficulty == Difficulty.Custom
                ? rules.StarCeiling
                : PresetCell.For(set.Difficulty, tier, PresetTables.Rank(biome)).Cap;
    }
}
