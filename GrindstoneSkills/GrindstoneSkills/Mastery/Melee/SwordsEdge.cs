namespace GrindstoneSkills
{
    /// <summary>
    /// Edge, the Swords milestone at level 50, on the attacker's client (<see cref="MeleeMastery"/>): a sword hit on a
    /// staggered foe deals <see cref="Damage"/> more.
    /// <list type="bullet">
    /// <item>The stagger is read with the game's own Character.IsStaggering, the state of the target's animator. The
    /// target's owner starts a stagger with the "stagger" trigger of its ZSyncAnimation, which the game sends to every
    /// peer, so a client that does not own the target plays the same stagger a moment later and reads it too. At the
    /// edges of a stagger the two can disagree for about the network delay: Edge may then add to a hit the owner no
    /// longer counts as staggered, or miss one it does.</item>
    /// <item>The bonus is HitData.ApplyModifier before the hit leaves, the way the game scales a hit for backstabs. The
    /// owner (Character.RPC_Damage) then doubles damage to a staggered creature as usual, and its resistances and
    /// armour come after. A stronger hit also adds that much more to the stagger bar, as any stronger hit does.</item>
    /// </list>
    /// </summary>
    public static class SwordsEdge
    {
        private const float Damage = 0.25f;

        public static void OnHit(Character target, HitData hit)
        {
            if (target.IsStaggering())
                hit.ApplyModifier(1f + Damage);
        }

        public static void Perk(SkillPage page) => page.Perk("Edge", MeleeMastery.Level,
            $"Sword hits on a staggered foe deal {SkillPage.Percent(Damage)} more damage, on top of the double damage the game already gives a staggered creature.");
    }
}
