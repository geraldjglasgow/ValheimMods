using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the hammock says: its description in the hammer, the live lines its build panel shows while it is in hand
    /// (what the aim would set, the hammock planned and what it is hung from, or why not), and what a click did. Plain text.
    /// </summary>
    public static class KwHammockWords
    {
        private const string CancelKeys = " (right click or Esc cancels)";

        public static readonly string Description =
            $"A hide hammock, hung by its two ends: click where one end goes, then the other, {KwHammockRules.MinApart:0.0} to {KwHammockRules.MaxApart:0} m apart. " +
            "An end on a floor, a deck or the ground stands on a post; one on a wall, beam, ceiling, post or mast hangs from an iron hook, " +
            "and the hammock comes down if what holds a hook goes. A bed to sleep in and wake at, on land or aboard a launched ship, " +
            "where it rides the ship and needs no fire.";

        public const string AimFirst = "Aim at a floor or the ground for a post, or at a wall, beam or ceiling for an iron hook";
        public const string AimSecond = "Aim where the other end goes";
        public const string OnWater = "Not on the water";
        public const string Moving = "Not on something that moves";
        public const string TooFarAway = "Too far away";
        public const string TwoShips = "Both ends on the same ship, or both off it";
        public const string ShipNotReady = "This ship is not ready to build on yet; try again in a moment";
        public const string ShipGone = "The ship the first end was on is gone; hammock cancelled";
        public const string NoFloor = "Hang it over a floor, a deck or the ground";
        public const string Cancelled = "Hammock cancelled";

        public static string TooClose(float apart) => $"Too close: the ends are {apart:0.0} m apart ({KwHammockRules.MinApart:0.0} m at least)";

        public static string TooFar(float apart) => $"Too far: the ends are {apart:0.0} m apart ({KwHammockRules.MaxApart:0} m at most)";

        public static string Uneven(float rise, float most) => $"Too uneven: one end is {rise:0.0} m higher than the other ({most:0.0} m at most)";

        public static string TooLow(float over) => $"Too low: the sling would hang {Mathf.Max(0f, over):0.00} m over the floor ({KwHammockRules.Clear:0.0} m at least)";

        public static string TooHigh(float over) => $"Too high: the sling would hang {over:0.0} m over the floor ({KwHammockRules.MostOver:0.0} m at most)";

        public static string InTheWay(string what) => $"{what} is in the way";

        public static string Started(KwHammockClick first) =>
            $"First end: {first.Says}. Now click where the other end goes, {KwHammockRules.MinApart:0.0} to {KwHammockRules.MaxApart:0} m away" + CancelKeys;

        public static string Hung(KwHammockPlan plan) => $"Hammock hung, {plan.Says}";

        /// <summary>The build panel's lines while the hammock is in hand.</summary>
        public static string Live(KwHammockClick? first, KwHammockClick? aim, KwHammockPlan? plan)
        {
            if (first == null)
                return aim == null ? AimFirst : aim.Why ?? $"Click to set the first end here: {aim.Says}";
            string head = $"First end: {first.Says}.\n";
            if (plan == null)
                return head + (aim?.Why ?? AimSecond) + CancelKeys;
            return head + (plan.Why ?? $"{plan.Says}: click to hang it") + CancelKeys;
        }
    }
}
