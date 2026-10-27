using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// Shift + click on a doorway or window with its tool: the edit window of the row it belongs to
    /// (<see cref="KwOpeningRowSet"/>): the tool's rows for the opening clicked (<see cref="KwOpeningForm"/>), a Number row
    /// after the first, built again when an answer asks other rows; the ghost the whole row as it would be, its leaves part
    /// open (the row as it is before a change); OK the whole row in one undo step (<see cref="KwOpeningSession"/>). The
    /// opening edited becomes the tool's current type. Local to this player until OK.
    /// </summary>
    public static class KwOpeningRowEdit
    {
        /// <summary>The row's window for an opening clicked with Shift (rows: the tool's for some answers); false when no row holds it.</summary>
        public static bool Open(Player player, string tool, KwOpeningSite site, KwOpening was, KwOpeningAsk ask, Func<KwOpeningAsk, List<KwField>> rows)
        {
            KwOpeningContext ctx = KwOpeningReads.Context(tool);
            KwOpeningRowSet? set = KwOpeningRowSet.Of(site, was, ctx);
            if (set == null)
                return false;
            (string one, string many) = KwOpeningWords.Kind(tool);
            string title = $"Change a row of {set.Count} {many}";
            Func<KwOpeningAsk, KwFormValues?, List<KwField>> fields = (a, form) => Numbered(rows(a), a, form != null ? KwOpeningForm.Count(form) ?? set.Count : set.Count, set.Most(a));
            KwOpeningWorld world = KwOpeningSession.World(site);
            KwOpeningSession.Start(player, title, fields(ask, null), site, world, Planner(set, ask, ctx), form => Lead(set, many, KwOpeningForm.Read(form, ask)),
                (one, many), form => Keep(tool, KwOpeningForm.Read(form, ask)), KwOpeningForm.Reshaper(title, ask, fields, a => " " + set.Most(a)), KwOpeningSession.Still(set.Shown()));
            return true;
        }

        // A change of the window's answers planned for the whole row, the answers it opened on kept as the first.
        private static Func<KwFormValues, KwOpeningPlan> Planner(KwOpeningRowSet set, KwOpeningAsk ask, KwOpeningContext ctx)
        {
            KwPartValues? first = null;
            return form =>
            {
                KwOpeningAsk now = KwOpeningForm.Read(form, ask);
                KwPartValues values = Values(now);
                return set.Plan(now, KwOpeningForm.Count(form) ?? set.Count, first ??= values, values, ctx);
            };
        }

        /// <summary>An edit's values from its answers: a window's sill over the deck inside; a doorway's from its floor.</summary>
        public static KwPartValues Values(KwOpeningAsk ask) => ask.Values(ask.Kind == KwOpening.Door ? 0f : ask.Above);

        // The tool's rows with the Number row after the first (none when filled in: the row goes).
        private static List<KwField> Numbered(List<KwField> fields, KwOpeningAsk ask, int count, int most)
        {
            if (!ask.Filled)
                fields.Insert(1, KwOpeningForm.NumberRow(count, most));
            return fields;
        }

        // What the note says first: the row, what a change does, a fill-in.
        private static string Lead(KwOpeningRowSet set, string many, KwOpeningAsk ask)
        {
            if (ask.Filled)
                return $"Fill in: the row's {set.Count} {many} filled in, the wall made whole again.";
            string where = set.Span.LoName.Length > 0 ? " " + KwOpeningSpans.Words(set.Span).Replace("Centred ", "") : "";
            return $"A row of {set.Count}{where}: what you set goes to them all, laid out again evenly spread across it.";
        }

        // The opening edited becomes the tool's current type (a fill-in does not).
        private static void Keep(string tool, KwOpeningAsk ask)
        {
            if (!ask.Filled)
                KwOpeningCurrent.Of(tool).Keep(ask);
        }
    }
}
