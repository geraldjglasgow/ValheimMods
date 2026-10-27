using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>A stretch of a wall free for an opening's frame (u along), what bounds it at each end, in words ("the rib"), and the section of its side it lies in.</summary>
    public readonly struct KwFreeSpan
    {
        public KwFreeSpan(float lo, float hi, string loName, string hiName, object? section = null)
        {
            (Lo, Hi, LoName, HiName, Section) = (lo, hi, loName, hiName, section);
        }

        public float Lo { get; }
        public float Hi { get; }
        public string LoName { get; }
        public string HiName { get; }

        /// <summary>The section of its side it lies in (<see cref="KwOpeningLine.Section"/>); null for a wall read alone.</summary>
        public object? Section { get; }
        public float Centre => (Lo + Hi) / 2f;
        public float Length => Hi - Lo;
    }

    /// <summary>
    /// The free stretches of a wall at an opening's heights (the user's request, 2026-10-25: "an easy way to centre things in a
    /// free stretch of a wall, allowing for doors and other things already there"), for the centring snap
    /// (<see cref="KwOpeningSnap"/>) and an open doorway's widest (<see cref="KwOpeningForm"/>): the reach an opening's frame
    /// may take on the wall (<see cref="KwOpeningSite.Allowed"/>: a planking part's ends at its ribs or keel ends, a cabin
    /// wall's corner posts, a raised side's ends), less every opening on the wall whose frame reaches into those heights
    /// (with the gap kept between frames, <see cref="KwOpeningRules.Gap"/>) and every ladder standing against it; openings
    /// skipped are left out (an opening's own widest, a row's members when it is laid out again). Each stretch says what
    /// bounds it ("Centred between the corner and the doorway", "between the rib and the keel end"). Pure.
    /// </summary>
    public static class KwOpeningSpans
    {
        /// <summary>A planking part's ends where its ribs stand, and where it meets a keel end (a stem's or a flat stern's).</summary>
        public const string Rib = "the rib", KeelEnd = "the keel end";

        /// <summary>The free stretches of the site's wall between two heights (an opening's frame's foot and top), lowest first.</summary>
        public static List<KwFreeSpan> Of(KwOpeningSite site, float z0, float z1, IEnumerable<KwOpening> openings, IEnumerable<(float Lo, float Hi)> ladders, Func<KwOpening, bool>? skip = null)
        {
            (float lo, float hi) = site.Allowed(0f, 0f, z0, z1);
            List<(float Lo, float Hi, string Name)> blocks = Blocks(site, z0, z1, openings, skip);
            foreach ((float a, float b) in ladders)
                blocks.Add((a - KwOpeningRules.Gap, b + KwOpeningRules.Gap, "the ladder"));
            blocks.Sort((x, y) => x.Lo.CompareTo(y.Lo));
            List<KwFreeSpan> spans = new List<KwFreeSpan>();
            (float at, string name) = (lo, EndName(site, false));
            foreach ((float a, float b, string what) in blocks)
            {
                if (a >= hi)
                    break;
                if (a > at + 0.01f)
                    spans.Add(new KwFreeSpan(at, a, name, what));
                if (b > at)
                    (at, name) = (b, what);
            }
            if (hi > at + 0.01f)
                spans.Add(new KwFreeSpan(at, hi, name, EndName(site, true)));
            return spans;
        }

        // The openings on the wall whose frames reach into the heights, as stretches taken, the gap counted.
        private static List<(float Lo, float Hi, string Name)> Blocks(KwOpeningSite site, float z0, float z1, IEnumerable<KwOpening> openings, Func<KwOpening, bool>? skip)
        {
            List<(float Lo, float Hi, string Name)> blocks = new List<(float, float, string)>();
            foreach (KwOpening o in openings)
            {
                float wide = KwOpeningFrames.Width(o.Frame).Wide, foot = o.IsDoorway ? o.Sill : o.Sill - wide;
                if (o.Wall != site.Wall || (skip != null && skip(o)) || Mathf.Max(foot, z0) >= Mathf.Min(o.Top + wide, z1))
                    continue;
                blocks.Add((o.U0 - wide - KwOpeningRules.Gap, o.U1 + wide + KwOpeningRules.Gap, Named(site, o)));
            }
            return blocks;
        }

        /// <summary>The stretch a place along lies in; null when it lies in none.</summary>
        public static KwFreeSpan? At(List<KwFreeSpan> spans, float u)
        {
            foreach (KwFreeSpan span in spans)
            {
                if (u >= span.Lo - 0.001f && u <= span.Hi + 0.001f)
                    return span;
            }
            return null;
        }

        /// <summary>What a stretch's centre is, in words: "Centred between the corner and the doorway".</summary>
        public static string Words(KwFreeSpan span)
        {
            if (span.LoName != span.HiName)
                return $"Centred between {span.LoName} and {span.HiName}";
            if (span.LoName == Wall)
                return "Centred on the wall";
            return $"Centred between the two {span.LoName.Substring(4)}s";
        }

        private const string Wall = "the end of the wall";

        // What bounds a wall at its start or its far end: a planking part's rib, or its keel end (it starts at its rib, or
        // under a keel end's tip when it runs from keel end to keel end); a cabin wall's corners; a raised side's ends.
        private static string EndName(KwOpeningSite site, bool far)
        {
            if (!site.Planking)
                return site.OwnWall ? "the corner" : Wall;
            KwSpan? span = site.Span;
            return span == null || (far ? span.RibEnded : !span.TwoEnded) ? Rib : KeelEnd;
        }

        /// <summary>An opening as a stretch's end names it: "the doorway", "the door", "the gunport", "the porthole", "the window".</summary>
        public static string Named(KwOpeningSite site, KwOpening o) => o.Shape switch
        {
            KwOpening.Door => o.HasDoor ? "the door" : "the doorway",
            KwOpening.Round => "the porthole",
            KwOpening.Square when site.Planking => "the gunport",
            _ => "the window",
        };
    }
}
