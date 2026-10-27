using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a generated hull part spans, the only thing it saves (in its ZDO, so every player builds the same): the hull
    /// shape, the frames of the rib it starts at and of the rib it ends at (<see cref="KwFrame"/>), its length, how it ends
    /// (at a rib, "rib", or at a keel end: a quick or oval stem with its rise and its post's top, "q:rise:top", a flat
    /// stern with its rise, "transom:rise"; or, laid from one keel end to another with no rib between, both ends,
    /// "q@4:0.6:3.45/transom@2:0.5", <see cref="KwSpanKeels"/>) and, for a deck, its height above the keel line at both
    /// ends (the heights the player clicked; equal for a level deck; "NaN" for none) and its own seed for how its boards are laid
    /// (<see cref="KwDeckBoards"/>), a random number given when it is built (<see cref="Seeded"/>; none, 0, for a deck built
    /// before seeds). Its stations run from the start rib (x = 0, the keel line at z = 0) along the part; the same shapes
    /// the joint table and the keels use. An end saved bare, "q", "o" or "transom", reads as such ends were first saved: a
    /// stem climbing 0.15 of its length to a post 3 m over its tip, a flat stern climbing 0.5 m. Planking may end its text
    /// with its painted bands (<see cref="KwPaint"/>, kept as <see cref="Paint"/>), a railing with its posts' spacing
    /// (<see cref="KwRailText"/>, kept as <see cref="Rail"/>); a text without them reads as before. Where it is built on its
    /// ship (the dock, a launched ship, a ghost) it is seated on the ship's keels (<see cref="Seat"/>, never saved): its
    /// ribs' heights then count from the level keel line, so a rib standing a little up a keel end meets every part at one
    /// height.
    /// </summary>
    public sealed class KwSpan
    {
        /// <summary>A flat stern's end, which a transom board closes.</summary>
        public const string Transom = "transom";

        // How high a stem saved with a bare end climbs, as a share of its length.
        private const float BareRise = 0.15f;

        public KwSpan(string family, KwFrame frameA, KwFrame frameB, float length, string end, float deckA = float.NaN, float deckB = float.NaN, int seed = 0)
        {
            Family = family;
            FrameA = frameA;
            FrameB = frameB;
            Length = length;
            End = end;
            DeckA = deckA;
            DeckB = deckB;
            Seed = seed;
        }

        public string Family { get; }
        public KwFrame FrameA { get; }
        public KwFrame FrameB { get; }
        public float Length { get; }
        public string End { get; }
        public float DeckA { get; }
        public float DeckB { get; }

        /// <summary>A deck's own seed for its boards; 0 for none.</summary>
        public int Seed { get; }

        /// <summary>Whether the part runs from one keel end to another, with no rib between (<see cref="KwSpanKeels"/>).</summary>
        public bool TwoEnded => KwSpanKeels.Is(End);

        /// <summary>Planking's painted bands, its text's last segment as saved (<see cref="KwPaint"/>); "" for none.</summary>
        public string Paint { get; internal set; } = "";

        /// <summary>A railing's own last segment, its posts' spacing and its sides (<see cref="KwRailText"/>); "" for none (posts about a metre apart at its stations, both sides).</summary>
        public string Rail { get; internal set; } = "";

        /// <summary>Planking's openings, a segment before its paint (<see cref="KwOpeningText"/>); "" for none.</summary>
        public string Openings { get; internal set; } = "";

        /// <summary>
        /// Where it sits on its ship's keels (<see cref="KwKeelLines.Seat"/>), given where it is built and never saved; null
        /// (as read from its text alone) for its shape as first made, its ribs' heights over their own feet.
        /// </summary>
        public KwSeat? Seat { get; internal set; }

        public override string ToString() => KwRailText.Join(KwPaint.Join(KwOpeningText.Join(Plain(), Openings), Paint), Rail);

        // The span's own text, without its paint.
        private string Plain()
        {
            string text = string.Join("|", Family, FrameA, FrameB, F(Length), End);
            if (Seed != 0)
                return text + "|" + F(DeckA) + "|" + F(DeckB) + "|" + Seed.ToString(CultureInfo.InvariantCulture);
            return float.IsNaN(DeckA) ? text : text + "|" + F(DeckA) + "|" + F(DeckB);
        }

        /// <summary>A deck's saved text with a seed of its own added, when it parses and has none yet; any other text as it is.</summary>
        public static string Seeded(string text)
        {
            if (!TryParse(text, out KwSpan span) || span.Seed != 0)
                return text;
            int seed = UnityEngine.Random.Range(1, int.MaxValue);
            return new KwSpan(span.Family, span.FrameA, span.FrameB, span.Length, span.End, span.DeckA, span.DeckB, seed) { Paint = span.Paint }.ToString();
        }

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        public static bool TryParse(string text, out KwSpan span)
        {
            span = null!;
            string[] p = KwOpeningText.Strip(KwPaint.Strip(KwRailText.Strip(text ?? "", out string rail), out string paint), out string openings).Split('|');
            if ((p.Length != 5 && p.Length != 7 && p.Length != 8) || !KwFrame.TryParse(p[1], out KwFrame a) || !KwFrame.TryParse(p[2], out KwFrame b)
                || !Num(p[3], out float length) || !Reads(p[4]))
                return false;
            float deckA = float.NaN, deckB = float.NaN;
            int seed = 0;
            if (p.Length >= 7 && (!Num(p[5], out deckA) || !Num(p[6], out deckB)))
                return false;
            if (p.Length == 8 && !int.TryParse(p[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
                return false;
            span = new KwSpan(p[0], a, b, length, p[4], deckA, deckB, seed) { Paint = paint, Rail = rail, Openings = openings };
            return Array.IndexOf(KwRibs.Families, span.Family) >= 0 && length > 0.1f;
        }

        /// <summary>
        /// A stem's token for a span: its curve, rise and post top, its post's depth when not the standard keel's, and where
        /// along it the part starts when not at its start.
        /// </summary>
        public static string EndToken(string curve, float rise, float postTop, float postDepth, float from = 0f)
        {
            if (from > 0.001f)
                return string.Join(":", curve, F(rise), F(postTop), F(postDepth), F(from));
            string token = string.Join(":", curve, F(rise), F(postTop));
            return Mathf.Abs(postDepth - KwKeel.PostDepth) >= 0.005f ? token + ":" + F(postDepth) : token;
        }

        /// <summary>A flat stern's token for a span: its curve and its rise.</summary>
        public static string TransomToken(float rise) => Transom + ":" + F(rise);

        /// <summary>Whether a span's end is a flat stern's (one end only, not a part's from keel end to keel end).</summary>
        public static bool IsTransom(string end) => end == Transom || end.StartsWith(Transom + ":", StringComparison.Ordinal);

        /// <summary>Whether the part ends at a rib (laid between two ribs), not at a keel end.</summary>
        public bool RibEnded => !TwoEnded && Ending(out string curve, out _, out _, out _, out _) && curve != "q" && curve != "o" && curve != Transom;

        /// <summary>Where along its keel end a part ending at one starts (a share of the end's length; 0 at its start, and for other ends).</summary>
        public float EndFrom => !TwoEnded && Ending(out _, out _, out _, out _, out float from) ? from : 0f;

        /// <summary>
        /// The level keel line's height in the part's frame (<see cref="KwDef.Level"/>): 0, but on a part starting partway
        /// along a stem (its rib standing on the climbed keel) as far under as the keel has climbed there; seated, its
        /// seat's (<see cref="KwSeat.Level"/>).
        /// </summary>
        public float Level()
        {
            if (Seat != null)
                return Seat.Level;
            if (TwoEnded || !Ending(out string curve, out float rise, out _, out _, out float from) || from <= 0f || IsTransom(End))
                return 0f;
            return -rise * KwKeel.Curve(curve == "o" ? KwKeelCurve.Oval : KwKeelCurve.Quick, from);
        }

        // An end token reads: one end's, or both ends' of a part from keel end to keel end.
        private static bool Reads(string token) =>
            KwSpanKeels.Is(token) ? KwSpanKeels.TryRead(token, out _, out _) : Ending(token, out _, out _, out _, out _, out _);

        // An end token: its curve and rise, and for a stem its post's top, its post's depth (the standard keel's when not
        // given) and where along the keel end the part starts.
        private bool Ending(out string curve, out float rise, out float postTop, out float postDepth, out float from) =>
            Ending(End, out curve, out rise, out postTop, out postDepth, out from, Length);

        /// <summary>One keel end's token read, for a keel end of this length (a bare end's rise and post when it gives none).</summary>
        internal static bool Ending(string token, out string curve, out float rise, out float postTop, out float postDepth, out float from, float length = 0f)
        {
            string[] p = token.Split(':');
            curve = p[0];
            rise = curve == Transom ? KwShapes.TransomRise : BareRise * length;
            postTop = KwKeel.PostTop(rise, KwKeel.PostAbove);
            (postDepth, from) = (KwKeel.PostDepth, 0f);
            if (p.Length == 1)
                return true;
            if (curve == Transom)
                return p.Length == 2 && Num(p[1], out rise) && rise >= 0f;
            return p.Length >= 3 && p.Length <= 5 && Num(p[1], out rise) && Num(p[2], out postTop) && (p.Length == 3 || Num(p[3], out postDepth))
                && (p.Length < 5 || (Num(p[4], out from) && from >= 0f && from < 0.95f));
        }

        private static bool Num(string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        /// <summary>The part's stations, seated on its keels when it has a <see cref="Seat"/>.</summary>
        public List<Station> Stations() => Stations(true);

        // Its stations; bends: a part between two ribs with stations where the keel under it bends (a railing's posts
        // stand at stations, so it leaves them out). Seated, its top edge starts the level keel line under its rib plus
        // the rib's height over it.
        private List<Station> Stations(bool bends)
        {
            if (TwoEnded)
                return KwSpanKeels.Stations(Family, FrameA, Length, End);
            Ending(out string curve, out float rise, out float postTop, out float postDepth, out float from);
            float low = Seat != null ? -Seat.Level : 0f;
            KwStemEnd end = new KwStemEnd(rise, postTop, postDepth, from, low);
            return curve switch
            {
                "q" => KwShapes.Stem(Family, FrameA, Length, true, end),
                "o" => KwShapes.OvalStem(Family, FrameA, Length, true, end),
                Transom => Mirror(KwShapes.Transom(Family, FrameA, Length, rise, low)),
                _ when Seat != null && !Seat.Plain => Seat.Between(Family, FrameA, FrameB, Length, bends),
                _ => FrameA.Same(FrameB) ? KwShapes.Run(Family, FrameA, Length) : KwShapes.Taper(Family, FrameA, FrameB, Length),
            };
        }

        // A transom's stations run from its flat end; here the part starts at the rib, so the flat end comes last.
        private List<Station> Mirror(List<Station> stations)
        {
            List<Station> mirrored = stations.ConvertAll(s => new Station(Length - s.X, s.Half));
            mirrored.Sort((p, q) => p.X.CompareTo(q.X));
            return mirrored;
        }

        /// <summary>The part a tool makes: planking (the hull's skin), a deck, or a railing on the planking's top edge.</summary>
        public KwDef Def(string tool)
        {
            List<Station> stations = Stations(tool != "rail");
            KwDef skin = new KwDef("KW_gen", "Planking", "run")
            {
                Stations = stations, FlatFore = IsTransom(End) || KwSpanKeels.Flat(End, true), FlatAft = KwSpanKeels.Flat(End, false),
                Level = Level(), RibStart = !TwoEnded, RibEnd = RibEnded,
            };
            float lift = Seat?.Level ?? 0f;
            if (tool == "deck")
                return new KwDef("KW_gen", "Deck", "deck") { Stations = stations, DeckZ = Deck(DeckA, FrameA, lift), DeckZEnd = Deck(DeckB, FrameB, lift), Seed = Seed };
            if (tool != "rail")
                return KwOpeningSkin.Cut(skin, Openings);
            KwDef rail = KwRailing.Railing(skin, KwRailText.Spacing(Rail), KwRailText.Sides(Rail));
            rail.Pivot = Vector2.zero;
            return rail;
        }

        /// <summary>
        /// A deck's height at an end: the clicked height, kept between a little above the keel and a little under the rib's
        /// top edge; without one, a deck one bulwark below the top edge, or floorboards low in a hull too shallow for that.
        /// </summary>
        internal static float Deck(float clicked, KwFrame frame) => Deck(clicked, frame, 0f);

        /// <summary>
        /// The same for a seated part (<see cref="Seat"/>), whose rib's top edge is the level keel line (at level in its
        /// frame) plus the rib's height: a clicked height under it kept as clicked, the rest from that top edge.
        /// </summary>
        internal static float Deck(float clicked, KwFrame frame, float level)
        {
            if (!float.IsNaN(clicked))
                return Mathf.Clamp(clicked, 0.15f, frame.Height + level - 0.1f);
            return (frame.Height >= 1.6f ? KwJoints.MainDeckZ(frame) : 0.35f * frame.Height) + level;
        }
    }
}
