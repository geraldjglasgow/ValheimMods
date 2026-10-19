using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Bleed, the Knives milestone at level 50, on the attacker's client (<see cref="MeleeMastery"/>): a knife hit also
    /// carries poison worth <see cref="Poison"/> of its damage, counted before the target's resistances.
    /// <list type="bullet">
    /// <item>The poison is added to HitData.m_damage.m_poison, which the game sends with the hit. The target's owner
    /// (Character.RPC_Damage) scales it with the rest of the hit (a backstab, a staggered creature's double damage),
    /// applies the target's poison resistance (immune foes take none), then starts the game's poison effect with it
    /// (SE_Poison.AddDamage, a tick every second). Poison never shows in the hit's damage number.</item>
    /// <item>The game's poison does not stack: a new dose replaces the one running only when it is bigger, so quick hits
    /// keep one dose going rather than adding up.</item>
    /// <item>Counted from every damage type but chop and pickaxe, the tool damage creatures ignore. Any other milestone
    /// that raised the hit before this one (Assassin, Counter) raises the poison with it.</item>
    /// </list>
    /// </summary>
    public static class KnivesBleed
    {
        private const float Poison = 0.2f;

        public static void OnHit(HitData hit)
        {
            float damage = hit.m_damage.GetTotalDamage() - hit.m_damage.m_chop - hit.m_damage.m_pickaxe;
            if (damage > 0f)
                hit.m_damage.m_poison += damage * Poison;
        }

        public static void Perk(SkillPage page) => page.Perk(Lang.Get("gs_perk_bleed"), MeleeMastery.Level,
            Lang.Format("gs_mastery_bleed_tip", SkillPage.Percent(Poison)));
    }
}
