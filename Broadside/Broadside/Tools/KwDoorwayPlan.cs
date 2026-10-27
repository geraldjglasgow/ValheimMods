using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the Doorway tool does (<see cref="KwDoorwayTool"/>): a new doorway at a place on a wall, from the deck inside up
    /// (a cabin's own floor), or doorways changed (size, frame, door) or filled in. Where a cabin stands at the hull its
    /// raised side starts at the sheer, which may stand over the cabin's floor: a doorway in the raised side also cuts the
    /// planking under it, and one in that planking also cuts the raised side over it, the same rectangle on each (a
    /// partner, <see cref="KwPartMap"/>, wherever the partner's wall stands within the doorway's heights), all or nothing;
    /// its door hangs on the part clicked (a partner already hanging it keeps it). Changing or filling a doorway changes or
    /// fills its partner with it, found by place (<see cref="Twin"/>). A doorway with a door is at most
    /// <see cref="KwOpeningShape.DoorMaxHigh"/> high; an open one as high as a cabin's wall lets it be. Pure.
    /// </summary>
    public static class KwDoorwayPlan
    {
        /// <summary>How near in place two doorways on partner parts are to be one doorway.</summary>
        public const float Match = 0.03f;

        /// <summary>
        /// The tallest doorway a wall takes from a floor with a frame: a cabin's, its frame under the roof's underside (with
        /// a door, no more than a door's most); planking's a door's most (it is cut on through the sheer); and no taller
        /// than any partner's wall it reaches.
        /// </summary>
        public static float Room(KwOpeningSite site, float floor, string frame, List<KwPartMap> partners, bool door = true)
        {
            float wide = KwOpeningFrames.Width(frame).Wide, cap = door || site.Spec == null ? KwOpeningShape.DoorMaxHigh : 50f;
            float room = Mathf.Min(cap, site.Head - wide - floor);
            foreach (KwPartMap map in partners)
            {
                float u = map.U(site.U), sill = map.Z(floor);
                (float lo, float hi) = map.Site.Surface.Range(u);
                if (InReach(map, u) && Mathf.Max(lo, sill) < Mathf.Min(hi, sill + Mathf.Min(cap, 6f)) - 0.02f)
                    room = Mathf.Min(room, map.Site.Head - wide - sill);
            }
            return room;
        }

        /// <summary>The doorway the answers cut at a site's place, from its floor up (null with no floor).</summary>
        public static KwOpening? Shape(KwOpeningSite site, KwOpeningAsk ask, int id) => site.Floor == null ? null
            : new KwOpening(id, site.Wall, KwOpening.Door, site.U, site.Floor.Value, ask.Wide, ask.Tall, ask.Framed, ask.Covered);

        /// <summary>A new doorway at the site's place, from the floor up, through its partners too.</summary>
        public static KwOpeningPlan New(KwOpeningSite site, KwOpeningAsk ask, List<KwPartMap> partners, KwOpeningWorld world)
        {
            KwOpeningPlan plan = new KwOpeningPlan();
            KwOpening? o = Shape(site, ask, KwOpening.Next(plan.Part(site).Openings));
            string? why = o == null ? "There is no deck inside under it to stand a doorway on" : Into(plan, site, o, partners, world);
            if (why != null)
                plan.Refuse(why);
            else
                plan.Made = 1;
            Words(plan);
            return plan;
        }

        /// <summary>
        /// A new doorway added to a plan with its partners, checked against the plan's openings as they would be; nothing
        /// added and why when the rules refuse it or a partner.
        /// </summary>
        public static string? Into(KwOpeningPlan plan, KwOpeningSite site, KwOpening o, List<KwPartMap> partners, KwOpeningWorld world)
        {
            KwPartCut cut = plan.Part(site);
            string? why = KwOpeningRules.Refused(site, o, cut.Openings, world.Crosses);
            if (why != null)
                return why;
            List<(KwPartCut Cut, KwOpeningSite Site, KwOpening Twin)> twins = new List<(KwPartCut, KwOpeningSite, KwOpening)>();
            foreach (KwPartMap map in partners)
            {
                KwPartCut other = plan.Part(map.Site);
                KwOpening? twin = Through(map, o, KwOpening.Next(other.Openings), KwOpening.None);
                why = twin != null ? KwOpeningRules.Refused(map.Site, twin, other.Openings, world.Crosses) : null;
                if (why != null)
                    return Partner(map, why);
                if (twin != null)
                    twins.Add((other, map.Site, twin));
            }
            cut.Put(site, o);
            foreach ((KwPartCut other, KwOpeningSite on, KwOpening twin) in twins)
                other.Put(on, twin);
            return null;
        }

        // A partner's refusal, said of the partner.
        private static string Partner(KwPartMap map, string why) =>
            (map.Site.Planking ? "In the hull's planking under it: " : "In the cabin's side over it: ") + char.ToLowerInvariant(why[0]) + why.Substring(1);

        /// <summary>A doorway changed or filled in, and its partners with it.</summary>
        public static KwOpeningPlan Change(KwOpeningSite site, KwOpening was, KwOpeningAsk ask, List<KwPartMap> partners, KwOpeningWorld world)
        {
            KwOpeningPlan plan = new KwOpeningPlan();
            ChangeInto(plan, site, was, ask, partners, world);
            Words(plan);
            return plan;
        }

        // One doorway changed or filled in with its partners; false, refused (nothing of it changed), when it cannot be.
        private static bool ChangeInto(KwOpeningPlan plan, KwOpeningSite site, KwOpening was, KwOpeningAsk ask, List<KwPartMap> partners, KwOpeningWorld world)
        {
            if (ask.Filled)
            {
                plan.Part(site).Fill(was);
                foreach (KwPartMap map in partners)
                {
                    KwOpening? twin = Twin(map, was);
                    if (twin != null)
                        plan.Part(map.Site).Fill(twin);
                }
                plan.Filled++;
                return true;
            }
            bool elsewhere = ask.Covered != KwOpening.None && partners.Exists(m => Twin(m, was)?.HasDoor == true);
            KwOpening o = was.With(KwOpening.Door, was.U, was.Sill, ask.Wide, ask.Tall, ask.Framed, elsewhere ? KwOpening.None : ask.Covered);
            string? why = Changes(plan, site, o, was, partners, elsewhere ? ask.Covered : KwOpening.None, world);
            if (why != null)
                plan.Refuse(why);
            else
                plan.Changed++;
            return why == null;
        }

        // The doorway changed on its wall and on every partner whose wall stands within its heights (a partner it no longer
        // reaches filled in), the door on the partner that hangs it; checked first, so a refusal changes nothing.
        private static string? Changes(KwOpeningPlan plan, KwOpeningSite site, KwOpening o, KwOpening was, List<KwPartMap> partners, string door, KwOpeningWorld world)
        {
            KwPartCut cut = plan.Part(site);
            string? why = KwOpeningRules.Refused(site, o, cut.Openings, world.Crosses);
            if (why != null)
                return why;
            List<Action> work = new List<Action> { () => cut.Put(site, o) };
            foreach (KwPartMap map in partners)
            {
                KwPartCut other = plan.Part(map.Site);
                KwOpening? old = Twin(map, was);
                KwOpening? twin = Through(map, o, old?.Id ?? KwOpening.Next(other.Openings), old != null && old.HasDoor ? door : KwOpening.None);
                why = twin != null ? KwOpeningRules.Refused(map.Site, twin, other.Openings, world.Crosses) : null;
                if (why != null)
                    return Partner(map, why);
                if (twin != null)
                    work.Add(() => other.Put(map.Site, twin));
                else if (old != null)
                    work.Add(() => other.Fill(old));
            }
            work.ForEach(step => step());
            return null;
        }

        // What the note says of a doorway cut: through a cabin's side and the planking under it, or up through the sheer.
        private static void Words(KwOpeningPlan plan)
        {
            bool planking = false, cabin = false, sheer = false;
            foreach (KwPartCut cut in plan.Parts)
            {
                foreach ((KwOpeningSite s, KwOpening o) in cut.Shown)
                {
                    (planking, cabin) = (planking || s.Planking, cabin || !s.Planking);
                    sheer |= s.Planking && o.Top > s.Surface.Range(o.U).Hi + 0.01f;
                }
            }
            if (planking && cabin)
                plan.Notes.Add("It goes through the cabin's side and the hull's planking under it.");
            else if (sheer)
                plan.Notes.Add("It runs up through the sheer; a railing on the sheer stays over it.");
        }

        /// <summary>The doorway as a partner part cuts it (the same rectangle, with the cover given), while the partner's wall stands within its heights there; else null.</summary>
        public static KwOpening? Through(KwPartMap map, KwOpening o, int id, string cover)
        {
            float u = map.U(o.U), sill = map.Z(o.Sill);
            (float lo, float hi) = map.Site.Surface.Range(u);
            if (!InReach(map, u) || Mathf.Max(lo, sill) > Mathf.Min(hi, sill + o.Height) - 0.02f)
                return null;
            return new KwOpening(id, map.Site.Wall, KwOpening.Door, u, sill, o.Width, o.Height, o.Frame, cover);
        }

        /// <summary>A doorway's partner on a partner part: a doorway on its wall at the same place and sill; null for none.</summary>
        public static KwOpening? Twin(KwPartMap map, KwOpening was)
        {
            float u = map.U(was.U), sill = map.Z(was.Sill);
            return map.Site.Openings.Find(o => o.IsDoorway && o.Wall == map.Site.Wall && Mathf.Abs(o.U - u) < Match && Mathf.Abs(o.Sill - sill) < Match);
        }

        private static bool InReach(KwPartMap map, float u)
        {
            (float from, float to) = map.Site.Surface.Reach;
            return u >= from && u <= to;
        }
    }
}
