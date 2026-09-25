using System;
using EliteCrafting.Rules;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// The health-critical condition of the local player (affixes.md "Health-critical"): current health at or below the
    /// threshold share of maximum health. Evaluated on the player's own client only, once per status-effect tick by
    /// the aggregate (one division and one compare); item hooks read the cached <see cref="Active"/> flag. Nothing here
    /// is synced: the only machine that needs it is the one that owns the player.
    /// </summary>
    internal static class HealthCritical
    {
        /// <summary>The local player is at or below the threshold. False when there is no aggregate (effects off, no player).</summary>
        public static bool Active { get; private set; }

        /// <summary>The threshold as a fraction of max health, refreshed on every rebuild.</summary>
        public static float Threshold { get; private set; } = 0.3f;

        /// <summary>Whether any enabled affix is health-critical; when none is, the per-tick check is skipped entirely.</summary>
        public static bool Needed { get; private set; }

        /// <summary>
        /// Rebuild: the rules' threshold, never above the rules' maximum; <see cref="AddThresholdBonus"/> follows once the
        /// totals are summed.
        /// </summary>
        public static void Refresh(AffixRules rules)
        {
            float percent = Math.Min(rules.HealthCriticalThreshold, rules.HealthCriticalMaxThreshold);
            Threshold = Math.Max(0f, percent) / 100f;
            Needed = false;
            foreach (ChannelDef channel in rules.Channels)
            {
                Needed |= channel.Condition == AffixCondition.HealthCritical;
            }
        }

        /// <summary>
        /// Rebuild, after the totals: Valhalla's Edge (<c>hc_threshold</c>, already capped at +20 points and scaled to a
        /// fraction) raises the threshold additively, never above the rules' maximum.
        /// </summary>
        public static void AddThresholdBonus(AffixRules rules, float bonus)
        {
            if (bonus > 0f)
            {
                Threshold = Math.Min(Threshold + bonus, Math.Max(0f, rules.HealthCriticalMaxThreshold) / 100f);
            }
        }

        /// <summary>Per tick on the owning client. True when the state flipped.</summary>
        public static bool Evaluate(Character character)
        {
            bool now = Needed && !character.IsDead() && character.GetHealthPercentage() <= Threshold;
            if (now == Active)
            {
                return false;
            }
            Active = now;
            return true;
        }

        /// <summary>The aggregate is gone (effects off, player gone): nothing is critical any more.</summary>
        public static void Reset() => Active = false;
    }
}
