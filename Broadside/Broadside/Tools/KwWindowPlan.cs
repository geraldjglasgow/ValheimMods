using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What the Window tool does (<see cref="KwWindowTool"/>): a new window at a place on a wall with its sill at a height
    /// (the ghost's, <see cref="KwOpeningPlace"/>); or windows changed or filled in (the one clicked), each planned from its
    /// own values with only the keys the player changed from the window's first values taken (<see cref="KwPartValues.Changed"/>,
    /// <see cref="KwPartValues.With"/>), as every edit does. A window whose deck inside is not known keeps its sill when
    /// another's height over its deck is given to it. Each is refused, with its
    /// reason, where the rules say (<see cref="KwOpeningRules"/>), against the openings of its part as they would be. Pure.
    /// </summary>
    public static class KwWindowPlan
    {
        /// <summary>The window the answers cut at a site's place with its sill at a height.</summary>
        public static KwOpening Shape(KwOpeningSite site, KwOpeningAsk ask, float sill, int id) =>
            new KwOpening(id, site.Wall, ask.Kind, site.U, sill, ask.Wide, ask.High, ask.Framed, ask.Covered);

        /// <summary>A new window at the site's place, its sill at a height.</summary>
        public static KwOpeningPlan New(KwOpeningSite site, KwOpeningAsk ask, float sill, KwOpeningWorld world)
        {
            KwOpeningPlan plan = new KwOpeningPlan();
            string? why = Into(plan, site, Shape(site, ask, sill, KwOpening.Next(plan.Part(site).Openings)), world);
            if (why != null)
                plan.Refuse(why);
            else
                plan.Made = 1;
            return plan;
        }

        /// <summary>A new window added to a plan, checked against the plan's openings of its part as they would be; nothing added and why when refused.</summary>
        public static string? Into(KwOpeningPlan plan, KwOpeningSite site, KwOpening o, KwOpeningWorld world)
        {
            KwPartCut cut = plan.Part(site);
            string? why = KwOpeningRules.Refused(site, o, cut.Openings, world.Crosses);
            if (why == null)
                cut.Add(site, o);
            return why;
        }

        /// <summary>Windows changed or filled in, the one clicked first: each its own values with the keys changed from first to now.</summary>
        public static KwOpeningPlan Change(List<KwOpeningTarget> targets, KwPartValues first, KwPartValues now, KwOpeningWorld world)
        {
            KwOpeningPlan plan = new KwOpeningPlan();
            List<string> changed = KwPartValues.Changed(first, now);
            if (changed.Count == 0 || targets.Count == 0)
                return plan;
            bool floored = targets[0].Floor != null, sill = changed.Contains(KwOpeningAsk.Sill);
            foreach (KwOpeningTarget target in targets)
                Change(plan, target, target.Values.With(now, changed), sill && floored && target.Floor == null, world);
            return plan;
        }

        // One window made again from its merged values, or filled in; refused against its part's openings as they would be.
        private static void Change(KwOpeningPlan plan, KwOpeningTarget target, KwPartValues merged, bool keepSill, KwOpeningWorld world)
        {
            KwPartCut cut = plan.Part(target.Site);
            if (KwOpeningAsk.Fills(merged))
            {
                cut.Fill(target.Opening);
                plan.Filled++;
                return;
            }
            KwOpening o = KwOpeningAsk.Made(target.Opening, merged, target.Floor, keepSill);
            string? why = KwOpeningRules.Refused(target.Site, o, cut.Openings, world.Crosses);
            if (why != null)
                plan.Refuse(why);
            else
            {
                cut.Put(target.Site, o);
                plan.Changed++;
            }
        }
    }
}
