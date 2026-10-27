using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a saved ship can be stretched (<see cref="KwBpStretch"/>): every planking, deck, railing and keel part covers
    /// a stretch along the ship, open when the part is straight (a run or taper from rib to rib, a keel that does not
    /// climb, the middle of a part laid from keel end to keel end) and closed when it is a stem, a flat stern, a climbing
    /// keel, the keel ends of a part laid between them, or a cabin (<see cref="KwBpHouseText"/>) over the deck sections it
    /// was built on. The stretch is the longest one that straight parts cover and no
    /// closed one touches (of two as long, the one nearer the middle of the hull).
    /// </summary>
    internal static class KwBpStretchZone
    {
        private const float Touch = 0.005f;

        /// <summary>A planking, deck, railing, keel or cabin part's length along the ship and the way it runs (+1 forward, -1 aft).</summary>
        public static bool Extent(KwBpPiece piece, out float length, out float way)
        {
            length = 0f;
            way = (piece.Rotation * Vector3.forward).z;
            if (Mathf.Abs(way) < 0.9f)
                return false;
            way = Mathf.Sign(way);
            string tool = KwBpPartText.Tool(piece), text = KwShipParts.Saved(piece);
            if (KwBpPartText.IsSpan(tool) && KwSpan.TryParse(text, out KwSpan span))
                length = span.Length;
            else if (tool == "keel" && KwKeelSpec.TryParse(text, out KwKeelSpec keel))
                length = keel.Length;
            else
                length = KwBpHouseText.CabinLength(tool, text);
            return length > 0f;
        }

        public static (float From, float To)? Find(KwBlueprint blueprint, KwBpHull hull)
        {
            List<(float A, float B)> open = new List<(float A, float B)>(), closed = new List<(float A, float B)>();
            foreach (KwBpPiece piece in blueprint.Pieces)
                Sort(piece, open, closed);
            List<float> cuts = Cuts(open, closed);
            (float From, float To)? best = null;
            int start = -1;
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                bool free = Free((cuts[i] + cuts[i + 1]) / 2f, open, closed);
                if (free && start < 0)
                    start = i;
                bool last = i + 2 == cuts.Count;
                if (start >= 0 && (!free || last))
                {
                    best = Better(best, (cuts[start], free ? cuts[i + 1] : cuts[i]), hull.Origin.z);
                    start = -1;
                }
            }
            return best;
        }

        // A part's stretch into the open or the closed ones; a part from keel end to keel end closed at both its ends.
        private static void Sort(KwBpPiece piece, List<(float A, float B)> open, List<(float A, float B)> closed)
        {
            if (!Extent(piece, out float length, out float way))
                return;
            float z = piece.Position.z;
            (float A, float B) Along(float a, float b) => (Mathf.Min(z + way * a, z + way * b), Mathf.Max(z + way * a, z + way * b));
            string text = KwShipParts.Saved(piece), tool = KwBpPartText.Tool(piece);
            if (KwHouses.Owns(tool))
            {
                closed.Add(Along(0f, length));
                return;
            }
            if (tool == "keel")
            {
                (KwKeelSpec.TryParse(text, out KwKeelSpec keel) && keel.Rise <= 0.005f ? open : closed).Add(Along(0f, length));
                return;
            }
            KwSpan.TryParse(text, out KwSpan span);
            if (!span.TwoEnded || !KwSpanKeels.TryRead(span.End, out KwSpanKeel first, out KwSpanKeel second))
                (Straight(span) ? open : closed).Add(Along(0f, length));
            else
                Ends(first.Length, length - second.Length, length, Along, open, closed);
        }

        // A part laid from keel end to keel end: closed along both keel ends, open along its straight middle.
        private static void Ends(float middle, float to, float length, System.Func<float, float, (float A, float B)> along,
            List<(float A, float B)> open, List<(float A, float B)> closed)
        {
            closed.Add(along(0f, middle));
            closed.Add(along(to, length));
            if (to > middle)
                open.Add(along(middle, to));
        }

        // A span from a rib that runs or tapers to another rib: not a stem or a flat stern.
        private static bool Straight(KwSpan span) =>
            !span.TwoEnded && KwSpan.Ending(span.End, out string curve, out _, out _, out _, out _) && curve != "q" && curve != "o" && curve != KwSpan.Transom;

        // Every stretch's ends, in order, those within a hair of each other as one.
        private static List<float> Cuts(List<(float A, float B)> open, List<(float A, float B)> closed)
        {
            List<float> all = new List<float>();
            foreach ((float a, float b) in open)
                all.AddRange(new[] { a, b });
            foreach ((float a, float b) in closed)
                all.AddRange(new[] { a, b });
            all.Sort();
            List<float> cuts = new List<float>();
            foreach (float cut in all)
            {
                if (cuts.Count == 0 || cut - cuts[cuts.Count - 1] > Touch)
                    cuts.Add(cut);
            }
            return cuts;
        }

        // A place a straight part covers and no closed part does.
        private static bool Free(float z, List<(float A, float B)> open, List<(float A, float B)> closed)
        {
            foreach ((float a, float b) in closed)
            {
                if (z > a + Touch && z < b - Touch)
                    return false;
            }
            return open.Exists(s => z > s.A && z < s.B);
        }

        private static (float From, float To) Better((float From, float To)? best, (float From, float To) found, float middle)
        {
            if (best == null)
                return found;
            float was = best.Value.To - best.Value.From, now = found.To - found.From;
            if (Mathf.Abs(now - was) > 0.01f)
                return now > was ? found : best.Value;
            float near(float a, float b) => Mathf.Abs((a + b) / 2f - middle);
            return near(found.From, found.To) < near(best.Value.From, best.Value.To) ? found : best.Value;
        }
    }
}
