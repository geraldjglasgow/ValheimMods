using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The planking, deck and railing tools from one keel end to the other (a keel's post at a bow or stern stem, or a flat
    /// stern): two clicks lay the whole keel line between them. With ribs standing on it (<see cref="KwRunPieces"/>), the
    /// length is cut at every rib, in order: the first end to the first rib, rib to rib, the last rib to the other end, each
    /// the very part clicking its two ends would make (<see cref="KwSpans"/>), so the planks meet at every rib, each part
    /// takes its own ribs' frames and the parts at the ends follow their stems' curves. A gap that already has a part of the
    /// tool (for a deck, one at about the same height) is passed over, so a part-built hull can be filled; a gap whose part
    /// would not build (ribs of two shapes, too far apart, a rib too far from its end) is left out and the player told which
    /// and why, ribs counted from the first end clicked. With no rib on the line, one part runs from end to end through a
    /// midship frame of the rib tool's last shape and size (<see cref="KwSpanKeels"/>), at most <see cref="MaxWhole"/> long.
    /// A deck takes the clicks' heights as between ribs: level at their middle, or sloping from one to the other, the whole
    /// length. The same for building and for the preview.
    /// </summary>
    public static class KwKeelRun
    {
        /// <summary>The longest part laid from keel end to keel end with no rib between, as between ribs.</summary>
        public const float MaxWhole = 12f;

        private const float Level = 0.05f, Square = 0.98f, Touch = 0.05f;
        private const string Counted = "ribs counted from the first keel end clicked";

        /// <summary>Every part from one keel end to the other into plans; what the player reads (see <see cref="KwSpans.Plans"/>).</summary>
        public static string? Plan(KwClick a, KwClick b, bool slant, string tool, List<KwPlan> plans, bool preview)
        {
            KwKeelEnds? endA = KwKeelEnds.Of(a.Piece), endB = KwKeelEnds.Of(b.Piece);
            if (endA == null || endB == null)
                return "Start at a rib or a keel end: click a rib, then another rib or a keel end";
            string? wrong = Facing(endA, endB);
            if (wrong != null)
                return wrong;
            KwRunLine line = new KwRunLine(a, b, endA, endB, slant);
            KwRunPieces near = KwRunPieces.Of(line, tool, preview);
            return near.Ribs.Count == 0 ? Whole(line, near, plans) : Cut(line, near, plans);
        }

        // Two keel ends of one keel line: on one level, each turned toward the other.
        private static string? Facing(KwKeelEnds a, KwKeelEnds b)
        {
            Vector3 run = b.Tip - a.Tip;
            run.y = 0f;
            if (Mathf.Abs(a.Tip.y - b.Tip.y) > Level)
                return "Those keel ends stand at different heights: click the two ends of one keel line";
            if (run.magnitude < 1f || Vector3.Dot(a.Inward, run.normalized) < Square || Vector3.Dot(b.Inward, -run.normalized) < Square)
                return "Those keel ends do not face each other: click the two ends of one keel line";
            return null;
        }

        // Every part between the ribs, in order: those already built passed over, those that would not build left out and told.
        private static string? Cut(KwRunLine line, KwRunPieces near, List<KwPlan> plans)
        {
            List<string> left = new List<string>();
            int there = 0;
            for (int i = 0; i <= near.Ribs.Count; i++)
            {
                string? why = Part(line, near.Ribs, i, out KwPlan? plan);
                if (plan == null)
                    left.Add($"{Gap(i, near.Ribs.Count)}: {why}");
                else if (near.Has(plan))
                    there++;
                else
                    plans.Add(plan);
            }
            return Said(plans.Count, there, left);
        }

        // Part i: the first end to rib 1 (i = 0), rib i to rib i + 1, the last rib to the other end (i = the ribs' count).
        private static string? Part(KwRunLine line, List<Piece> ribs, int i, out KwPlan? plan)
        {
            if (i == 0)
                return KwSpans.RibToEnd(line.At(ribs[0]), line.End(false), true, out plan);
            if (i == ribs.Count)
                return KwSpans.RibToEnd(line.At(ribs[i - 1]), line.End(true), true, out plan);
            return KwSpans.RibToRib(line.At(ribs[i - 1]), line.At(ribs[i]), true, out plan);
        }

        private static string Gap(int i, int ribs) =>
            i == 0 ? "First keel end to rib 1" : i == ribs ? $"Rib {ribs} to the other keel end" : $"Rib {i} to rib {i + 1}";

        // Why nothing builds when no part does; else what was left out, at most two told, and how many were there already
        // (null for nothing).
        private static string? Said(int planned, int there, List<string> left)
        {
            if (planned == 0 && left.Count == 0)
                return there > 0 ? "Every part from keel end to keel end is already built" : "Nothing to build there";
            string built = there > 0 ? $"{there} already built, left as {(there == 1 ? "it is" : "they are")}" : "";
            if (left.Count == 0)
                return built.Length > 0 ? built : null;
            string more = left.Count > 2 ? $"\n(and {left.Count - 2} more)" : "";
            string told = string.Join("\n", left.GetRange(0, Mathf.Min(2, left.Count))) + more + (built.Length > 0 ? "\n" + built : "");
            return (planned > 0 ? $"Left out ({Counted}):\n" : $"Nothing built ({Counted}):\n") + told;
        }

        // No rib on the line: one part from end to end through the rib tool's last frame, unless one is there already.
        private static string? Whole(KwRunLine line, KwRunPieces near, List<KwPlan> plans)
        {
            if (line.Length > MaxWhole + Touch)
                return $"No rib stands between those keel ends, {line.Length:0.0} m apart: a part reaches {MaxWhole:0} m at most, so stand a rib in it";
            if (line.Length + Touch < line.A.Length + line.B.Length)
                return "Those keel ends' curves overlap: their keels must meet end to end, or with keel between them";
            KwFrame frame = KwShapeTool.LastFrame.OnKeel(KwKeel.BodyOf(line.PieceA).Half);
            string shape = KwShapeTool.LastShape;
            KwSpan span = new KwSpan(shape, frame, frame, line.Length, KwSpanKeels.Token(line.A, line.B), line.DeckA, line.DeckB);
            KwPlan plan = new KwPlan(line.Start, line.Axis, span);
            if (near.Has(plan))
                return "That keel line is already built from end to end";
            plans.Add(plan);
            return $"No rib between: laid through the rib tool's last frame ({shape}, {frame.Label})";
        }
    }

    /// <summary>
    /// A keel line between two keel ends, as a run from one to the other lays it (<see cref="KwKeelRun"/>): the ends, where
    /// the first one's tip is on the keel line, the way to the second's and how far, and a deck's height over the keel line
    /// at both ends (from the clicks, as between ribs). It gives each part's ends the clicks that part would have had, at the
    /// deck's height there, so each part is the one clicking its ends makes.
    /// </summary>
    public sealed class KwRunLine
    {
        public KwRunLine(KwClick a, KwClick b, KwKeelEnds endA, KwKeelEnds endB, bool slant)
        {
            (A, B, PieceA, PieceB, Start) = (endA, endB, a.Piece, b.Piece, endA.Tip);
            Vector3 run = endB.Tip - endA.Tip;
            run.y = 0f;
            Length = run.magnitude;
            Axis = Length > 1e-3f ? run / Length : Vector3.forward;
            (DeckA, DeckB) = KwSpans.Heights(a, b, Start.y, slant);
        }

        public KwKeelEnds A { get; }
        public KwKeelEnds B { get; }
        public Piece PieceA { get; }
        public Piece PieceB { get; }

        /// <summary>The first end's tip on the keel line: where a part from end to end starts.</summary>
        public Vector3 Start { get; }

        /// <summary>The way, level, from the first end's tip to the second's.</summary>
        public Vector3 Axis { get; }
        public float Length { get; }

        /// <summary>A deck's height over the keel line at the first end, and at the second.</summary>
        public float DeckA { get; }
        public float DeckB { get; }

        /// <summary>How far along the line a point stands, from the first end's tip.</summary>
        public float Along(Vector3 point) => Vector3.Dot(point - Start, Axis);

        /// <summary>How far, level, a point stands beside the line.</summary>
        public float Aside(Vector3 point)
        {
            Vector3 off = point - Start - Axis * Along(point);
            off.y = 0f;
            return off.magnitude;
        }

        /// <summary>A click on a rib of the line, at the deck's height where it stands.</summary>
        public KwClick At(Piece rib) => new KwClick(rib, Point(Along(rib.transform.position)));

        /// <summary>A click on the first keel end (or the second), at the deck's height at its tip.</summary>
        public KwClick End(bool second) => new KwClick(second ? PieceB : PieceA, Point(second ? Length : 0f));

        // The point this far along the line, at the deck's height over the keel line there.
        private Vector3 Point(float along) =>
            Start + Axis * along + Vector3.up * Mathf.Lerp(DeckA, DeckB, Length > 1e-3f ? along / Length : 0f);
    }
}
