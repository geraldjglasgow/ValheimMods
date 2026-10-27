using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One end of a part laid from keel end to keel end (<see cref="KwSpanKeels"/>): its curve ("q", "o" or "transom"),
    /// the keel end's whole length, and how it climbs and, for a stem, where its post is (<see cref="KwStemEnd"/>).
    /// </summary>
    public readonly struct KwSpanKeel
    {
        public KwSpanKeel(string curve, float length, KwStemEnd stem)
        {
            Curve = curve;
            Length = length;
            Stem = stem;
        }

        public string Curve { get; }
        public float Length { get; }
        public KwStemEnd Stem { get; }
    }

    /// <summary>
    /// A part laid from one keel end to another with no rib between (<see cref="KwKeelRun"/>). Its span's end holds both
    /// keel ends, "first/second", each a keel end's own token (<see cref="KwSpan.EndToken"/>) with its whole length after
    /// its curve: "q@4:0.6:3.45/o@8:1.2:4.05" is a quick 4 m stem climbing 0.6 m to a post top 3.45 m over the keel
    /// line, then an 8 m oval stem; a 2 m flat stern climbing 0.5 m is "transom@2:0.5". The part starts under the first
    /// end's tip (x = 0, on the keel line) and runs to the second's: its hull follows the first end's curve back from its
    /// tip to where the curve starts, runs on in its one frame (the midship frame, FrameA) to where the second end's curve
    /// starts, and follows that curve to its tip.
    /// No span saved before these holds a "/", so every older one reads as it always did.
    /// </summary>
    public static class KwSpanKeels
    {
        public const char Split = '/', Size = '@';
        private const float Touch = 0.05f;

        /// <summary>Whether a span's end is a part's from keel end to keel end.</summary>
        public static bool Is(string end) => end.IndexOf(Split) >= 0;

        /// <summary>The end of a part from one keel end to the other.</summary>
        public static string Token(KwKeelEnds first, KwKeelEnds second) => Of(first) + Split + Of(second);

        // A keel end's token with its length after the curve: "q:0.9:3.75" on a 6 m stem is "q@6:0.9:3.75".
        private static string Of(KwKeelEnds end)
        {
            string token = end.Token, length = Size + end.Length.ToString("0.###", CultureInfo.InvariantCulture);
            int colon = token.IndexOf(':');
            return colon < 0 ? token + length : token.Substring(0, colon) + length + token.Substring(colon);
        }

        /// <summary>Both ends of a part from keel end to keel end, from its span's end; false when that does not read.</summary>
        public static bool TryRead(string end, out KwSpanKeel first, out KwSpanKeel second)
        {
            string[] ends = end.Split(Split);
            first = second = default;
            return ends.Length == 2 && Read(ends[0], out first) && Read(ends[1], out second);
        }

        // One end: "curve@length" and its ":rise", for a stem ":rise:top" and its post's depth; never a share along it.
        private static bool Read(string text, out KwSpanKeel end)
        {
            end = default;
            int at = text.IndexOf(Size), colon = text.IndexOf(':');
            int stop = colon < 0 ? text.Length : colon;
            if (at <= 0 || at > stop)
                return false;
            string curve = text.Substring(0, at);
            if ((curve != "q" && curve != "o" && curve != KwSpan.Transom)
                || !float.TryParse(text.Substring(at + 1, stop - at - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float length) || length < 0.5f)
                return false;
            if (!KwSpan.Ending(curve + text.Substring(stop), out _, out float rise, out float top, out float depth, out float from, length) || from > 0f)
                return false;
            end = new KwSpanKeel(curve, length, new KwStemEnd(rise, top, depth));
            return true;
        }

        /// <summary>Whether a part's first end (or its second) is a flat stern, which a transom board closes.</summary>
        public static bool Flat(string end, bool second)
        {
            string[] ends = end.Split(Split);
            return ends.Length == 2 && ends[second ? 1 : 0].StartsWith(KwSpan.Transom + Size);
        }

        /// <summary>
        /// The part's stations: the first end's curve from near its tip back to where it starts, the second end's from where
        /// it starts to near its tip, the length between them in the one frame (two stations of it, the hull straight there).
        /// </summary>
        public static List<Station> Stations(string family, KwFrame frame, float length, string end)
        {
            if (!TryRead(end, out KwSpanKeel first, out KwSpanKeel second))
                return KwShapes.Run(family, frame, length);
            List<Station> stations = End(family, frame, first, false);
            foreach (Station s in End(family, frame, second, true))
            {
                float x = length - second.Length + s.X;
                if (x > stations[stations.Count - 1].X + Touch)
                    stations.Add(new Station(x, s.Half));
            }
            return stations;
        }

        // One end's stations in its own x: the first end's from its tip (x = 0) to where its curve starts (its length), the
        // second end's (far) from where its curve starts (x = 0) to its tip; each the frame's section where its curve starts.
        private static List<Station> End(string family, KwFrame frame, KwSpanKeel end, bool far)
        {
            if (end.Curve != KwSpan.Transom)
                return end.Curve == "o" ? KwShapes.OvalStem(family, frame, end.Length, far, end.Stem) : KwShapes.Stem(family, frame, end.Length, far, end.Stem);
            List<Station> flat = KwShapes.Transom(family, frame, end.Length, end.Stem.Rise);
            if (!far)
                return flat;
            List<Station> mirrored = flat.ConvertAll(s => new Station(end.Length - s.X, s.Half));
            mirrored.Sort((p, q) => p.X.CompareTo(q.X));
            return mirrored;
        }

        /// <summary>
        /// The part meeting both its keel ends as they stand now, each found under its tip (<see cref="KwSpanEnds.Find"/>);
        /// an end with no keel loaded there keeps what it saved.
        /// </summary>
        public static KwSpan Live(KwSpan span, Transform part)
        {
            string[] ends = span.End.Split(Split);
            if (!TryRead(span.End, out KwSpanKeel first, out KwSpanKeel second))
                return span;
            KwKeelEnds? a = KwSpanEnds.Find(part.position, first.Length);
            KwKeelEnds? b = KwSpanEnds.Find(part.position + part.forward * span.Length, second.Length);
            string token = (a != null ? Of(a) : ends[0]) + Split + (b != null ? Of(b) : ends[1]);
            return token == span.End ? span : new KwSpan(span.Family, span.FrameA, span.FrameB, span.Length, token, span.DeckA, span.DeckB, span.Seed)
                { Rail = span.Rail, Paint = span.Paint, Openings = span.Openings };
        }
    }
}
