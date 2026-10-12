namespace GrindstoneSkills
{
    /// <summary>
    /// Brawler, the Unarmed milestone at level 50, on the attacker's client (<see cref="MeleeMastery"/>): an unarmed hit
    /// deals <see cref="Damage"/> more.
    /// <list type="bullet">
    /// <item>Bare fists are the player's unarmed weapon, whose skill is Unarmed, so their hits carry m_skill Unarmed
    /// (Attack.DoMeleeAttack copies the weapon's skill; the game's own Character.RPC_Damage tells a bare fist from
    /// other Unarmed hits by the hit's status effect mark 99999). The game's claws (Flesh Rippers and the other fist
    /// weapons) train Unarmed too, so Brawler counts them as well.</item>
    /// <item>The bonus is HitData.ApplyModifier before the hit leaves; the target's owner (Character.RPC_Damage) applies
    /// its resistances and armour after, and the stronger hit also adds more stagger, as any stronger hit does.</item>
    /// </list>
    /// </summary>
    public static class UnarmedBrawler
    {
        private const float Damage = 0.5f;

        public static void OnHit(HitData hit) => hit.ApplyModifier(1f + Damage);

        public static void Perk(SkillPage page) => page.Perk("Brawler", MeleeMastery.Level,
            $"Unarmed hits, bare fists and claws alike, deal {SkillPage.Percent(Damage)} more damage.");
    }
}
