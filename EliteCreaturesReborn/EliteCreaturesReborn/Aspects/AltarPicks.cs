using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// What a player may choose at a boss altar, and what the altar's stored choices mean right now: the star counts the
    /// boss table can roll (a chance above 0) and the aspects the boss's rotation holds, both read from the live rules,
    /// and always no stars and no aspect, since the altar can always roll the plain boss (<see cref="AltarRoller"/>);
    /// beside a chosen Bountiful, up to `extra aspects` of the aspects it can carry. A stored choice the rules no longer allow (a reloaded rule file, boss stars or aspects switched off), and every
    /// choice while the Altar Choice setting is off, counts as Random: the altar rolls that part as it always has.
    /// </summary>
    internal static class AltarPicks
    {
        /// <summary>The stored value of a part left on Random.</summary>
        public const int Random = -1;

        /// <summary>The star counts the boss table can roll and always none, fewest first; empty while boss stars are off.</summary>
        public static List<int> StarChoices()
        {
            List<int> counts = new List<int>();
            for (int stars = 0; stars <= RuleState.Active.Boss.StarCeiling; stars++)
            {
                if (AllowsStars(stars))
                {
                    counts.Add(stars);
                }
            }
            return counts;
        }

        /// <summary>The aspects this boss can roll, in catalog order, `none` first whatever its chance; empty while
        /// aspects are off.</summary>
        public static List<Aspect> AspectChoices(string boss)
        {
            List<Aspect> choices = AspectRoller.Choices(boss);
            if (RuleState.Active.Boss.Aspects.Enabled && !choices.Contains(Aspect.None))
            {
                choices.Insert(0, Aspect.None);
            }
            return choices;
        }

        /// <summary>The chosen star count while the setting and the rules allow it; null for Random.</summary>
        public static int? Stars(ZDO zdo)
        {
            int pick = AspectStore.GetPickedStars(zdo);
            return AltarSettings.Enabled && AllowsStars(pick) ? pick : (int?)null;
        }

        /// <summary>The chosen aspect while the setting and the rules allow it; null for Random.</summary>
        public static Aspect? AspectOf(ZDO zdo, string boss)
        {
            int pick = AspectStore.GetPickedAspect(zdo);
            return AltarSettings.Enabled && AllowsAspect(boss, pick) ? (Aspect)pick : (Aspect?)null;
        }

        public static bool AllowsStars(int stars)
        {
            BossRules boss = RuleState.Active.Boss;
            return boss.Enabled && (stars == 0 || (stars > 0 && stars < boss.StarChances.Length && boss.StarChances[stars] > 0f));
        }

        public static bool AllowsAspect(string boss, int aspect) => aspect >= 0 && AspectChoices(boss).Contains((Aspect)aspect);

        /// <summary>The extras chosen beside a chosen Bountiful, as the rules allow them now, one bit per aspect value; 0
        /// while they all roll, and always 0 while Bountiful is not the chosen aspect.</summary>
        public static int Extras(ZDO zdo, string boss) =>
            AspectOf(zdo, boss) == Aspect.Bountiful ? TrimExtras(boss, AspectStore.GetPickedExtras(zdo)) : 0;

        /// <summary>Only the aspects a Bountiful boss of this prefab can carry, at most `extra aspects` of them and one
        /// that brings more bodies, taken in catalog order.</summary>
        public static int TrimExtras(string boss, int extras)
        {
            int kept = 0;
            int room = AspectRoller.ExtraCount();
            foreach (Aspect aspect in AspectRoller.ExtraChoices(boss))
            {
                if (room > 0 && (extras & (1 << (int)aspect)) != 0 && AspectRoller.Fits(kept, aspect))
                {
                    kept |= 1 << (int)aspect;
                    room--;
                }
            }
            return kept;
        }

        /// <summary>True while some part of the altar still rolls at each shift: a part on Random, or a chosen Bountiful
        /// with extras left to roll. False when the choices fix the whole fight.</summary>
        public static bool AnyRolls(ZDO zdo, string boss)
        {
            bool starsRoll = RuleState.Active.Boss.Enabled && Stars(zdo) == null;
            Aspect? aspect = RuleState.Active.Boss.Aspects.Enabled ? AspectOf(zdo, boss) : Aspect.None;
            return starsRoll || aspect == null
                || (aspect == Aspect.Bountiful && AspectRoller.CanFill(boss, TrimExtras(boss, AspectStore.GetPickedExtras(zdo))));
        }
    }
}
