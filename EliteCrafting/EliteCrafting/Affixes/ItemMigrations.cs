using System;
using EliteCrafting.Rules;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// The chain of format migrations (item-data.md section 8, classes-and-tiers.md section 7), each a pure step over
    /// the parsed state, run in memory on read. The dictionary is untouched until the item is next written for its own
    /// reasons, which then writes the current form. A new step ships with a test holding a frozen v(n) dictionary and
    /// its expected v(n+1) form.
    /// <para>
    /// 1 → 2: format 1 stored a grade 1-7 over seven tiers; format 2 stores a grade on each inscription's own ladder of k
    /// tiers. <c>grade' = clamp(round(grade * k / 7), 1, k)</c> for every roll whose id is defined; values are kept. It
    /// needs the rules (k), so <see cref="Upgrade"/> only marks the state and <see cref="ResolveGrades"/> converts it
    /// each time it is resolved; an orphaned roll keeps its grade until its id is defined again.
    /// </para>
    /// </summary>
    internal static class ItemMigrations
    {
        /// <summary>The seven tiers every format-1 inscription had.</summary>
        public const int FormatOneTiers = 7;

        public static StateData Upgrade(StateData state)
        {
            string? rarity = RenamedRarity(state.RarityId);
            if (state.Newer || (state.Format >= ItemKeys.CurrentFormat && rarity == state.RarityId))
            {
                return state;
            }
            StateData upgraded = state.Copy();
            upgraded.LegacyGrades = state.Format < 2;
            upgraded.Format = ItemKeys.CurrentFormat;
            upgraded.RarityId = rarity;
            return upgraded;
        }

        /// <summary>The state with format-1 grades converted under these rules; the state itself when there are none.</summary>
        public static StateData ResolveGrades(StateData state, AffixRules rules)
        {
            if (!state.LegacyGrades)
            {
                return state;
            }
            StateData resolved = state.Copy();
            resolved.LegacyGrades = false;
            resolved.Segments = new ItemSegment[state.Segments.Length];
            for (int i = 0; i < state.Segments.Length; i++)
            {
                ItemSegment segment = state.Segments[i];
                AffixDef? def = segment.IsRoll ? rules.Get(segment.Roll.Id) : null;
                resolved.Segments[i] = def == null ? segment : new ItemSegment(Convert(segment.Roll, def.TierCount));
            }
            return resolved;
        }

        /// <summary><c>clamp(round(grade * k / 7), 1, k)</c>, half away from zero.</summary>
        public static int ConvertGrade(int grade, int k) =>
            Math.Max(1, Math.Min(k, (int)Math.Round(grade * (double)k / FormatOneTiers, MidpointRounding.AwayFromZero)));

        private static AffixRoll Convert(AffixRoll roll, int k) =>
            k <= 0 ? roll : new AffixRoll(roll.Id, ConvertGrade(roll.Tier, k), roll.Value);

        /// <summary>
        /// The six built-in rarities before the runes (2026-10-02) in today's three: Uncommon is Magic, Epic,
        /// Legendary and Mythic are Rare, Common is Normal (never stored). Any other id is kept as it is.
        /// </summary>
        private static string? RenamedRarity(string? id) => id switch
        {
            "common" => null,
            "uncommon" => "magic",
            "epic" or "legendary" or "mythic" => "rare",
            _ => id,
        };
    }
}
