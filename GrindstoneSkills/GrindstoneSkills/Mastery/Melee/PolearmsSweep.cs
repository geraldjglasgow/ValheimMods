using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sweep, the Polearms milestone at level 50, on the attacker's client (<see cref="MeleeMastery"/>): an atgeir hit
    /// pushes the foe back <see cref="Knockback"/> harder.
    /// <list type="bullet">
    /// <item>Raises HitData.m_pushForce, which the game sends with the hit. The target's owner (Character.ApplyPushback,
    /// from Character.RPC_Damage) turns it into a push divided by the target's mass, kept only when stronger than the
    /// push already on it, so heavy foes still barely move. A blocking player's guard scales it down as usual.</item>
    /// </list>
    /// </summary>
    public static class PolearmsSweep
    {
        private const float Knockback = 0.3f;

        public static void OnHit(HitData hit) => hit.m_pushForce *= 1f + Knockback;

        public static void Perk(SkillPage page) => page.Perk(Lang.Get("gs_perk_polearmsweep"), MeleeMastery.Level,
            Lang.Format("gs_mastery_sweep_tip", SkillPage.Percent(Knockback)));
    }
}
