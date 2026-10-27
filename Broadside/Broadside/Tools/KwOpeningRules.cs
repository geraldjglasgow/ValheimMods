using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Why an opening cannot be cut where it is planned (<see cref="KwOpeningSite"/>), each with the words the tool's window
    /// says: a cover its shape does not take; sizes past the tool's limits (a doorway with a door within the door's sizes,
    /// an open doorway any size its wall has room for); crossing the end of a planking part, where its rib stands (its frame kept
    /// <see cref="KwOpeningSite.EndClear"/> clear); off a cabin wall's ends or over its corner posts; higher than a cabin
    /// wall (its frame under the roof's underside), a window on planking cutting the sheer or reaching the keel, a window in
    /// a cabin's raised side reaching down into the planking under it or one in a cabin's wall into the deck (a doorway
    /// runs on through the sheer, and through a cabin's side over it, <see cref="KwDoorwayPlan"/>); overlapping another opening on the same wall (their outlines grown by their
    /// frames and a gap, <see cref="Gap"/>); and on planking, cutting through a deck inside the hull (asked of the world, a
    /// ray test). Pure but for that test.
    /// </summary>
    public static class KwOpeningRules
    {
        /// <summary>The least gap kept between two openings' frames.</summary>
        public const float Gap = 0.03f;

        /// <summary>Why the opening cannot be cut on its site's wall beside the others there; null when it can.</summary>
        public static string? Refused(KwOpeningSite site, KwOpening o, IEnumerable<KwOpening> others, Func<KwOpeningSite, KwOpening, bool>? crosses = null) =>
            Kind(o) ?? Ends(site, o) ?? Heights(site, o) ?? Overlaps(o, others) ?? Deck(site, o, crosses);

        // A cover the shape takes, and sizes within the tool's limits.
        private static string? Kind(KwOpening o)
        {
            if (!KwOpeningShape.Covers(o.Shape, o.Cover))
                return $"{Named(o.Shape)} takes {Takes(o.Shape)}";
            if (o.IsDoorway)
                return o.HasDoor ? Door(o) : Open(o);
            return Mathf.Min(o.Width, o.Height) < KwOpeningShape.Least - 0.005f || Mathf.Max(o.Width, o.Height) > KwOpeningShape.Most + 0.005f
                ? $"A window is {KwOpeningShape.Least:0.00} to {KwOpeningShape.Most:0.0} m across" : null;
        }

        // A doorway with a door: within the door's sizes (the tools make a single door 1.3 m wide, a double 2.2 m).
        private static string? Door(KwOpening o) =>
            o.Width < KwOpeningShape.DoorMinWide - 0.005f || o.Width > KwOpeningShape.DoorMaxWide + 0.005f
            || o.Height < KwOpeningShape.DoorMinHigh - 0.005f || o.Height > KwOpeningShape.DoorMaxHigh + 0.005f
                ? $"A doorway with a door is {KwOpeningShape.DoorMinWide:0.0} to {KwOpeningShape.DoorMaxWide:0.0} m wide and {KwOpeningShape.DoorMinHigh:0.0} to {KwOpeningShape.DoorMaxHigh:0.0} m high" : null;

        // An open doorway (no door): any size its wall has room for, from its least up.
        private static string? Open(KwOpening o) =>
            o.Width < KwOpeningTypes.OpenLeastWide - 0.005f || o.Height < KwOpeningTypes.OpenLeastHigh - 0.005f
                ? $"An open doorway is at least {KwOpeningTypes.OpenLeastWide:0.0} m wide and {KwOpeningTypes.OpenLeastHigh:0.0} m high" : null;

        /// <summary>A shape as the player reads it: "A round porthole".</summary>
        public static string Named(string shape) => shape switch
        {
            KwOpening.Door => "A doorway",
            KwOpening.Round => "A round porthole",
            KwOpening.Square => "A square window",
            KwOpening.Arch => "An arched window",
            _ => "A rectangular window",
        };

        // The covers a shape takes, in words.
        private static string Takes(string shape) => shape switch
        {
            KwOpening.Door => "no door, a door or a double door",
            KwOpening.Round => "no cover or its round lid",
            _ => "no cover, a lid hinged at its top or bottom, or side shutters",
        };

        // Within the wall's reach along: planking's ends (its ribs) and a cabin wall's ends and corner posts kept clear.
        private static string? Ends(KwOpeningSite site, KwOpening o)
        {
            float wide = KwOpeningFrames.Width(o.Frame).Wide, z0 = o.IsDoorway ? o.Sill : o.Sill - wide, z1 = o.Top + wide;
            (float lo, float hi) = site.Allowed(o.Width / 2f, wide, z0, z1);
            if (o.U >= lo - 0.001f && o.U <= hi + 0.001f)
                return null;
            if (lo > hi)
                return site.Planking ? "This planking is too short for it with its frame between its ribs" : "This wall is too narrow for it with its frame and the corner posts";
            if (site.Planking)
                return $"It would cross the end of the planking, where a rib stands: keep its frame {KwOpeningSite.EndClear:0.0} m clear of it";
            (float from, float to) = site.Allowed(o.Width / 2f, wide, z0, z1, false);
            return o.U < from - 0.001f || o.U > to + 0.001f ? "It would run off the end of the wall"
                : $"It would stand over a corner post: keep its frame {KwCabinPosts.Size:0.00} m from the wall's end";
        }

        // Within the wall's heights: under a cabin's roof, a planking window under the sheer and over the keel, and so on.
        private static string? Heights(KwOpeningSite site, KwOpening o)
        {
            float wide = KwOpeningFrames.Width(o.Frame).Wide;
            string? why = site.Planking ? PlankHeights(site, o, wide) : CabinHeights(site, o, wide);
            return why ?? (KwOpeningSurfaces.Fits(site.Surface, o) ? null : "It does not fit on this wall there");
        }

        private static string? CabinHeights(KwOpeningSite site, KwOpening o, float wide)
        {
            if (o.Top + wide > site.Head + 0.001f)
                return o.IsDoorway ? $"Too high for this wall: a doorway here is at most {Mathf.Max(0f, site.Head - wide - o.Sill):0.00} m high"
                    : $"Too high for this wall: lower it by {o.Top + wide - site.Head:0.00} m";
            if (o.IsDoorway)
                return null;
            if (site.Raised && o.Sill - wide < Foot(site, o, wide) - 0.001f)
                return $"It would reach down into the hull's planking under the cabin's side: raise it by {Foot(site, o, wide) - o.Sill + wide:0.00} m";
            return site.OwnWall && site.Floor != null && o.Sill - wide < site.Floor.Value - 0.001f
                ? $"It would reach down into the deck: raise it by {site.Floor.Value - o.Sill + wide:0.00} m" : null;
        }

        private static string? PlankHeights(KwOpeningSite site, KwOpening o, float wide)
        {
            if (o.IsDoorway)
                return null;
            (float lo, float hi) = Span(site.Surface, o, wide);
            if (o.Top + wide > hi - 0.01f)
                return $"Too high: its frame would cut the sheer; lower it by {o.Top + wide - hi + 0.01f:0.00} m";
            return o.Sill - wide < lo + 0.05f ? "Too low: it would reach the keel" : null;
        }

        // A raised side's foot (the sheer under it, or the deck) at its highest over the opening's reach with its frame.
        private static float Foot(KwOpeningSite site, KwOpening o, float wide) => Span(site.Surface, o, wide).Lo;

        // The wall's foot at its highest and top at its lowest over the opening's reach along, its frame counted.
        private static (float Lo, float Hi) Span(KwOpeningSurface s, KwOpening o, float wide)
        {
            float lo = float.MinValue, hi = float.MaxValue;
            foreach (float u in new[] { o.U0 - wide, o.U, o.U1 + wide })
            {
                (float a, float c) = s.Range(u);
                (lo, hi) = (Mathf.Max(lo, a), Mathf.Min(hi, c));
            }
            return (lo, hi);
        }

        // Clear of every other opening on the same wall, both grown by their frames and half the gap.
        private static string? Overlaps(KwOpening o, IEnumerable<KwOpening> others)
        {
            List<Vector2> ring = o.Outline(KwOpeningFrames.Width(o.Frame).Wide + Gap / 2f);
            foreach (KwOpening other in others)
            {
                if (other.Wall != o.Wall || other.Id == o.Id || !Overlap(ring, other.Outline(KwOpeningFrames.Width(other.Frame).Wide + Gap / 2f)))
                    continue;
                return other.IsDoorway ? "It would overlap a doorway there (with their frames)" : "It would overlap another opening there (with their frames)";
            }
            return null;
        }

        // On planking: no deck inside the hull between its sill and its top.
        private static string? Deck(KwOpeningSite site, KwOpening o, Func<KwOpeningSite, KwOpening, bool>? crosses) =>
            site.Planking && crosses != null && crosses(site, o) ? "It would cut through a deck inside the hull" : null;

        /// <summary>Whether two convex outlines (anticlockwise) overlap: no edge of either has the other wholly outside it.</summary>
        public static bool Overlap(List<Vector2> a, List<Vector2> b) => !Apart(a, b) && !Apart(b, a);

        // Some edge of a has every point of b on its outside.
        private static bool Apart(List<Vector2> a, List<Vector2> b)
        {
            for (int i = 0; i < a.Count; i++)
            {
                Vector2 p = a[i], e = a[(i + 1) % a.Count] - p;
                bool outside = true;
                foreach (Vector2 q in b)
                    outside &= e.x * (q.y - p.y) - e.y * (q.x - p.x) <= 0f;
                if (outside)
                    return true;
            }
            return false;
        }
    }
}
