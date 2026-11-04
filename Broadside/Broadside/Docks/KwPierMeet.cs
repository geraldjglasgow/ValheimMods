using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a planned dock (<see cref="KwPierPlan"/>) runs over other docks (user, 2026-11-03: "if you have one dock go to
    /// another that it will connect just fine ... it should just try to attach it to it"; "if ... it goes through another
    /// dock, still allow it to be placed ... we should allow docks to connect"): the spans along its line, from its start,
    /// where its deck (its centre line and both its sides) would lie on another loaded section's deck. The plan is laid
    /// around them: a run up to each one's near edge, the next from its far edge, and an end aimed on (or up to
    /// <see cref="Reach"/> short of) a dock stops at its near edge. The new deck's ends then lie on the other's edges, so
    /// the check for docks in the way (<see cref="KwPierChecks"/>, a little short of each end) passes, and where the decks
    /// are level <see cref="KwPierGaps"/> leaves the curbs out there, as for a branch. Only a deck within
    /// <see cref="Step"/> of the new deck's height counts (a dock passing over a much lower one goes on over it). Plain maths
    /// on the sections' saved shapes.
    /// </summary>
    public static class KwPierMeet
    {
        public const float Reach = 1f;
        private const float Step = 1f, Inside = 0.05f;

        /// <summary>The spans (in, out: metres from the start) up to <paramref name="upTo"/> where the dock lies on other docks, in order, merged.</summary>
        public static List<Vector2> Crossings(KwPierStart start, Vector3 way, float width, float upTo)
        {
            var spans = new List<Vector2>();
            Vector3 side = Vector3.Cross(Vector3.up, way) * Mathf.Max(0f, width * 0.5f - Inside);
            foreach (KwPier other in KwPier.Loaded)
            {
                if (other == null || other == start.Joins || KwPierDeck.Of(other) is not KwPierDeck deck || Mathf.Abs(deck.Start.y - start.At.y) > Step)
                    continue;
                Vector2? span = Over(Span(start.At, way, deck), Span(start.At - side, way, deck), Span(start.At + side, way, deck));
                if (span is Vector2 s && s.y > 0f && s.x < upTo)
                    spans.Add(new Vector2(Mathf.Max(0f, s.x), s.y));
            }
            spans.Sort((a, b) => a.x.CompareTo(b.x));
            return Merge(spans);
        }

        // The three lines' spans together: from the first in to the last out.
        private static Vector2? Over(params Vector2?[] lines)
        {
            Vector2? all = null;
            foreach (Vector2? line in lines)
            {
                if (line is Vector2 l)
                    all = all is Vector2 a ? new Vector2(Mathf.Min(a.x, l.x), Mathf.Max(a.y, l.y)) : l;
            }
            return all;
        }

        private static List<Vector2> Merge(List<Vector2> spans)
        {
            var merged = new List<Vector2>();
            foreach (Vector2 span in spans)
            {
                int last = merged.Count - 1;
                if (last >= 0 && span.x <= merged[last].y)
                    merged[last] = new Vector2(merged[last].x, Mathf.Max(merged[last].y, span.y));
                else
                    merged.Add(span);
            }
            return merged;
        }

        // Where the line from `from` along `way` is on the section's deck (level, across its width and length), in and out.
        private static Vector2? Span(Vector3 from, Vector3 way, KwPierDeck deck)
        {
            Vector3 local = from - deck.Start;
            float half = deck.Width * 0.5f;
            Vector2 across = Slab(Vector3.Dot(local, deck.Right), Vector3.Dot(way, deck.Right), -half, half);
            Vector2 along = Slab(Vector3.Dot(local, deck.Forward), Vector3.Dot(way, deck.Forward), 0f, deck.Length);
            float enter = Mathf.Max(across.x, along.x), leave = Mathf.Min(across.y, along.y);
            return enter <= leave ? new Vector2(enter, leave) : (Vector2?)null;
        }

        // The span of t over which at + t * step lies between low and high (empty: from > to).
        private static Vector2 Slab(float at, float step, float low, float high)
        {
            if (Mathf.Abs(step) < 1e-5f)
                return at >= low && at <= high ? new Vector2(float.NegativeInfinity, float.PositiveInfinity) : new Vector2(1f, 0f);
            float a = (low - at) / step, b = (high - at) / step;
            return a < b ? new Vector2(a, b) : new Vector2(b, a);
        }
    }
}
