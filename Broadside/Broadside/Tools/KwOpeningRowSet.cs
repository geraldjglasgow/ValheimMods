using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The row an opening clicked with Shift belongs to (the user's words, 2026-10-25: "if you Shift-click on a door using
    /// the door tool, there is a number of doors in the menu, so you can change the number of doors while they stay evenly
    /// spaced, with all of the doors changed at the same time"): the openings of the tool's kind along the same side in line
    /// (<see cref="KwOpeningLine"/>) and in the same section of it (the hull's planking from rib to rib, or an end part
    /// alone), whose sills are within <see cref="Level"/> of the one clicked, standing in the free stretch round it
    /// (<see cref="KwOpeningRun"/>, worked out with those openings left out), all in the clicked part's measures. Its edit:
    /// filled in, every one filled in (a doorway's partners with it); with anything changed, every one
    /// filled in and as many as the number asks cut again of the answers' type, evenly spread across the stretch and moved
    /// off the ribs (<see cref="KwOpeningRow.Spread"/>, through <see cref="KwOpeningFill"/>: a doorway as high as the wall
    /// has room for with the frame chosen), so the row stays evenly spaced whatever changes; all in one plan, so one undo
    /// step. Nothing changed plans nothing. Pure but for what the context asks.
    /// </summary>
    public sealed class KwOpeningRowSet
    {
        /// <summary>How near a sill stands to the clicked one's to be in its row.</summary>
        public const float Level = 0.05f;

        private KwOpeningRowSet(KwOpeningLine line, KwLineOpening clicked, List<KwLineOpening> members, KwFreeSpan span)
        {
            (Line, Clicked, Members, Span) = (line, clicked, members, span);
        }

        public KwOpeningLine Line { get; }
        public KwLineOpening Clicked { get; }

        /// <summary>The row's openings along the side, the one clicked among them.</summary>
        public List<KwLineOpening> Members { get; }
        public KwFreeSpan Span { get; }
        public int Count => Members.Count;

        /// <summary>The row of an opening clicked on a site's wall; null when the side does not hold it.</summary>
        public static KwOpeningRowSet? Of(KwOpeningSite site, KwOpening clicked, KwOpeningContext ctx)
        {
            KwOpeningLine line = ctx.Line(site);
            KwLineOpening? c = line.Openings.Find(o => o.Opening.Id == clicked.Id && o.Opening.Wall == clicked.Wall && Same(o.Map.Site, site));
            if (c == null)
                return null;
            object section = KwOpeningLine.Section(c.Map);
            List<KwLineOpening> level = line.Openings.FindAll(o => Mathf.Abs(o.Sill - c.Sill) <= Level && Equals(KwOpeningLine.Section(o.Map), section));
            (float foot, float top) = Heights(c.Opening, c.Sill);
            List<KwFreeSpan> spans = KwOpeningRun.Of(line, foot, top, ctx, (map, o) => level.Exists(l => l.Map == map && l.Opening.Id == o.Id));
            KwFreeSpan span = KwOpeningRun.Nearest(spans, c.U, section) ?? Round(c);
            List<KwLineOpening> members = level.FindAll(o => o == c || (o.U >= span.Lo - 0.01f && o.U <= span.Hi + 0.01f));
            return new KwOpeningRowSet(line, c, members, span);
        }

        /// <summary>The most the row may have across its stretch, of the answers' width and frame: no fewer than it has while they are no wider than its own.</summary>
        public int Most(KwOpeningAsk ask)
        {
            float own = KwOpeningRow.Footprint(KwOpeningAsk.Of(Clicked.Opening, null)), now = KwOpeningRow.Footprint(ask);
            int fit = KwOpeningRow.Fit(Span.Length, now);
            return now <= own + 0.001f ? Mathf.Max(Count, fit) : fit;
        }

        /// <summary>The row's openings as they are, each on its part's wall (what the edit's ghost shows before a change).</summary>
        public List<(KwOpeningSite Site, KwOpening Opening)> Shown() =>
            Members.ConvertAll(m => (m.Map.Site.At(m.Opening.U, m.Opening.Sill + m.Opening.Height / 2f), m.Opening));

        /// <summary>The row's edit: filled in; laid out again with the answers and the number; nothing when nothing changed from first.</summary>
        public KwOpeningPlan Plan(KwOpeningAsk ask, int count, KwPartValues first, KwPartValues now, KwOpeningContext ctx)
        {
            KwOpeningPlan plan = new KwOpeningPlan();
            if (!ask.Filled && count == Count && KwPartValues.Changed(first, now).Count == 0)
                return plan;
            foreach (KwLineOpening m in Members)
                FillIn(plan, m, ctx);
            if (ask.Filled)
                return plan;
            KwOpeningPlacing placing = Placing(ask, ctx);
            count = Mathf.Clamp(count, 1, Most(ask));
            (float foot, float top) = KwOpeningFill.Heights(placing);
            KwFreeSpan span = KwOpeningRun.Nearest(KwOpeningRun.Of(Line, foot, top, ctx, Skip), Clicked.U, KwOpeningLine.Section(Clicked.Map)) ?? Span;
            KwOpeningFill.Into(plan, placing, KwOpeningRow.Spread(placing, span, count, out _), ctx);
            return plan;
        }

        // One of the row filled in, a doorway's partners with it.
        private static void FillIn(KwOpeningPlan plan, KwLineOpening m, KwOpeningContext ctx)
        {
            KwOpeningSite site = m.Map.Site;
            plan.Part(site).Fill(m.Opening);
            plan.Filled++;
            if (!m.Opening.IsDoorway)
                return;
            foreach (KwPartMap partner in ctx.Partners(site.At(m.Opening.U, m.Opening.Sill + 1f)))
            {
                KwOpening? twin = KwDoorwayPlan.Twin(partner, m.Opening);
                if (twin != null)
                    plan.Part(partner.Site).Fill(twin);
            }
        }

        // The row laid out again as a placing of the answers: a doorway from the clicked one's floor, a window its sill over the deck there.
        private KwOpeningPlacing Placing(KwOpeningAsk ask, KwOpeningContext ctx)
        {
            KwOpening o = Clicked.Opening;
            float sill = Clicked.Sill;
            if (!o.IsDoorway)
            {
                float? floor = ctx.Floor(Clicked.Map.Site, o.U, o.Sill);
                sill = (floor != null ? floor.Value - Clicked.Map.Dz : 0f) + ask.Above;
            }
            return new KwOpeningPlacing(KwOpeningTypes.Own(ask), ask.Copy(), Line) { U = Clicked.U, Sill = sill, Sited = true };
        }

        // The row's own openings left out of the stretches.
        private bool Skip(KwPartMap map, KwOpening o) => Members.Exists(m => m.Map == map && m.Opening.Id == o.Id);

        // The heights an opening takes with its frame, its sill given in the clicked part's measures.
        private static (float Foot, float Top) Heights(KwOpening o, float sill)
        {
            float wide = KwOpeningFrames.Width(o.Frame).Wide;
            return (o.IsDoorway ? sill : sill - wide, sill + o.Height + wide);
        }

        // An opening's own room along, when the side has no free stretch round it.
        private static KwFreeSpan Round(KwLineOpening c)
        {
            float half = c.Opening.Width / 2f + KwOpeningFrames.Width(c.Opening.Frame).Wide;
            return new KwFreeSpan(c.U - half, c.U + half, "", "");
        }

        private static bool Same(KwOpeningSite a, KwOpeningSite b) => ReferenceEquals(a.Of ?? a, b.Of ?? b);
    }
}
