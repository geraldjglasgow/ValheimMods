using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A keel end: a keel with a post, read from its ZDO (<see cref="KwKeelSpec"/>): its curve ("q", "o", or "transom" for
    /// a flat stern), its length, where its tip is (a stem's post, a flat stern's flat end), which way its keel line runs
    /// from there, how high it climbs and the top its post allows the hull.
    /// </summary>
    public sealed class KwKeelEnds
    {
        private KwKeelEnds(string end, float length, Vector3 tip, Vector3 start, float rise, float postTop, float postDepth)
        {
            End = end;
            Length = length;
            Tip = tip;
            Rise = rise;
            PostTop = postTop;
            PostDepth = postDepth;
            Vector3 inward = start - tip;
            inward.y = 0f;
            Inward = inward.sqrMagnitude > 1e-6f ? inward.normalized : Vector3.zero;
        }

        public float PostDepth { get; }

        public string End { get; }
        public float Length { get; }

        /// <summary>The point under its tip on its keel line, at the keel line's level height (where it has not climbed).</summary>
        public Vector3 Tip { get; }

        /// <summary>Which way, level, its keel line runs from the tip to where its curve starts.</summary>
        public Vector3 Inward { get; }
        public float Rise { get; }
        public float PostTop { get; }

        /// <summary>What a span ending here writes for its end.</summary>
        public string Token => TokenFrom(0f);

        /// <summary>What a span ending here writes for its end when it starts a share of the way along this end.</summary>
        public string TokenFrom(float from) =>
            End == KwSpan.Transom ? KwSpan.TransomToken(Rise) : KwSpan.EndToken(End, Rise, PostTop, PostDepth, from);

        /// <summary>The keel end a piece is (a keel with a post: a stem or a flat stern), or null.</summary>
        public static KwKeelEnds? Of(Piece piece)
        {
            ZDO? zdo = KwKeel.IsKeel(piece) ? piece.GetComponent<ZNetView>()?.GetZDO() : null;
            if (zdo == null || !KwKeelSpec.TryParse(zdo.GetString(KwGenerated.Key), out KwKeelSpec spec) || !spec.Post)
                return null;
            float depth = spec.Body.Depth;
            return new KwKeelEnds(Curve(spec.Curve), spec.Length, TipAt(piece, spec.Length, depth), TipAt(piece, 0f, depth), spec.Rise,
                KwKeel.PostTop(spec.Rise, spec.PostAbove), spec.Body.PostDepth);
        }

        private static string Curve(KwKeelCurve curve) => curve == KwKeelCurve.Oval ? "o" : curve == KwKeelCurve.Flat ? KwSpan.Transom : "q";

        // A keel's origin is its keel line's start, its depth below it; its x runs along the piece.
        private static Vector3 TipAt(Piece piece, float kitX, float depth) =>
            piece.transform.TransformPoint(KwMesh.ToUnity(new Vector3(kitX, 0f, depth)));
    }

    /// <summary>
    /// A click with a tool: the piece clicked, the point on it and, when a deck was clicked (it stands for the rib at its
    /// end), that deck's height above the rib's keel line, which the new deck keeps (NaN otherwise).
    /// </summary>
    public readonly struct KwClick
    {
        public KwClick(Piece piece, Vector3 point, float deckHeight = float.NaN)
        {
            Piece = piece;
            Point = point;
            DeckHeight = deckHeight;
        }

        public Piece Piece { get; }
        public Vector3 Point { get; }
        public float DeckHeight { get; }
    }

    /// <summary>A part ready to build: where it starts (the start rib's foot), which way it runs, and its span.</summary>
    public sealed class KwPlan
    {
        public KwPlan(Vector3 position, Vector3 direction, KwSpan span)
        {
            Position = position;
            Direction = direction;
            Span = span;
        }

        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public KwSpan Span { get; }
    }

    /// <summary>
    /// The rules for a tool's two clicks and the part they make: two ribs of one shape at one height, 0.5 to 12 m apart
    /// (the part runs from the first to the second), or a rib and a keel end with the rib standing where the keel end's
    /// curve starts or anywhere along it (the part runs from the rib to the tip, following the rest of the curve), or two
    /// keel ends, which lay the whole keel line between them (<see cref="KwKeelRun"/>). A deck takes the heights clicked: level at their middle, or
    /// sloping from one to the other when slanted. The same for building and for the preview; errors are what the player reads.
    /// </summary>
    public static class KwSpans
    {
        private const float MaxSpan = 12f, MinSpan = 0.5f, StartSlack = 0.4f;

        /// <summary>
        /// Every part two clicks make: the one between a rib and another rib or a keel end, or every part of the keel line
        /// between two keel ends. What the player reads comes back: why nothing builds when no part does, else what was
        /// left out (null for nothing). The preview may reuse what a run between keel ends looked up a moment ago.
        /// </summary>
        public static string? Plans(KwClick a, KwClick b, bool slant, string tool, out List<KwPlan> plans, bool preview = false)
        {
            plans = new List<KwPlan>();
            if (!KwTools.IsRib(a.Piece) && !KwTools.IsRib(b.Piece))
                return KwKeelRun.Plan(a, b, slant, tool, plans, preview);
            string? error = Plan(a, b, slant, out KwPlan? plan);
            if (plan != null)
                plans.Add(plan);
            return error;
        }

        public static string? Plan(KwClick a, KwClick b, bool slant, out KwPlan? plan)
        {
            plan = null;
            bool ribA = KwTools.IsRib(a.Piece), ribB = KwTools.IsRib(b.Piece);
            if (ribA && ribB)
                return RibToRib(a, b, slant, out plan);
            if (ribA || ribB)
                return RibToEnd(ribA ? a : b, ribA ? b : a, slant, out plan);
            return "Start at a rib: click a rib, then another rib or a keel end";
        }

        /// <summary>The part between two ribs (a run between keel ends lays these too).</summary>
        internal static string? RibToRib(KwClick a, KwClick b, bool slant, out KwPlan? plan)
        {
            plan = null;
            KwRibs.Of(a.Piece, out string familyA, out KwFrame frameA);
            KwRibs.Of(b.Piece, out string familyB, out KwFrame frameB);
            Vector3 from = a.Piece.transform.position, along = b.Piece.transform.position - from;
            if (familyA != familyB)
                return $"Planking joins ribs of one shape: {familyA} and {familyB} differ";
            if (Mathf.Abs(along.y) > 0.05f)
                return "Those ribs stand at different heights: put both on one keel";
            along.y = 0f;
            if (along.magnitude < MinSpan || along.magnitude > MaxSpan)
                return $"Ribs must be {MinSpan} to {MaxSpan} m apart (these are {along.magnitude:0.0} m)";
            (float deckA, float deckB) = Heights(a, b, from.y, slant);
            plan = new KwPlan(from, along.normalized, new KwSpan(familyA, frameA, frameB, along.magnitude, "rib", deckA, deckB));
            return null;
        }

        /// <summary>The part from a rib to a keel end (a run between keel ends lays these too).</summary>
        internal static string? RibToEnd(KwClick rib, KwClick keel, bool slant, out KwPlan? plan)
        {
            plan = null;
            KwRibs.Of(rib.Piece, out string family, out KwFrame frame);
            KwKeelEnds end = KwKeelEnds.Of(keel.Piece)!;
            Vector3 from = rib.Piece.transform.position, along = end.Tip - from;
            along.y = 0f;
            float d = along.magnitude, start = d >= end.Length - StartSlack ? 0f : 1f - d / end.Length;
            if (d > end.Length + StartSlack)
                return $"Stand the rib on that keel end or where it starts, within {end.Length:0} m of its tip (it is {d:0.0} m)";
            if (end.End == KwSpan.Transom ? start > 0f : d < MinSpan || start > 1f - end.PostDepth / end.Length)
                return end.End == KwSpan.Transom ? "Stand the rib where the flat stern's keel starts" : "That rib stands too near the keel's tip";
            (float deckA, float deckB) = Heights(rib, keel, from.y, slant);
            plan = new KwPlan(from, along.normalized, new KwSpan(family, frame, frame, start > 0f ? d : end.Length, end.TokenFrom(start), deckA, deckB));
            return null;
        }

        // A deck's height above the start rib's keel line at both ends: an existing deck's height when one was clicked (the
        // new deck carries it on, level), else the two clicks, or their middle for a level deck.
        internal static (float, float) Heights(KwClick a, KwClick b, float keelY, bool slant)
        {
            if (!float.IsNaN(a.DeckHeight) || !float.IsNaN(b.DeckHeight))
            {
                float kept = !float.IsNaN(a.DeckHeight) ? a.DeckHeight : b.DeckHeight;
                return (kept, kept);
            }
            float first = a.Point.y - keelY, second = b.Point.y - keelY;
            return slant ? (first, second) : ((first + second) / 2f, (first + second) / 2f);
        }
    }
}
