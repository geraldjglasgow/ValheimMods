namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Presents the boss table as a <see cref="BiomeRules"/> so a boss travels the same scaling path a creature does -
    /// <c>StatMath</c>, <c>DamageMath</c> and the drop patch all read <c>Rules.Star</c> and need no boss branch of
    /// their own. Mutation chance is zero throughout, which is what keeps bosses free of mutations by construction
    /// rather than by a check every caller would have to remember.
    /// </summary>
    public static class BossView
    {
        public static BiomeRules For(BossRules boss)
        {
            return new BiomeRules
            {
                StarChances = boss.StarChances,
                Star = boss.Star,
                MutationChance = new[] { 0f },
                LargeStarPower = 1f, // no mutations to enhance, so nothing to enhance them by
            };
        }
    }
}
