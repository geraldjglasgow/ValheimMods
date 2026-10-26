using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Writes an altar's draw on the altar's owner, with the players' choices first (<see cref="AltarPicks"/>): a chosen
    /// part is set to the choice whenever the altar shows anything else, a part on Random rolls when asked to (a shift, or
    /// a choice just set back to Random) or when it was never rolled. A chosen Bountiful keeps the extras a player chose
    /// beside it and rolls the rest with each roll. When every part that is on rolls at once, one roll in <see cref="PlainOdds"/> is the plain
    /// boss - no stars, no aspect - whatever the rule file's chances, so Random can always land on the boss as the game
    /// ships it; never twice running, so a shift still always shows a change.
    /// </summary>
    internal static class AltarRoller
    {
        /// <summary>One roll of the whole fight in this many is the plain boss, beside the plain fights the rules roll.</summary>
        public const int PlainOdds = 10;

        /// <summary>Both parts, each only while it is on; <paramref name="rollStars"/> and <paramref name="rollAspect"/>
        /// ask a part on Random to roll again.</summary>
        public static void Draw(ZDO zdo, string boss, bool rollStars, bool rollAspect)
        {
            bool starsOn = RuleState.Active.Boss.Enabled;
            bool aspectsOn = RuleState.Active.Boss.Aspects.Enabled;
            if (!starsOn && !aspectsOn)
            {
                return;
            }
            if (StarsRoll(zdo, starsOn, rollStars) && AspectRolls(zdo, boss, aspectsOn, rollAspect)
                && !ShowsPlain(zdo, starsOn, aspectsOn) && Random.Range(0, PlainOdds) == 0)
            {
                Plain(zdo, starsOn, aspectsOn);
                return;
            }
            if (starsOn)
            {
                Stars(zdo, boss, rollStars);
            }
            if (aspectsOn)
            {
                Aspects(zdo, boss, rollAspect);
            }
        }

        private static void Stars(ZDO zdo, string boss, bool roll)
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

        private static void Aspects(ZDO zdo, string boss, bool roll)
        {
            bool rolled = AspectStore.AltarRolled(zdo);
            Aspect current = AspectStore.GetAltarAspect(zdo);
            if (AltarPicks.AspectOf(zdo, boss) is Aspect chosen)
            {
                Chosen(zdo, boss, chosen, !rolled || current != chosen, roll);
            }
            else if (roll || !rolled)
            {
                AspectStore.SetAltarAspects(zdo, AspectRoller.RollBoss(boss, rolled ? current : (Aspect?)null));
            }
        }

        // A chosen aspect is set when the bowl shows another (or nothing yet); a chosen Bountiful also at a roll and
        // whenever the bowl lacks an extra a player chose, keeping those and rolling the rest.
        private static void Chosen(ZDO zdo, string boss, Aspect chosen, bool other, bool roll)
        {
            if (chosen != Aspect.Bountiful)
            {
                if (other)
                {
                    AspectStore.SetAltarAspects(zdo, new BossAspects(chosen, 0));
                }
                return;
            }
            int keep = AltarPicks.Extras(zdo, boss);
            if (other || roll || (AspectStore.GetAltarAspects(zdo).Extras & keep) != keep)
            {
                RedrawExtras(zdo, boss, keep);
            }
        }

        /// <summary>A chosen Bountiful on the bowl with <paramref name="keep"/>, the extras a player chose, and the rest
        /// rolled again; owner only.</summary>
        public static void RedrawExtras(ZDO zdo, string boss, int keep) =>
            AspectStore.SetAltarAspects(zdo, new BossAspects(Aspect.Bountiful, AspectRoller.RollExtras(boss, keep)));

        // A part that is off never holds the plain boss back; one that is on must be on Random and about to roll.
        private static bool StarsRoll(ZDO zdo, bool on, bool roll) =>
            !on || (AltarPicks.Stars(zdo) == null && (roll || !AspectStore.AltarStarsRolled(zdo)));

        private static bool AspectRolls(ZDO zdo, string boss, bool on, bool roll) =>
            !on || (AltarPicks.AspectOf(zdo, boss) == null && (roll || !AspectStore.AltarRolled(zdo)));

        private static bool ShowsPlain(ZDO zdo, bool starsOn, bool aspectsOn) =>
            (!starsOn || (AspectStore.AltarStarsRolled(zdo) && AspectStore.GetAltarStars(zdo) == 0))
            && (!aspectsOn || (AspectStore.AltarRolled(zdo) && AspectStore.GetAltarAspect(zdo) == Aspect.None));

        private static void Plain(ZDO zdo, bool starsOn, bool aspectsOn)
        {
            if (starsOn)
            {
                AspectStore.SetAltarStars(zdo, 0);
            }
            if (aspectsOn)
            {
                AspectStore.SetAltarAspects(zdo, BossAspects.None);
            }
        }
    }
}
