using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Writes one part of an altar's draw on the altar's owner, with the players' choices first (<see cref="AltarPicks"/>):
    /// a chosen part is set to the choice whenever the altar shows anything else, a part on Random rolls when
    /// <c>roll</c> is true (a shift, or a choice just set back to Random) or when it was never rolled. A chosen Bountiful
    /// rolls its extras with each roll, since nobody chose them.
    /// </summary>
    internal static class AltarRoller
    {
        public static void Stars(ZDO zdo, string boss, bool roll)
        {
            bool rolled = AspectStore.AltarStarsRolled(zdo);
            if (AltarPicks.Stars(zdo) is int chosen)
            {
                if (!rolled || AspectStore.GetAltarStars(zdo) != chosen)
                {
                    AspectStore.SetAltarStars(zdo, chosen);
                }
            }
            else if (roll || !rolled)
            {
                AspectStore.SetAltarStars(zdo, BossDraw.RollStars(BossKills.StarBoost(boss))); // leaned by past defeats
            }
        }

        public static void Aspects(ZDO zdo, string boss, bool roll)
        {
            bool rolled = AspectStore.AltarRolled(zdo);
            Aspect current = AspectStore.GetAltarAspect(zdo);
            if (AltarPicks.AspectOf(zdo, boss) is Aspect chosen)
            {
                if (!rolled || current != chosen || (roll && chosen == Aspect.Bountiful))
                {
                    AspectStore.SetAltarAspects(zdo, new BossAspects(chosen, chosen == Aspect.Bountiful ? AspectRoller.RollExtras(boss) : 0));
                }
            }
            else if (roll || !rolled)
            {
                AspectStore.SetAltarAspects(zdo, AspectRoller.RollBoss(boss, rolled ? current : (Aspect?)null));
            }
        }
    }
}
