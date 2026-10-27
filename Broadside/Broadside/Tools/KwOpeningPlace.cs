using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What placing an opening asks of the world (<see cref="KwOpeningPlace"/>): the tool, the rays a plan asks
    /// (<see cref="KwOpeningWorld"/>), the side a site is on (<see cref="KwOpeningLine"/>), the deck inside under a place,
    /// a doorway's partners, the ladders standing against a wall between two heights (as stretches along it) and where an
    /// opening's middle is in the world. The game's are <see cref="KwOpeningReads"/>'; an offline check gives its own.
    /// </summary>
    public sealed class KwOpeningContext
    {
        public string Tool { get; set; } = KwWindowTool.Tool;
        public KwOpeningWorld World { get; set; } = new KwOpeningWorld();
        public Func<KwOpeningSite, KwOpeningLine> Line { get; set; } = site => new KwOpeningLine(new List<KwPartMap> { KwPartMap.Same(site) }, false);
        public Func<KwOpeningSite, float, float, float?> Floor { get; set; } = (site, u, z) => site.Spec?.Floor;
        public Func<KwOpeningSite, List<KwPartMap>> Partners { get; set; } = _ => new List<KwPartMap>();
        public Func<KwOpeningSite, float, float, List<(float Lo, float Hi)>> Ladders { get; set; } = (_, _, _) => new List<(float, float)>();
        public Func<KwOpeningSite, KwOpening, Vector3> Middle { get; set; } = (site, o) => site.Surface.Point(o.U, o.Sill + o.Height / 2f, 0f);

        public Vector3 MiddleOf(KwLineOpening o) => Middle(o.Map.Site, o.Opening);
    }

    /// <summary>
    /// Where the ghost of a Doorway or Window tool stands and what a click there would cut (<see cref="KwOpeningPlace"/>): the
    /// type, its answers as cut (a door as high as the wall lets it be), the side, the place along and sill in the aimed
    /// part's measures, the spacing, what the snap is in words, and the plan (one opening, or refused and why).
    /// </summary>
    public sealed class KwOpeningPlacing
    {
        public KwOpeningPlacing(KwOpeningType type, KwOpeningAsk ask, KwOpeningLine line)
        {
            (Type, Ask, Line) = (type, ask, line);
        }

        public KwOpeningType Type { get; }
        public KwOpeningAsk Ask { get; }
        public KwOpeningLine Line { get; }
        public float U { get; set; }
        public float Sill { get; set; }
        public float Spacing { get; set; }

        /// <summary>Whether its sill is known (a doorway's deck inside found), so a row can be laid from it.</summary>
        public bool Sited { get; set; }
        public string Words { get; set; } = "";
        public KwOpeningPlan Plan { get; set; } = new KwOpeningPlan();

        /// <summary>Whether a click cuts it (else the plan says why not).</summary>
        public bool Fits => Plan.Made > 0;

        /// <summary>The opening it cuts and its wall's site; none when refused.</summary>
        public (KwOpeningSite Site, KwOpening Opening)? Cut
        {
            get
            {
                KwPartCut? cut = Plan.Parts.Find(p => p.Shown.Count > 0);
                return cut != null ? cut.Shown[0] : ((KwOpeningSite, KwOpening)?)null;
            }
        }
    }

    /// <summary>
    /// The Doorway and Window tools' one click (the user's request, 2026-10-25: the ghost "should actually show the object
    /// that you're trying to place, exactly how it would look if you did place it"): the current type at the place aimed,
    /// snapped (<see cref="KwOpeningSnap"/>): a window with its middle at the aim, its sill snapped; a doorway from the deck
    /// inside up, its height the type's or what the wall has room for; along, whole spacings from the openings of the
    /// tool's kind or a free stretch's middle (moved off a rib there), both in the aimed part's section of the side (the
    /// hull's planking from rib to rib together, or an end part alone, <see cref="KwOpeningLine.Section"/>); on whichever
    /// part in line holds the place (a rib there refuses it). The
    /// spacing is never closer than the opening with its frames takes. A row of them (Shift) is laid from it
    /// (<see cref="KwOpeningRow"/>). Pure but for what the context asks.
    /// </summary>
    public static class KwOpeningPlace
    {
        /// <summary>The placing at a site aimed at, snapped.</summary>
        public static KwOpeningPlacing At(KwOpeningSite aimed, KwOpeningType type, KwOpeningContext ctx) =>
            type.Doorway ? Door(aimed, type, ctx) : Window(aimed, type, ctx);

        private static KwOpeningPlacing Window(KwOpeningSite aimed, KwOpeningType type, KwOpeningContext ctx)
        {
            KwOpeningPlacing placing = new KwOpeningPlacing(type, type.Ask.Copy(), ctx.Line(aimed));
            KwOpeningAsk ask = placing.Ask;
            float wide = KwOpeningFrames.Width(ask.Framed).Wide;
            float? floor = ctx.Floor(aimed, KwOpeningSnap.Round(aimed.U, 0.1f), aimed.Z);
            (float sill, string level) = KwOpeningSnap.Sill(aimed.Z - ask.High / 2f, placing.Line, floor, ask.Above);
            (placing.Sill, placing.Sited) = (sill, true);
            Along(placing, aimed, ctx, sill - wide, sill + ask.High + wide);
            placing.Words = Join(placing.Words, level);
            KwPartMap map = placing.Line.Holding(placing.U, ask.Wide / 2f, wide, sill - wide, sill + ask.High + wide) ?? placing.Line.Reaching(placing.U);
            KwOpeningSite site = map.Site.At(map.U(placing.U), map.Z(sill + ask.High / 2f));
            placing.Plan = KwWindowPlan.New(site, ask, map.Z(sill), ctx.World);
            return placing;
        }

        private static KwOpeningPlacing Door(KwOpeningSite aimed, KwOpeningType type, KwOpeningContext ctx)
        {
            KwOpeningPlacing placing = new KwOpeningPlacing(type, type.Ask.Copy(), ctx.Line(aimed));
            KwOpeningAsk ask = placing.Ask;
            float? floor = ctx.Floor(aimed, aimed.U, aimed.Z);
            if (floor == null)
                return Refused(placing, "There is no deck inside under this spot to stand a doorway on");
            float wide = KwOpeningFrames.Width(ask.Framed).Wide;
            (placing.Sill, placing.U, placing.Sited) = (floor.Value, aimed.U, true);
            Along(placing, aimed, ctx, floor.Value, floor.Value + ask.Tall + wide);
            KwPartMap map = placing.Line.Holding(placing.U, ask.Wide / 2f, wide, floor.Value, floor.Value + ask.Tall + wide) ?? placing.Line.Reaching(placing.U);
            KwOpeningSite site = map.Site.At(map.U(placing.U), map.Z(floor.Value + 1f));
            site.Floor = ctx.Floor(site, site.U, site.Z);
            List<KwPartMap> partners = ctx.Partners(site);
            string? low = Fit(site, ask, partners);
            placing.Plan = low != null ? Refusal(low) : KwDoorwayPlan.New(site, ask, partners, ctx.World);
            return placing;
        }

        /// <summary>A doorway's height made to fit the wall's room at a site (lowered to a 5 cm step); why not, when too low.</summary>
        public static string? Fit(KwOpeningSite site, KwOpeningAsk ask, List<KwPartMap> partners)
        {
            if (site.Floor == null)
                return "There is no deck inside under this spot to stand a doorway on";
            bool door = ask.Covered != KwOpening.None;
            float room = KwDoorwayPlan.Room(site, site.Floor.Value, ask.Framed, partners, door);
            float least = door ? KwOpeningShape.DoorMinHigh : KwOpeningTypes.OpenLeastHigh;
            if (room < least - 0.005f)
                return $"Too low for a doorway: {Mathf.Max(0f, room):0.00} m under its frame here, and one {(door ? "with a door " : "")}is at least {least:0.0} m high";
            ask.Tall = Mathf.Min(ask.Tall, Mathf.Floor(room / KwOpeningSnap.Step + 0.001f) * KwOpeningSnap.Step);
            return null;
        }

        // Along the side, in the aimed part's section (the hull's planking from rib to rib together, or an end part alone):
        // its stretches free at the opening's heights, the spacing and the openings it counts from, the snap; a place on a
        // rib moved off it.
        private static void Along(KwOpeningPlacing placing, KwOpeningSite aimed, KwOpeningContext ctx, float z0, float z1)
        {
            KwOpeningAsk ask = placing.Ask;
            float wide = KwOpeningFrames.Width(ask.Framed).Wide, room = ask.Wide + 2f * wide + KwOpeningRules.Gap;
            object section = placing.Line.Aimed;
            KwOpeningLine own = placing.Line.Within(section);
            List<KwFreeSpan> spans = KwOpeningRun.Of(placing.Line, z0, z1, ctx).FindAll(s => Equals(s.Section, section));
            float spacing = KwOpeningSpacing.Of(ctx.Tool, own, ctx.MiddleOf, own.Nearest(aimed.U), Mathf.Max(placing.Type.Spacing, room));
            placing.Spacing = Mathf.Max(spacing, room);
            (float u, string words) = KwOpeningSnap.Along(aimed.U, own, placing.Spacing, spans, ask.Wide + 2f * wide);
            KwFreeSpan? span = KwOpeningRun.Nearest(spans, u);
            (placing.U, placing.Words) = span != null ? OffRib(placing, span.Value, u, words, z0, z1) : (u, words);
        }

        // A place on the hull no planking part in line holds (a rib there, or a keel end) moved to the nearest place in its
        // stretch one does, and said; a cabin's wall as it is.
        private static (float U, string Words) OffRib(KwOpeningPlacing placing, KwFreeSpan span, float u, string words, float z0, float z1)
        {
            float half = placing.Ask.Wide / 2f, wide = KwOpeningFrames.Width(placing.Ask.Framed).Wide;
            if (!placing.Line.Maps[0].Site.Planking || placing.Line.Holding(u, half, wide, z0, z1) != null)
                return (u, words);
            float? held = placing.Line.Held(u, half, wide, z0, z1, span.Lo + half + wide, span.Hi - half - wide);
            if (held == null)
                return (u, words);
            string end = Mathf.Abs(u - span.Lo) <= Mathf.Abs(span.Hi - u) ? span.LoName : span.HiName;
            string moved = end == KwOpeningSpans.KeelEnd ? "moved off the keel end" : "moved off a rib";
            return (held.Value, words.Length == 0 ? "M" + moved.Substring(1) : Join(words, moved));
        }

        private static KwOpeningPlacing Refused(KwOpeningPlacing placing, string why)
        {
            placing.Plan = Refusal(why);
            return placing;
        }

        /// <summary>A plan that cuts nothing, and why.</summary>
        public static KwOpeningPlan Refusal(string why)
        {
            KwOpeningPlan plan = new KwOpeningPlan();
            plan.Refuse(why);
            return plan;
        }

        public static string Join(string a, string b) => a.Length == 0 ? b : b.Length == 0 ? a : a + "; " + b;
    }
}
