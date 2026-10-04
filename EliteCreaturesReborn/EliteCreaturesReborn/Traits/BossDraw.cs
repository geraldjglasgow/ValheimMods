using EliteCreaturesReborn.Rules;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// Everything one boss fight is decided with before there is a boss to carry it: the star count and the aspects (a
    /// Bountiful one's extras within them). It is what an altar shows and locks at the offering and what an altar-less boss
    /// rolls, carried as one value so the stars can never be parted from the aspects they were shown with on the way from
    /// the bowl to the boss's traits.
    /// </summary>
    public readonly struct BossDraw
    {
        public readonly int Stars;
        public readonly BossAspects Aspects;

        public BossDraw(int stars, BossAspects aspects)
        {
            Stars = stars;
            Aspects = aspects;
        }

        /// <summary>The traits a boss is born with: these aspects on these stars.</summary>
        public CreatureTraits ToTraits() => Aspects.ToTraits(Stars);

        /// <summary>A fresh draw on the live rules, each part only while it is on: no stars with boss stars off, no aspect
        /// with aspects off.</summary>
        public static BossDraw Roll(string bossPrefab) => new BossDraw(RollStars(), AspectRoller.RollBoss(bossPrefab, null));

        /// <summary>The boss table's star roll, or 0 when boss stars are off.</summary>
        public static int RollStars()
        {
            BossRules boss = RuleState.Active.Boss;
            return boss.Enabled ? TraitRoller.RollStars(BossView.For(boss)) : 0;
        }
    }
}
