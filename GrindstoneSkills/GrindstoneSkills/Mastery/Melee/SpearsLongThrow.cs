namespace GrindstoneSkills
{
    /// <summary>
    /// Long Throw, the Spears milestone at level 50, on the thrower's client (<see cref="MeleeMastery"/>): a thrown
    /// spear's hit deals <see cref="Damage"/> more. Held thrusts are left as they are.
    /// <list type="bullet">
    /// <item>A thrown spear is a Projectile owned by the thrower's client, which builds its hit there with
    /// HitData.m_ranged set and the spear's skill in m_skill (Projectile.OnHit); a held thrust's hit has m_ranged
    /// unset.</item>
    /// <item>The bonus is HitData.ApplyModifier before the hit leaves; the target's owner (Character.RPC_Damage) applies
    /// its resistances and armour after, and the stronger hit also adds more stagger, as any stronger hit does.</item>
    /// </list>
    /// </summary>
    public static class SpearsLongThrow
    {
        private const float Damage = 0.25f;

        public static void OnHit(HitData hit)
        {
            if (hit.m_ranged)
                hit.ApplyModifier(1f + Damage);
        }

        public static void Perk(SkillPage page) => page.Perk("Long Throw", MeleeMastery.Level,
            $"Thrown spears deal {SkillPage.Percent(Damage)} more damage. Held spear thrusts are unchanged.");
    }
}
