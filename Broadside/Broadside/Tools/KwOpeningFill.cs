using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Openings of one placing's type cut at several places along its side at once (<see cref="KwOpeningRow"/>: a row laid
    /// with Shift held, or a row laid out again from its edit window), all in the aimed part's measures on whichever part in
    /// line holds each place (<see cref="KwOpeningLine"/>): a window at the placing's sill, a doorway from the deck inside
    /// at each place, its height the placing's or what the wall has room for there, through its partners. A place on a rib
    /// or a part's end, over a corner post, overlapping another opening, cutting a deck or with no room is skipped, with its
    /// reason (it shows as a gap in the ghost). At most <see cref="Most"/>. One plan, so one undo step. Pure but for what
    /// the context asks.
    /// </summary>
    public static class KwOpeningFill
    {
        public const int Most = 60;

        /// <summary>A new plan cutting the placing's type at the places; the first two cut side by side kept as the side's spacing.</summary>
        public static KwOpeningPlan Plan(KwOpeningPlacing placing, IEnumerable<float> places, KwOpeningContext ctx, out List<(KwOpeningSite Site, KwOpening Opening)> pair)
        {
            KwOpeningPlan plan = new KwOpeningPlan();
            pair = Into(plan, placing, places, ctx);
            return plan;
        }

        /// <summary>The placing's type cut at the places into a plan (its fill-ins already in it); the first two cut side by side.</summary>
        public static List<(KwOpeningSite Site, KwOpening Opening)> Into(KwOpeningPlan plan, KwOpeningPlacing placing, IEnumerable<float> places, KwOpeningContext ctx)
        {
            List<(KwOpeningSite, KwOpening)> pair = new List<(KwOpeningSite, KwOpening)>();
            (KwOpeningSite, KwOpening)? before = null;
            int count = 0;
            foreach (float u in places)
            {
                if (count++ >= Most)
                    break;
                (KwOpeningSite, KwOpening)? made = Spot(plan, placing, u, ctx);
                if (made != null && before != null && pair.Count == 0)
                    pair.AddRange(new[] { before.Value, made.Value });
                before = made;
            }
            return pair;
        }

        /// <summary>The heights an opening of the placing takes with its frame: a doorway's from its floor, a window's round its sill.</summary>
        public static (float Foot, float Top) Heights(KwOpeningPlacing placing)
        {
            KwOpeningAsk ask = placing.Ask;
            float wide = KwOpeningFrames.Width(ask.Framed).Wide;
            return placing.Type.Doorway ? (placing.Sill, placing.Sill + ask.Tall + wide) : (placing.Sill - wide, placing.Sill + ask.High + wide);
        }

        // One place: the opening cut there, or skipped and why.
        private static (KwOpeningSite, KwOpening)? Spot(KwOpeningPlan plan, KwOpeningPlacing placing, float u, KwOpeningContext ctx)
        {
            KwOpeningAsk ask = placing.Ask;
            (float z0, float z1) = Heights(placing);
            KwPartMap? map = placing.Line.Holding(u, ask.Wide / 2f, KwOpeningFrames.Width(ask.Framed).Wide, z0, z1);
            if (map == null)
            {
                plan.Skip("a rib, an end or a corner post there");
                return null;
            }
            (KwOpeningSite site, KwOpening? o, string? why) = placing.Type.Doorway ? Door(plan, placing, map, u, ctx) : Window(plan, placing, map, u, ctx);
            if (why != null)
                plan.Skip(char.ToLowerInvariant(why[0]) + why.Substring(1));
            else
                plan.Made++;
            return why == null ? (site, o!) : ((KwOpeningSite, KwOpening)?)null;
        }

        // A window at the placing's sill on the part holding the place.
        private static (KwOpeningSite, KwOpening?, string?) Window(KwOpeningPlan plan, KwOpeningPlacing placing, KwPartMap map, float u, KwOpeningContext ctx)
        {
            KwOpeningAsk ask = placing.Ask;
            KwOpeningSite site = map.Site.At(map.U(u), map.Z(placing.Sill + ask.High / 2f));
            KwOpening o = KwWindowPlan.Shape(site, ask, map.Z(placing.Sill), Id(plan, site));
            return (site, o, KwWindowPlan.Into(plan, site, o, ctx.World));
        }

        // A doorway from the deck inside at the place, as high as the placing's or as the wall's room there, through its partners.
        private static (KwOpeningSite, KwOpening?, string?) Door(KwOpeningPlan plan, KwOpeningPlacing placing, KwPartMap map, float u, KwOpeningContext ctx)
        {
            KwOpeningSite site = map.Site.At(map.U(u), map.Z(placing.Sill + 1f));
            site.Floor = ctx.Floor(site, site.U, site.Z);
            List<KwPartMap> partners = ctx.Partners(site);
            KwOpeningAsk ask = placing.Ask.Copy();
            string? why = KwOpeningPlace.Fit(site, ask, partners);
            KwOpening? o = why == null ? KwDoorwayPlan.Shape(site, ask, Id(plan, site)) : null;
            return (site, o, why ?? KwDoorwayPlan.Into(plan, site, o!, partners, ctx.World));
        }

        // A new opening's number on its part: past every one it has and had (a row laid out again fills its own in first,
        // and a number used again would take on the old one's open or shut leaf).
        private static int Id(KwOpeningPlan plan, KwOpeningSite site) => Mathf.Max(KwOpening.Next(plan.Part(site).Openings), KwOpening.Next(site.Openings));
    }
}
