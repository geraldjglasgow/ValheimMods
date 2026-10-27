using System.Globalization;

namespace Broadside
{
    /// <summary>
    /// A planking, deck or railing part's saved span (<see cref="KwSpan"/>) made again to new sizes for
    /// <see cref="KwBpReshape"/>: a run or taper to a new length; both frames wider; or the whole span scaled (its frames'
    /// beam and height, its length, its stem's rise and post top or its flat stern's rise, both keel ends of a part from
    /// keel end to keel end with their lengths, and a deck's clicked heights), the stem post's thickness, where along a
    /// keel end the part starts and the hull's clearance under the post's top kept. The deck's seed and the planking's
    /// painted bands stay; its openings move along with its length and grow with its scale. Null and the reason when a frame would be past a rib's limits or a length too short; a span
    /// that does not read comes back as it is.
    /// </summary>
    internal static class KwBpSpanText
    {
        private const float Shortest = 0.2f, ShortestEnd = 0.5f;

        public static string? Lengthened(string saved, float length, out string? error)
        {
            error = null;
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return saved;
            if (!Long(length, out error))
                return null;
            return new KwSpan(span.Family, span.FrameA, span.FrameB, length, span.End, span.DeckA, span.DeckB, span.Seed)
                { Paint = span.Paint, Rail = span.Rail, Openings = KwOpeningSkin.Scaled(span.Openings, length / span.Length, 1f) }.ToString();
        }

        public static string? Widened(string saved, float w, out string? error)
        {
            error = null;
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return Roof(saved, w, 1f);
            KwFrame? a = KwBpPartText.Frame(span.FrameA, w, 1f, out error);
            KwFrame? b = a != null ? KwBpPartText.Frame(span.FrameB, w, 1f, out error) : null;
            if (a == null || b == null)
                return null;
            return new KwSpan(span.Family, a.Value, b.Value, span.Length, span.End, span.DeckA, span.DeckB, span.Seed) { Paint = span.Paint, Rail = span.Rail, Openings = span.Openings }.ToString();
        }

        public static string? Scaled(string saved, float s, out string? error)
        {
            error = null;
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return Roof(saved, 1f, s);
            KwFrame? a = KwBpPartText.Frame(span.FrameA, s, s, out error);
            KwFrame? b = a != null ? KwBpPartText.Frame(span.FrameB, s, s, out error) : null;
            string? end = b != null ? End(span, s, out error) : null;
            if (a == null || b == null || end == null || !Long(span.Length * s, out error))
                return null;
            return new KwSpan(span.Family, a.Value, b.Value, span.Length * s, end, span.DeckA * s, span.DeckB * s, span.Seed)
                { Paint = span.Paint, Rail = span.Rail, Openings = KwOpeningSkin.Scaled(span.Openings, s, s) }.ToString();
        }

        // A roof railing's text (never a span) made again as its cabin is, wider (w) or scaled (s); any other text as it is.
        private static string Roof(string saved, float w, float s) =>
            KwRoofRail.TryParse(saved, out KwRoofRail roof) ? roof.Reshaped(w, s).ToString() : saved;

        private static bool Long(float length, out string? error)
        {
            error = length >= Shortest ? null : $"a hull part would be only {KwBpPartText.Num(length)} m long";
            return error == null;
        }

        // The span's end scaled: a stem's rise and post top, a flat stern's rise, both ends of a keel end to keel end part;
        // an end at a rib as it is.
        private static string? End(KwSpan span, float s, out string? error)
        {
            error = null;
            if (span.TwoEnded)
                return Ends(span.End, s, out error);
            if (!KwSpan.Ending(span.End, out string curve, out float rise, out float top, out float depth, out float from, span.Length))
                return span.End;
            if (curve == KwSpan.Transom)
                return KwSpan.TransomToken(rise * s);
            return curve == "q" || curve == "o" ? KwSpan.EndToken(curve, rise * s, Top(top, s), depth, from) : span.End;
        }

        // The top a stem's post allows the hull, scaled as its post is (its rise and height over the keel scaled, the
        // hull's clearance under the post's top kept: KwKeel.PostTop).
        private static float Top(float top, float s)
        {
            float clearance = KwKeel.PostTop(0f, 0f);
            return (top - clearance) * s + clearance;
        }

        // Both keel ends of a part laid from one to the other, each "curve@length" and its heights, scaled.
        private static string? Ends(string end, float s, out string? error)
        {
            error = null;
            if (!KwSpanKeels.TryRead(end, out KwSpanKeel first, out KwSpanKeel second))
                return end;
            string? a = One(first, s, out error);
            string? b = a != null ? One(second, s, out error) : null;
            return a != null && b != null ? a + KwSpanKeels.Split + b : null;
        }

        private static string? One(KwSpanKeel end, float s, out string? error)
        {
            float length = end.Length * s;
            error = length >= ShortestEnd ? null : $"a keel end would be only {KwBpPartText.Num(length)} m long";
            if (error != null)
                return null;
            string size = KwSpanKeels.Size + length.ToString("0.###", CultureInfo.InvariantCulture);
            if (end.Curve == KwSpan.Transom)
                return KwSpan.Transom + size + KwSpan.TransomToken(end.Stem.Rise * s).Substring(KwSpan.Transom.Length);
            string token = KwSpan.EndToken(end.Curve, end.Stem.Rise * s, Top(end.Stem.PostTop, s), end.Stem.PostDepth);
            return end.Curve + size + token.Substring(end.Curve.Length);
        }
    }
}
