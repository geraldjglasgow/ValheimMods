using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hull part that ends at a keel end meets that keel as it is now: its end is read from the keel standing at its tip
    /// (rise, post top and post depth, <see cref="KwKeelEnds"/>) each time it builds, not only from what it saved when it
    /// was made, so a part made before a keel changed (or before posts followed their keel's thickness) still closes on
    /// the post. With no keel end at its tip (not loaded yet), the saved end is used; the keel's loading builds it again
    /// (<see cref="KwGenerated.Refit"/>). A part laid from keel end to keel end meets both (<see cref="KwSpanKeels.Live"/>).
    /// It is seated on the keels loaded round it too (<see cref="KwSpan.Seat"/>), so its top edge meets its neighbours'
    /// at every rib.
    /// </summary>
    public static class KwSpanEnds
    {
        private const float Near = 0.3f;
        private static readonly List<Piece> Found = new List<Piece>();

        /// <summary>The part standing at a place as it builds there: its keel ends as they stand now, seated on the keels loaded.</summary>
        public static KwSpan Live(KwSpan span, Transform part)
        {
            KwSpan live = Ended(span, part);
            live.Seat = KwKeelLines.World().Seat(part.position, part.forward, live);
            return live;
        }

        // The part meeting the keel end at its tip as it stands now (both ends' for a part from keel end to keel end).
        private static KwSpan Ended(KwSpan span, Transform part)
        {
            if (span.TwoEnded)
                return KwSpanKeels.Live(span, part);
            if (span.End == "rib" || KwSpan.IsTransom(span.End))
                return span;
            float from = span.EndFrom, whole = span.Length / Mathf.Max(1f - from, 0.05f);
            KwKeelEnds? end = Find(part.position + part.forward * span.Length, whole);
            string token = end != null ? end.TokenFrom(from) : span.End;
            return token == span.End ? span : new KwSpan(span.Family, span.FrameA, span.FrameB, span.Length, token, span.DeckA, span.DeckB, span.Seed)
                { Rail = span.Rail, Paint = span.Paint, Openings = span.Openings };
        }

        /// <summary>The keel end loaded whose tip is over a point (within 30 cm, level) and whose whole length is this, or null.</summary>
        public static KwKeelEnds? Find(Vector3 tip, float whole)
        {
            Found.Clear();
            Piece.GetAllPiecesInRadius(tip, whole + 1f, Found);
            foreach (Piece piece in Found)
            {
                KwKeelEnds? end = piece != null ? KwKeelEnds.Of(piece) : null;
                if (end != null && Mathf.Abs(end.Length - whole) <= 0.05f && Flat(end.Tip - tip) <= Near)
                    return end;
            }
            return null;
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
