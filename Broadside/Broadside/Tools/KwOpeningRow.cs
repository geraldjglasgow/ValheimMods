using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>A row of openings worked out for the ghost and the click: its placing (type, side, sill), the stretch, how many, the most that fit, and its plan.</summary>
    public sealed class KwOpeningRowPlan
    {
        public KwOpeningRowPlan(KwOpeningPlacing placing, KwFreeSpan span, int count, int most)
        {
            (Placing, Span, Count, Most) = (placing, span, count, most);
        }

        public KwOpeningPlacing Placing { get; }
        public KwFreeSpan Span { get; }
        public int Count { get; }
        public int Most { get; }

        /// <summary>Centre to centre: the stretch shared out equally.</summary>
        public float Spacing => Span.Length / Count;

        public KwOpeningPlan Plan { get; set; } = new KwOpeningPlan();

        /// <summary>How many of its places were moved off a rib (planking's, between two parts).</summary>
        public int Moved { get; set; }

        /// <summary>The first two cut side by side (the side's spacing once placed).</summary>
        public List<(KwOpeningSite Site, KwOpening Opening)> Pair { get; set; } = new List<(KwOpeningSite, KwOpening)>();
    }

    /// <summary>
    /// A row of openings evenly spread across a free stretch of a side (the user's words, 2026-10-25: "holding down Shift
    /// equally spaces the number of doors across an area"): the stretch shared out equally, one opening in the middle of each
    /// share, so one alone is centred and a row on planking with as many as its run has even bays stands one in each bay; as
    /// many as fit with the gap between frames kept (<see cref="KwOpeningRules.Gap"/>), never fewer than one. A place on the
    /// rib between two planking parts moves to the nearest place in its own share a part holds it (<see cref="Spread"/>), so
    /// a row along a ribbed hull is not left with gaps where the ribs stand. The stretch is the side's
    /// (<see cref="KwOpeningRun"/>) nearest the aim at the placing's heights in the aimed part's section (the hull's planking
    /// from rib to rib together, or an end part from a rib to a keel end alone, so a row stays on the part of the hull it is
    /// laid on); the count the player's, else as many as the
    /// side's spacing fits (<see cref="KwOpeningSpacing"/>). The openings are cut through <see cref="KwOpeningFill"/>, places
    /// that still do not fit skipped. Pure but for what the context asks.
    /// </summary>
    public static class KwOpeningRow
    {
        /// <summary>An opening's width along with its frames.</summary>
        public static float Footprint(KwOpeningAsk ask) => ask.Wide + 2f * KwOpeningFrames.Width(ask.Framed).Wide;

        /// <summary>How many openings so wide (with frames) fit across a length, the gap kept between them: at least one.</summary>
        public static int Fit(float length, float footprint) =>
            Mathf.Clamp(Mathf.FloorToInt((length + 0.001f) / (footprint + KwOpeningRules.Gap)), 1, KwOpeningFill.Most);

        /// <summary>The middles of a row of count openings evenly spread across a stretch.</summary>
        public static List<float> Places(KwFreeSpan span, int count)
        {
            List<float> places = new List<float>();
            float share = span.Length / Mathf.Max(1, count);
            for (int k = 0; k < count; k++)
                places.Add(span.Lo + (k + 0.5f) * share);
            return places;
        }

        /// <summary>
        /// The row's places: evenly spread, each one a part in line does not hold (a rib there) moved to the nearest place in
        /// its own share that one does, with room kept for its frames and the gap; how many moved.
        /// </summary>
        public static List<float> Spread(KwOpeningPlacing placing, KwFreeSpan span, int count, out int moved)
        {
            List<float> places = Places(span, count);
            KwOpeningAsk ask = placing.Ask;
            (float z0, float z1) = KwOpeningFill.Heights(placing);
            float share = span.Length / Mathf.Max(1, count), half = ask.Wide / 2f, wide = KwOpeningFrames.Width(ask.Framed).Wide, room = half + wide + KwOpeningRules.Gap / 2f;
            moved = 0;
            for (int k = 0; k < places.Count; k++)
            {
                if (placing.Line.Holding(places[k], half, wide, z0, z1) != null)
                    continue;
                float lo = span.Lo + k * share;
                float? held = placing.Line.Held(places[k], half, wide, z0, z1, lo + room, lo + share - room);
                if (held != null)
                    (places[k], moved) = (held.Value, moved + 1);
            }
            return places;
        }

        /// <summary>The side's free stretches at the placing's heights (none for a placing with no sill: a doorway with no deck).</summary>
        public static List<KwFreeSpan> Spans(KwOpeningPlacing placing, KwOpeningContext ctx)
        {
            if (!placing.Sited)
                return new List<KwFreeSpan>();
            (float z0, float z1) = KwOpeningFill.Heights(placing);
            return KwOpeningRun.Of(placing.Line, z0, z1, ctx);
        }

        /// <summary>How many the row has across a stretch: the count wanted, else as many as the spacing fits; within one and the most.</summary>
        public static int Count(KwFreeSpan span, KwOpeningPlacing placing, int? wanted)
        {
            int most = Fit(span.Length, Footprint(placing.Ask));
            int spaced = Mathf.RoundToInt(span.Length / Mathf.Max(placing.Spacing, KwOpeningSpacing.Closest));
            return Mathf.Clamp(wanted ?? spaced, 1, most);
        }

        /// <summary>The row of a placing across a stretch, so many: its plan worked out.</summary>
        public static KwOpeningRowPlan Lay(KwOpeningPlacing placing, KwFreeSpan span, int count, KwOpeningContext ctx)
        {
            KwOpeningRowPlan row = new KwOpeningRowPlan(placing, span, count, Fit(span.Length, Footprint(placing.Ask)));
            List<float> places = Spread(placing, span, count, out int moved);
            row.Plan = KwOpeningFill.Plan(placing, places, ctx, out List<(KwOpeningSite, KwOpening)> pair);
            (row.Pair, row.Moved) = (pair, moved);
            return row;
        }

        /// <summary>The row at the aim at once (a click): the stretch nearest the aim in the aimed part's section, the count wanted; null with no stretch there.</summary>
        public static KwOpeningRowPlan? At(KwOpeningPlacing placing, float aimed, int? wanted, KwOpeningContext ctx)
        {
            KwFreeSpan? span = KwOpeningRun.Nearest(Spans(placing, ctx), aimed, placing.Line.Aimed);
            return span != null ? Lay(placing, span.Value, Count(span.Value, placing, wanted), ctx) : null;
        }
    }
}
