using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The free stretches along a whole side, where a row of openings is spread (<see cref="KwOpeningRow"/>) and a single
    /// one centred (<see cref="KwOpeningPlace"/>): every part in line (<see cref="KwOpeningLine"/>: the planking along the
    /// keel line on that side, or a cabin's wall alone) read for its own free stretches at an opening's heights
    /// (<see cref="KwOpeningSpans"/>: between its ends, its openings and the ladders against it), all in the aimed part's
    /// measures, each section of the side apart (<see cref="KwOpeningLine.Section"/>, the user's words, 2026-10-25: "the
    /// hull pieces between supports (ribs) should all be treated as one piece, and the hull piece that goes from a support
    /// to the actual keel (the end) should be treated as a separate whole piece"). In the middle, the planking from rib to
    /// rib, the stretches meeting at the rib between two parts are joined into one, so a rib is no end of a row's stretch
    /// there (a place on it moves off it, <see cref="KwOpeningRow.Spread"/>); a part from a rib to a keel end at the bow or
    /// the stern, or from keel end to keel end, is never joined to another, so a row on it stays on it and one on the middle
    /// never runs onto it. Pure but for what the context asks.
    /// </summary>
    public static class KwOpeningRun
    {
        /// <summary>How far apart two parts' stretches may end at the rib between them and still be one: both parts' clearance and a little.</summary>
        public const float Joined = 2f * KwOpeningSite.EndClear + 0.05f;

        /// <summary>The side's free stretches between two heights in the aimed part's measures, along it, each in its section; skip leaves openings out (a part's map and the opening).</summary>
        public static List<KwFreeSpan> Of(KwOpeningLine line, float z0, float z1, KwOpeningContext ctx, Func<KwPartMap, KwOpening, bool>? skip = null)
        {
            Dictionary<object, List<KwFreeSpan>> sections = new Dictionary<object, List<KwFreeSpan>>();
            foreach (KwPartMap map in line.Maps)
            {
                object section = KwOpeningLine.Section(map);
                if (!sections.TryGetValue(section, out List<KwFreeSpan> spans))
                    sections[section] = spans = new List<KwFreeSpan>();
                spans.AddRange(Read(map, section, z0, z1, ctx, skip));
            }
            List<KwFreeSpan> all = new List<KwFreeSpan>();
            foreach (List<KwFreeSpan> spans in sections.Values)
                all.AddRange(Join(spans));
            all.Sort((x, y) => x.Lo.CompareTo(y.Lo));
            return all;
        }

        // One part's free stretches at the heights, in the aimed part's measures, marked with its section.
        private static List<KwFreeSpan> Read(KwPartMap map, object section, float z0, float z1, KwOpeningContext ctx, Func<KwPartMap, KwOpening, bool>? skip)
        {
            (float a, float b) = (map.Z(z0), map.Z(z1));
            Func<KwOpening, bool>? left = skip != null ? o => skip(map, o) : (Func<KwOpening, bool>?)null;
            List<KwFreeSpan> spans = KwOpeningSpans.Of(map.Site, a, b, map.Site.Openings, ctx.Ladders(map.Site, a, b), left);
            return spans.ConvertAll(span => Back(map, span, section));
        }

        // A part's stretch in the aimed part's measures (a part running the other way has its ends the other way round).
        private static KwFreeSpan Back(KwPartMap map, KwFreeSpan span, object section)
        {
            (float lo, float hi) = (map.Back(span.Lo), map.Back(span.Hi));
            return lo <= hi ? new KwFreeSpan(lo, hi, span.LoName, span.HiName, section) : new KwFreeSpan(hi, lo, span.HiName, span.LoName, section);
        }

        // One section's stretches, lowest first, those meeting at a rib (or overlapping) joined into one.
        private static List<KwFreeSpan> Join(List<KwFreeSpan> spans)
        {
            spans.Sort((x, y) => x.Lo.CompareTo(y.Lo));
            List<KwFreeSpan> joined = new List<KwFreeSpan>();
            foreach (KwFreeSpan span in spans)
            {
                int last = joined.Count - 1;
                if (last >= 0 && Meet(joined[last], span))
                    joined[last] = Joining(joined[last], span);
                else
                    joined.Add(span);
            }
            return joined;
        }

        private static KwFreeSpan Joining(KwFreeSpan a, KwFreeSpan b) =>
            new KwFreeSpan(a.Lo, Mathf.Max(a.Hi, b.Hi), a.LoName, b.Hi > a.Hi ? b.HiName : a.HiName, a.Section);

        private static bool Meet(KwFreeSpan a, KwFreeSpan b) =>
            b.Lo <= a.Hi + 0.001f || (a.HiName == KwOpeningSpans.Rib && b.LoName == KwOpeningSpans.Rib && b.Lo - a.Hi <= Joined);

        /// <summary>
        /// The stretch a place along lies in, else the nearest to it; with a section given (the part aimed at's), only that
        /// section's, so a row never goes to another section. Null when there are none.
        /// </summary>
        public static KwFreeSpan? Nearest(List<KwFreeSpan> spans, float u, object? section = null)
        {
            KwFreeSpan? best = null;
            float off = float.MaxValue;
            foreach (KwFreeSpan span in spans)
            {
                float d = u < span.Lo ? span.Lo - u : u > span.Hi ? u - span.Hi : 0f;
                if (d < off && (section == null || Equals(span.Section, section)))
                    (best, off) = (span, d);
            }
            return best;
        }
    }
}
