using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>A free stretch of an edge, between two bounds a ladder fits between, and what bounds it at each end.</summary>
    public readonly struct KwLadderSpan
    {
        public KwLadderSpan(float from, float to, string low, string high)
        {
            From = from;
            To = to;
            Low = low;
            High = high;
        }

        public float From { get; }
        public float To { get; }
        public string Low { get; }
        public string High { get; }
        public float Middle => (From + To) / 2f;
        public float Width => To - From;
    }

    /// <summary>
    /// Where the ladder tool puts a ladder on an edge (<see cref="At"/>, along it; null for nowhere) and what it says: the
    /// note for the placement message (centred between two things, in front of a window), why not, and what the hover
    /// tells the player (the note, or a refusal by name).
    /// </summary>
    public readonly struct KwLadderPick
    {
        public KwLadderPick(float? at, string note, string why, string tell)
        {
            At = at;
            Note = note;
            Why = why;
            Tell = tell;
        }

        public float? At { get; }
        public string Note { get; }
        public string Why { get; }
        public string Tell { get; }
    }

    /// <summary>
    /// The ladder tool's centring on an open edge (the user's request, 2026-10-25: "some way that users can easily get a
    /// centring snap point on a certain area that accounts for doors and so on"). The edge's bounds (its ends, the doorways
    /// and windows in the wall under it, other ladders on it, corner posts and anything solid below it, where the floor
    /// below ends, <see cref="KwLadderNear"/>) leave free spans where a ladder fits with <see cref="Clear"/> to spare each
    /// side. The middle of each span snaps the ladder when the aim is within <see cref="Centre"/> of it, its quarter points
    /// (a span at least <see cref="QuarterSpan"/> long) within <see cref="Quarter"/>; else the 0.25 m grid from the edge's
    /// middle (<see cref="KwLadderSpots.Grid"/>), kept clear of the ends and the corner posts there. A spot whose ladder
    /// would stand in front of a doorway is refused ("That would block the doorway"), as one over another ladder or
    /// something solid; one in front of a window is allowed, and said. Pure.
    /// </summary>
    public static class KwLadderSpans
    {
        /// <summary>Metres: the room kept each side of a centred ladder, the snaps' reaches, the shortest span with quarter points.</summary>
        public const float Clear = 0.05f, Centre = 0.4f, Quarter = 0.2f, QuarterSpan = 2.4f;

        private const float Far = 1000f, Touch = 0.005f;
        private static readonly string[] Ranks = { KwLadderBound.HullSide, KwLadderBound.Corner, KwLadderBound.DeckEnd, "ladder", "window", "doorway" };

        // One run of bounds no ladder fits between: from, to, the bounds at its two faces, and the edge's end it holds (if any).
        private sealed class Block
        {
            public float From, To;
            public KwLadderBound Low, High;
            public string? End;
        }

        /// <summary>The spot along an edge the aim (s along it) snaps to among the edge's bounds, and what is said of it.</summary>
        public static KwLadderPick Pick(KwLadderEdge edge, List<KwLadderBound> bounds, float aim)
        {
            List<KwLadderSpan> spans = Free(edge.Length, edge.StartName, edge.EndName, bounds);
            KwLadderPick? snapped = Centred(spans, aim) ?? Quartered(spans, aim);
            if (snapped != null)
                return snapped.Value;
            (float lo, float hi) = Ends(edge.Length, bounds);
            float? at = KwLadderSpots.Grid(edge.Length, lo, hi, aim) ?? KwLadderSpots.Grid(edge.Length, 0f, edge.Length, aim);
            return at != null ? Checked(at.Value, bounds) : new KwLadderPick(null, "", "That edge is too short for a ladder", "");
        }

        /// <summary>The free spans along an edge that long between its bounds and its two ends (named), in order.</summary>
        public static List<KwLadderSpan> Free(float length, string start, string end, List<KwLadderBound> bounds)
        {
            List<Block> blocks = Blocks(length, start, end, bounds);
            List<KwLadderSpan> spans = new List<KwLadderSpan>();
            for (int i = 0; i + 1 < blocks.Count; i++)
                spans.Add(new KwLadderSpan(blocks[i].To, blocks[i + 1].From, Face(blocks[i], blocks[i].High), Face(blocks[i + 1], blocks[i + 1].Low)));
            return spans;
        }

        // The bounds and the edge's ends sorted along it, those closer than a ladder's room merged into blocks.
        private static List<Block> Blocks(float length, string start, string end, List<KwLadderBound> bounds)
        {
            List<KwLadderBound> all = new List<KwLadderBound>(bounds)
            {
                new KwLadderBound(-Far, 0f, KwLadderBoundKind.End, start), new KwLadderBound(length, length + Far, KwLadderBoundKind.End, end),
            };
            all.Sort((a, b) => a.From.CompareTo(b.From));
            float need = 2f * (KwLadderSpots.Half + Clear);
            List<Block> blocks = new List<Block>();
            foreach (KwLadderBound b in all)
            {
                Block? last = blocks.Count > 0 ? blocks[blocks.Count - 1] : null;
                if (last == null || b.From - last.To >= need)
                    blocks.Add(last = new Block { From = b.From, To = b.To, Low = b, High = b });
                else if (b.To > last.To)
                    (last.To, last.High) = (b.To, b);
                if (b.Kind == KwLadderBoundKind.End)
                    last.End = b.Name;
            }
            return blocks;
        }

        // A block's face named: by its bound, but something solid or the floor's end beside an end of the edge goes by the end's name.
        private static string Face(Block block, KwLadderBound bound) =>
            block.End != null && (bound.Kind == KwLadderBoundKind.Solid || bound.Kind == KwLadderBoundKind.NoFloor || bound.Kind == KwLadderBoundKind.End) ? block.End : bound.Name;

        // The middle of the span nearest the aim, when it is within reach.
        private static KwLadderPick? Centred(List<KwLadderSpan> spans, float aim)
        {
            KwLadderPick? best = null;
            float nearest = Centre;
            foreach (KwLadderSpan span in spans)
            {
                if (Mathf.Abs(aim - span.Middle) <= nearest)
                    (best, nearest) = (Snapped(span.Middle, Between(span)), Mathf.Abs(aim - span.Middle));
            }
            return best;
        }

        // A quarter point of a long span nearest the aim, when it is within reach (a quarter from each end).
        private static KwLadderPick? Quartered(List<KwLadderSpan> spans, float aim)
        {
            KwLadderPick? best = null;
            float nearest = Quarter;
            foreach (KwLadderSpan span in spans)
            {
                if (span.Width < QuarterSpan)
                    continue;
                foreach ((float at, string from, string to) in new[] { (span.From + span.Width / 4f, span.Low, span.High), (span.To - span.Width / 4f, span.High, span.Low) })
                {
                    if (Mathf.Abs(aim - at) <= nearest)
                        (best, nearest) = (Snapped(at, QuarterNote(from, to)), Mathf.Abs(aim - at));
                }
            }
            return best;
        }

        private static KwLadderPick Snapped(float at, string note) => new KwLadderPick(at, note, "", note);

        // "A quarter of the way from the hull side to the doorway"; between two of a kind, "A quarter of the way between the two hull sides".
        private static string QuarterNote(string from, string to) =>
            from == to ? $"A quarter of the way between the two {from}s" : $"A quarter of the way from the {from} to the {to}";

        /// <summary>"Centred between the hull side and the doorway": the span's two bounds, the edge's ends first, a doorway last.</summary>
        public static string Between(KwLadderSpan span)
        {
            if (span.Low == span.High)
                return $"Centred between the two {span.Low}s";
            bool lowFirst = Rank(span.Low) <= Rank(span.High);
            return $"Centred between the {(lowFirst ? span.Low : span.High)} and the {(lowFirst ? span.High : span.Low)}";
        }

        private static int Rank(string name)
        {
            int i = System.Array.IndexOf(Ranks, name);
            return i >= 0 ? i : Ranks.Length;
        }

        // How far in from each end of the edge a ladder must keep: past the corner posts and anything solid against an end.
        private static (float Lo, float Hi) Ends(float length, List<KwLadderBound> bounds)
        {
            float lo = 0f, hi = length;
            for (bool moved = true; moved;)
            {
                moved = false;
                foreach (KwLadderBound b in bounds)
                {
                    if (b.Kind != KwLadderBoundKind.Solid)
                        continue;
                    if (b.From <= lo + Touch && b.To > lo)
                        (lo, moved) = (b.To, true);
                    if (b.To >= hi - Touch && b.From < hi)
                        (hi, moved) = (b.From, true);
                }
            }
            return (lo, hi);
        }

        // A grid spot: refused over a doorway, another ladder or something solid; said when it stands in front of a window.
        private static KwLadderPick Checked(float at, List<KwLadderBound> bounds)
        {
            float a = at - KwLadderSpots.Half, b = at + KwLadderSpots.Half;
            List<KwLadderBound> over = bounds.FindAll(x => a < x.To - Touch && b > x.From + Touch);
            string? why = Refusal(over);
            if (why != null)
                return new KwLadderPick(null, "", why, why);
            string note = over.Exists(x => x.Kind == KwLadderBoundKind.Window) ? "In front of a window" : "";
            return new KwLadderPick(at, note, "", note);
        }

        // Why a ladder may not stand in front of these: a doorway, another ladder, something solid; null when it may.
        private static string? Refusal(List<KwLadderBound> over)
        {
            if (over.Exists(x => x.Kind == KwLadderBoundKind.Doorway))
                return "That would block the doorway";
            if (over.Exists(x => x.Kind == KwLadderBoundKind.Ladder))
                return "Another ladder stands there";
            int solid = over.FindIndex(x => x.Kind == KwLadderBoundKind.Solid);
            if (solid < 0)
                return null;
            string name = over[solid].Name;
            return name == KwLadderBound.Corner ? "A corner post is in the way there"
                : name == KwLadderBound.Obstacle ? "Something is in the way there" : $"The {name} is in the way there";
        }
    }
}
