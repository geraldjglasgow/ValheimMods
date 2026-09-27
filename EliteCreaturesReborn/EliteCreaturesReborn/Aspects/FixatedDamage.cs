using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Fixated's hit, called from <see cref="Scaling.AspectDamage.Outgoing"/> on the victim's owner (a player's own
    /// client for a player) after the star scaling. It reads the mark from the boss's ZDO: the marked player takes
    /// `marked bonus` % more of the whole hit, and everyone else on the players' side - other players and tames - takes
    /// `others less` % less. Wild creatures caught in the boss's attacks are left as they are, and while nobody is
    /// marked every hit lands unchanged.
    /// </summary>
    internal static class FixatedDamage
    {
        public static void Apply(EliteController boss, Character victim, HitData hit)
        {
            ZNetView nview = boss.View;
            ZDOID mark = nview != null && nview.IsValid() ? FixatedMark.Get(nview.GetZDO()) : ZDOID.None;
            if (mark == ZDOID.None || !(victim.IsPlayer() || victim.IsTamed()))
            {
                return;
            }
            hit.ApplyModifier(victim.GetZDOID() == mark
                ? AspectMath.Boost(AspectMath.Power(Aspect.Fixated, Fields.MarkedBonus))
                : AspectMath.Cut(AspectMath.Power(Aspect.Fixated, Fields.OthersLess)));
        }
    }
}
