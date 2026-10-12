namespace GrindstoneSkills
{
    /// <summary>
    /// Crush, the Clubs milestone at level 50, on the attacker's client (<see cref="MeleeMastery"/>): a club hit builds
    /// <see cref="Stagger"/> more stagger.
    /// <list type="bullet">
    /// <item>Raises HitData.m_staggerMultiplier, which the game sends with the hit. The target's owner
    /// (Character.RPC_Damage) adds the hit's blunt, slash, pierce and lightning damage after resistances, times that
    /// multiplier, to the target's stagger bar, and staggers it when the bar passes its threshold (max health times the
    /// creature's stagger factor; a creature with none never staggers).</item>
    /// <item>A multiplier of <see cref="ForcedStagger"/> or more is the game's mark for a hit that always staggers. Crush
    /// never raises a hit to that mark: such a hit keeps the multiplier the attack gave it.</item>
    /// </list>
    /// </summary>
    public static class ClubsCrush
    {
        private const float Stagger = 0.3f;
        private const float ForcedStagger = 100f;

        public static void OnHit(HitData hit)
        {
            float raised = hit.m_staggerMultiplier * (1f + Stagger);
            if (raised < ForcedStagger)
                hit.m_staggerMultiplier = raised;
        }

        public static void Perk(SkillPage page) => page.Perk("Crush", MeleeMastery.Level,
            $"Club hits build {SkillPage.Percent(Stagger)} more stagger, so foes are knocked off balance sooner.");
    }
}
