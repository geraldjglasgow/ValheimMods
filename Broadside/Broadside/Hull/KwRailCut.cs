using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One part of a hull railing as a cabin leaves it: from and to along the old part (its own frame, from its start) and the sides it stands on.</summary>
    public readonly struct KwRailPiece
    {
        public KwRailPiece(float from, float to, string sides)
        {
            From = from;
            To = to;
            Sides = sides;
        }

        public float From { get; }
        public float To { get; }

        /// <summary>The part's own letters (<see cref="KwRailText"/>).</summary>
        public string Sides { get; }
    }

    /// <summary>
    /// A hull railing part where a cabin stands (<see cref="KwCabinRails"/>; the user's request, 2026-10-25: a side drawn in
    /// to leave a walkway keeps its railing, only a side at the hull loses it). The cabin takes the part's side under each of
    /// its own sides at the hull, mapped by the way the part runs (<see cref="Taken"/>). Over the cabin's own length the part
    /// keeps the sides left (a one-sided railing, <see cref="KwRailText"/>; none: that stretch goes); past it the railing
    /// stays whole. The cut falls at ribs, as railing parts start and end: the rib inside the part at or before the cabin's
    /// start and the one at or after its end, so the one-sided stretch covers the cabin and no more than to the next rib.
    /// Only a straight run between two ribs of one frame is cut (its pieces are the same run, so the railing does not move);
    /// a part that tapers, follows a stem or ends at a flat stern, or has no rib inside it, keeps one length and loses the
    /// side along all of it. Pure.
    /// </summary>
    public static class KwRailCut
    {
        // How near a rib must be to a part's end to count as at it, or to the cabin's end to count as there.
        private const float Near = 0.05f;

        /// <summary>The part's letters a cabin on its keel line takes: the side under each of its sides at the hull, the part running the cabin's way (sameWay) or back.</summary>
        public static string Taken(KwCabinSpec spec, bool sameWay)
        {
            string taken = "";
            if (spec.Left.AtHull)
                taken += sameWay ? KwRailText.Left : KwRailText.Right;
            if (spec.Right.AtHull)
                taken += sameWay ? KwRailText.Right : KwRailText.Left;
            return taken;
        }

        /// <summary>Whether a part can be cut at a rib inside it and stay where it is: a straight run between two ribs of one frame.</summary>
        public static bool Cuts(KwSpan span) => span.RibEnded && span.EndFrom == 0f && span.FrameA.Same(span.FrameB);

        /// <summary>
        /// The part's pieces once a cabin over lo to hi along it (its own frame, 0 to its length) takes these letters, ribs
        /// standing at the places given along it; null when the cabin takes none of its sides (it stays as it is), empty when
        /// it goes whole.
        /// </summary>
        public static List<KwRailPiece>? Plan(KwSpan span, float lo, float hi, List<float> ribs, string taken)
        {
            string sides = KwRailText.Sides(span.Rail), left = Less(sides, taken);
            if (left == sides)
                return null;
            float length = span.Length;
            (float a, float b) = Cuts(span) ? (Below(ribs, lo, length), Above(ribs, hi, length)) : (0f, length);
            if (b - a < 0.1f)
                (a, b) = (0f, length);
            List<KwRailPiece> pieces = new List<KwRailPiece>();
            if (a > Near)
                pieces.Add(new KwRailPiece(0f, a, sides));
            if (left.Length > 0)
                pieces.Add(new KwRailPiece(a, b, left));
            if (b < length - Near)
                pieces.Add(new KwRailPiece(b, length, sides));
            return pieces;
        }

        // The sides a railing keeps once some are taken ("" for none).
        private static string Less(string sides, string taken)
        {
            string left = "";
            foreach (char side in sides)
                left += taken.IndexOf(side) < 0 ? side.ToString() : "";
            return left;
        }

        // The last rib inside the part at or before a place along it; the part's start without one.
        private static float Below(List<float> ribs, float at, float length)
        {
            float found = 0f;
            foreach (float rib in ribs)
            {
                if (rib > Near && rib < length - Near && rib <= at + Near)
                    found = Mathf.Max(found, rib);
            }
            return found;
        }

        // The first rib inside the part at or after a place along it; the part's end without one.
        private static float Above(List<float> ribs, float at, float length)
        {
            float found = length;
            foreach (float rib in ribs)
            {
                if (rib > Near && rib < length - Near && rib >= at - Near)
                    found = Mathf.Min(found, rib);
            }
            return found;
        }

        /// <summary>
        /// Whether a railing (where it starts, its way, its text) lies along a planking part's top edge as a cut piece of a
        /// railing laid with it would: both straight runs of one shape and frame, the same way, the railing's start on the
        /// planking's line and its end within the planking's length.
        /// </summary>
        public static bool Covers(Vector3 railAt, Vector3 railWay, string railText, Vector3 plankAt, Vector3 plankWay, string plankText)
        {
            if (Vector3.Dot(railWay, plankWay) < 0.999f || !KwSpan.TryParse(railText, out KwSpan rail) || !KwSpan.TryParse(plankText, out KwSpan plank)
                || !Cuts(rail) || !Cuts(plank) || rail.Family != plank.Family || !rail.FrameA.Same(plank.FrameA))
                return false;
            Vector3 off = railAt - plankAt;
            float along = Vector3.Dot(off, plankWay);
            return (off - plankWay * along).magnitude < Near && along > -Near && along + rail.Length < plank.Length + Near;
        }

        /// <summary>
        /// A piece's saved text: the part's own text with only its sides changed when the piece is the whole part; else the
        /// same run cut to the piece's length (it stands at its From along the old part), its spacing and these sides.
        /// </summary>
        public static string Text(string saved, KwSpan span, KwRailPiece piece)
        {
            float spacing = KwRailText.Spacing(span.Rail);
            if (piece.From <= Near && piece.To >= span.Length - Near)
                return KwRailText.With(saved, spacing, piece.Sides);
            KwSpan cut = new KwSpan(span.Family, span.FrameA, span.FrameA, piece.To - piece.From, span.End, span.DeckA, span.DeckB, span.Seed)
                { Paint = span.Paint, Rail = KwRailText.Write(spacing, piece.Sides) };
            return cut.ToString();
        }
    }
}
