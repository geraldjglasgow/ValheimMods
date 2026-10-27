using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The row the Doorway and Window tools' ghost shows while Shift is held (<see cref="KwOpeningHover"/>), kept so hovering
    /// stays cheap: the side's free stretches (<see cref="KwOpeningRow.Spans"/>) read again only when the side or the
    /// opening's heights change, the row's plan only when its stretch, count, sill or type change, and then at most every
    /// <see cref="Every"/> seconds (a long run's ghost builds many parts again). A part a ward keeps refuses the row. The
    /// wheel with Shift held sets the count, from the row shown, within one and as many as fit (<see cref="Step"/>). Local
    /// to this player.
    /// </summary>
    public static class KwOpeningRowShown
    {
        private const float Every = 0.15f;
        private static (KwOpeningLine? Line, int Foot, int Top) spansFor;
        private static List<KwFreeSpan> spans = new List<KwFreeSpan>();
        private static (string Tool, KwOpeningType? Type, KwOpeningLine? Line, int Lo, int Hi, int Count, int Sill, int Tall) key;
        private static KwOpeningRowPlan? shown;
        private static float next;

        /// <summary>The row of the placing at the aim across the stretch it is in (in the aimed part's section); null when the section has no free stretch there.</summary>
        public static KwOpeningRowPlan? Of(string tool, KwOpeningPlacing placing, float aimed)
        {
            KwFreeSpan? span = KwOpeningRun.Nearest(Spans(tool, placing), aimed, placing.Line.Aimed);
            if (span == null)
                return null;
            int count = KwOpeningRow.Count(span.Value, placing, KwOpeningCurrent.Of(tool).Row);
            (string, KwOpeningType?, KwOpeningLine?, int, int, int, int, int) now = (tool, placing.Type, placing.Line, Mm(span.Value.Lo), Mm(span.Value.Hi), count, Mm(placing.Sill), Mm(placing.Ask.Tall));
            if (shown != null && (now.Equals(key) || (Time.time < next && key.Tool == tool)))
                return shown;
            (key, next) = (now, Time.time + Every);
            shown = KwOpeningRow.Lay(placing, span.Value, count, KwOpeningReads.Context(tool));
            if (shown.Plan.Any && KwOpeningWork.Warded(shown.Plan))
                shown.Plan = KwOpeningPlace.Refusal("A ward you have no access to keeps this part as it is");
            return shown;
        }

        /// <summary>The wheel's notches with Shift held: the row's count, from the row shown, within one and the most that fit; kept for the tool.</summary>
        public static void Step(string tool, int notches)
        {
            KwOpeningCurrent current = KwOpeningCurrent.Of(tool);
            bool here = shown != null && key.Tool == tool;
            int from = here ? shown!.Count : current.Row ?? 1, most = here ? shown!.Most : KwOpeningFill.Most;
            current.Row = Mathf.Clamp(from + notches, 1, most);
        }

        /// <summary>Everything worked out forgotten (another tool, or the tools have just built).</summary>
        public static void Forget()
        {
            (shown, key, spansFor, next) = (null, default, default, 0f);
            spans = new List<KwFreeSpan>();
        }

        // The side's free stretches at the placing's heights, read again only when the side or the heights change.
        private static List<KwFreeSpan> Spans(string tool, KwOpeningPlacing placing)
        {
            (float foot, float top) = KwOpeningFill.Heights(placing);
            var now = ((KwOpeningLine?)placing.Line, Mm(foot), Mm(top));
            if (!placing.Sited)
                return new List<KwFreeSpan>();
            if (!now.Equals(spansFor))
                (spansFor, spans) = (now, KwOpeningRow.Spans(placing, KwOpeningReads.Context(tool)));
            return spans;
        }

        private static int Mm(float value) => Mathf.RoundToInt(value * 1000f);
    }
}
