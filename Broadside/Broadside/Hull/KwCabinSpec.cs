using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>One end of a cabin along the ship: what is there (open deck, the hull's pointed end or its flat stern) and how far the cabin is drawn in from it.</summary>
    public readonly struct KwCabinEnd
    {
        /// <summary>What the chosen deck sections end at: midships (a bulkhead closes the cabin), the hull's pointed end, a flat stern.</summary>
        public const int Open = 0, Pointed = 1, Flat = 2;

        public KwCabinEnd(int hull, float inset)
        {
            Hull = hull;
            Inset = inset;
        }

        public int Hull { get; }
        public float Inset { get; }
    }

    /// <summary>
    /// One side of a cabin: how far in from the hull the edge it stands on lies (<see cref="Base"/>: 0 on a deck, the side
    /// of the cabin under it on a roof) and how far the player drew it in from there (<see cref="Inset"/>).
    /// </summary>
    public readonly struct KwCabinSide
    {
        public KwCabinSide(float @base, float inset)
        {
            Base = @base;
            Inset = inset;
        }

        public float Base { get; }
        public float Inset { get; }
        public float Total => Base + Inset;

        /// <summary>The side stands at the hull: the hull's planking is its wall.</summary>
        public bool AtHull => Total < 0.001f;
    }

    /// <summary>
    /// What a cabin (<see cref="KwCabinTool"/>) saves in its ZDO under the generated parts' key, the only thing it is built
    /// from, so every peer, a blueprint and a launched ship build the same cabin without the hull round it: the hull's
    /// sections under the deck sections it was built on, in its own frame (x from its back end, 0, to its front, the way
    /// the part faces; y across, its left +; z up from the keel line), the deck's height under it, its roof's top, the
    /// height its drawn-in sides are measured at (<see cref="Ref"/>), its two ends and two sides, and its roof's seed for its
    /// boards. Written "cabin|c2|floor|top|ref|back|front|left|right|seed|x:y,z;y,z;...|x:...", an end as "hull,inset", a
    /// side as "base,inset"; its openings (doorways, windows) after it all as "|open=..." (<see cref="KwOpeningText"/>, none
    /// in a text saved before them). Old castles' texts ("castle|c1|floor|deck|rail|flat|seed|sections", x from the bulkhead out to
    /// the ship's end) read as cabins: open at the back, the hull's end at the front, no insets, their rail left out. Never
    /// read as a hull part's span (<see cref="KwSpan"/>), so the hull's measures, floors, slices and water mask leave cabins out.
    /// A text is the cabin asked for (<see cref="Asked"/>, the window's values); what it builds is that cabin made to fit its
    /// hull (<see cref="KwCabinFit"/>): every spec read or made here is the fitted one, and writes what was asked.
    /// </summary>
    public sealed class KwCabinSpec
    {
        public const string Tag = "cabin", Version = "c2";
        private const string OldTag = "castle", OldVersion = "c1";

        /// <summary>The cabin's height limits (its roof's top over the deck) and the most any side or end is drawn in.</summary>
        public const float MinHeight = 1f, MaxHeight = 3.5f, MaxInset = 6f;

        // Texts read lately and the cabins they build, as the tools ask for the same few every frame.
        private const int Kept = 64;
        private static readonly Dictionary<string, KwCabinSpec?> read = new Dictionary<string, KwCabinSpec?>();

        public KwCabinSpec(List<Station> stations, float floor, float top, float reference, KwCabinEnd back, KwCabinEnd front, KwCabinSide left, KwCabinSide right, int seed)
        {
            (Stations, Floor, Top, Ref, Seed) = (stations, floor, top, reference, seed);
            (Back, Front, Left, Right) = (back, front, left, right);
            Asked = this;
        }

        /// <summary>The cabin as asked, which its text saves (the window's values): this one itself when nothing had to be fitted.</summary>
        public KwCabinSpec Asked { get; private set; }

        /// <summary>What was changed to make it fit (<see cref="KwCabinFit"/>), each a sentence for the window's note; none when nothing was.</summary>
        public IReadOnlyList<string> Fitted { get; private set; } = Array.Empty<string>();

        /// <summary>Why no cabin fits here at all (it is drawn as a plain frame); null when it builds.</summary>
        public string? Unbuilt { get; private set; }

        /// <summary>The hull's sections under the chosen deck sections, from the back end (x = 0) to the front, in order.</summary>
        public List<Station> Stations { get; }

        /// <summary>Heights over the keel line: the deck's top under the cabin, the roof's top, and where drawn-in sides are measured from the hull's inside.</summary>
        public float Floor { get; }
        public float Top { get; }
        public float Ref { get; }

        public KwCabinEnd Back { get; }
        public KwCabinEnd Front { get; }
        public KwCabinSide Left { get; }
        public KwCabinSide Right { get; }
        public int Seed { get; }

        /// <summary>The openings cut in its walls and raised sides (<see cref="KwOpening"/>), its text's last segment; none for a text without it.</summary>
        public List<KwOpening> Openings { get; private set; } = new List<KwOpening>();

        /// <summary>The same cabin with these openings in place of its own.</summary>
        public KwCabinSpec Opened(List<KwOpening> openings) =>
            KwCabinFit.Of(new KwCabinSpec(Asked.Stations, Asked.Floor, Asked.Top, Asked.Ref, Asked.Back, Asked.Front, Asked.Left, Asked.Right, Asked.Seed) { Openings = openings });

        public float Length => Stations[Stations.Count - 1].X;
        public float Height => Top - Floor;

        /// <summary>Where the cabin itself starts and ends along its frame, its back and front drawn in.</summary>
        public float X0 => Back.Inset;
        public float X1 => Length - Front.Inset;

        /// <summary>The same cabin asked for with another height and insets (an edit, the window), its hull and seed kept, fitted.</summary>
        public KwCabinSpec With(float height, float back, float front, float left, float right)
        {
            KwCabinSpec a = Asked;
            return KwCabinFit.Of(new KwCabinSpec(a.Stations, a.Floor, a.Floor + height, a.Ref, new KwCabinEnd(a.Back.Hull, back), new KwCabinEnd(a.Front.Hull, front),
                new KwCabinSide(a.Left.Base, left), new KwCabinSide(a.Right.Base, right), a.Seed) { Openings = a.Openings });
        }

        /// <summary>A cabin of these sizes over a range with its own seed and openings (one on a roof set on it again), fitted.</summary>
        public static KwCabinSpec Over(KwCabinRange range, KwCabinSizes sizes, int seed, List<KwOpening> openings) =>
            KwCabinFit.Of(new KwCabinSpec(range.Stations, range.Floor, range.Floor + sizes.Height, range.Ref, new KwCabinEnd(range.BackHull, sizes.Back),
                new KwCabinEnd(range.FrontHull, sizes.Front), new KwCabinSide(range.LeftBase, sizes.Left), new KwCabinSide(range.RightBase, sizes.Right), seed) { Openings = openings });

        /// <summary>The same cabin with a seed of its own for its roof's boards.</summary>
        public KwCabinSpec Seeded(int seed) =>
            KwCabinFit.Of(new KwCabinSpec(Asked.Stations, Asked.Floor, Asked.Top, Asked.Ref, Asked.Back, Asked.Front, Asked.Left, Asked.Right, seed) { Openings = Asked.Openings });

        /// <summary>This cabin's hull, deck, reference and seed with another top, ends and sides (a cabin the fit tries), its openings kept; not fitted.</summary>
        internal KwCabinSpec Made(float top, KwCabinEnd back, KwCabinEnd front, KwCabinSide left, KwCabinSide right) =>
            new KwCabinSpec(Stations, Floor, top, Ref, back, front, left, right, Seed) { Openings = Openings };

        /// <summary>This one as what is built for a cabin asked (<see cref="KwCabinFit"/>): what was asked, what was changed and why none fits.</summary>
        internal KwCabinSpec Fit(KwCabinSpec asked, List<string> fitted, string? unbuilt)
        {
            (Asked, Fitted, Unbuilt) = (asked, fitted, unbuilt);
            return this;
        }

        /// <summary>
        /// The cabin on a hull built back other sizes (a blueprint widened or scaled): its sections w times as wide and every
        /// measure along and up s times, its sides' insets w and s times, its ends' s times, as the hull under it is made again.
        /// </summary>
        public KwCabinSpec Reshaped(float w, float s)
        {
            KwCabinSpec a = Asked;
            return KwCabinFit.Of(new KwCabinSpec(a.Stations.ConvertAll(st => new Station(st.X * s, Array.ConvertAll(st.Half, p => new Vector2(p.x * w * s, p.y * s)))),
                a.Floor * s, a.Top * s, a.Ref * s, new KwCabinEnd(a.Back.Hull, a.Back.Inset * s), new KwCabinEnd(a.Front.Hull, a.Front.Inset * s),
                new KwCabinSide(a.Left.Base * w * s, a.Left.Inset * w * s), new KwCabinSide(a.Right.Base * w * s, a.Right.Inset * w * s), a.Seed)
                { Openings = a.Openings.ConvertAll(o => o.Scaled(o.Wall == "back" || o.Wall == "front" ? w * s : s, s)) });
        }

        /// <summary>The saved text: the cabin as asked (<see cref="Asked"/>), never what the fit changed.</summary>
        public override string ToString() => ReferenceEquals(Asked, this) ? Text() : Asked.ToString();

        private string Text()
        {
            List<string> parts = new List<string> { Tag, Version, F(Floor), F(Top), F(Ref), End(Back), End(Front), Side(Left), Side(Right), Seed.ToString(CultureInfo.InvariantCulture) };
            foreach (Station station in Stations)
                parts.Add(F(station.X) + ":" + string.Join(";", Array.ConvertAll(station.Half, p => F(p.x) + "," + F(p.y))));
            return KwOpeningText.Join(string.Join("|", parts), KwOpeningText.Write(Openings));
        }

        private static string End(KwCabinEnd end) => end.Hull.ToString(CultureInfo.InvariantCulture) + "," + F(end.Inset);

        private static string Side(KwCabinSide side) => F(side.Base) + "," + F(side.Inset);

        /// <summary>
        /// A cabin's text, or an old castle's, as the cabin it builds (fitted to its hull: odd numbers, an inset under 0 or a
        /// roof under its deck, are fitted too, never refused); false for anything else, or no hull to stand in.
        /// </summary>
        public static bool TryParse(string? text, out KwCabinSpec spec)
        {
            string key = text ?? "";
            if (!read.TryGetValue(key, out KwCabinSpec? known))
            {
                if (read.Count >= Kept)
                    read.Clear();
                read[key] = known = Parse(key);
            }
            spec = known!;
            return known != null;
        }

        // A text read as what it asks for (its openings too), then fitted; null when it does not read.
        private static KwCabinSpec? Parse(string text)
        {
            string[] p = KwOpeningText.Strip(text, out string openings).Split('|');
            KwCabinSpec asked = null!;
            bool ok = p.Length >= 2 && p[0] == Tag && p[1] == Version ? ReadNew(p, out asked) : p.Length >= 2 && p[0] == OldTag && p[1] == OldVersion && ReadOld(p, out asked);
            if (!ok || asked.Stations.Count < 2)
                return null;
            asked.Openings = KwOpeningText.Read(openings);
            return KwCabinFit.Of(asked);
        }

        // "cabin|c2|floor|top|ref|back|front|left|right|seed|sections".
        private static bool ReadNew(string[] p, out KwCabinSpec spec)
        {
            spec = null!;
            if (p.Length < 12 || !Num(p[2], out float floor) || !Num(p[3], out float top) || !Num(p[4], out float reference) || !EndOf(p[5], out KwCabinEnd back)
                || !EndOf(p[6], out KwCabinEnd front) || !SideOf(p[7], out KwCabinSide left) || !SideOf(p[8], out KwCabinSide right)
                || !int.TryParse(p[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed) || !Sections(p, 10, out List<Station> stations))
                return false;
            spec = new KwCabinSpec(stations, floor, top, reference, back, front, left, right, seed);
            return true;
        }

        // An old castle, "castle|c1|floor|deck|rail|flat|seed|sections": its bulkhead at the back, the ship's end at the front.
        private static bool ReadOld(string[] p, out KwCabinSpec spec)
        {
            spec = null!;
            if (p.Length < 9 || !Num(p[2], out float floor) || !Num(p[3], out float deck) || !Num(p[4], out _)
                || !int.TryParse(p[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed) || !Sections(p, 7, out List<Station> stations))
                return false;
            KwCabinEnd front = new KwCabinEnd(p[5] == "1" ? KwCabinEnd.Flat : KwCabinEnd.Pointed, 0f);
            spec = new KwCabinSpec(stations, floor, deck, floor, new KwCabinEnd(KwCabinEnd.Open, 0f), front, new KwCabinSide(0f, 0f), new KwCabinSide(0f, 0f), seed);
            return true;
        }

        // "hull,inset": what the end is at (0 to 2) and how far it is drawn in.
        private static bool EndOf(string text, out KwCabinEnd end)
        {
            end = default;
            string[] two = text.Split(',');
            if (two.Length != 2 || !int.TryParse(two[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hull) || hull < 0 || hull > 2 || !Num(two[1], out float inset))
                return false;
            end = new KwCabinEnd(hull, inset);
            return true;
        }

        // "base,inset": how far in the edge under the side is, and how far the side is drawn in from it.
        private static bool SideOf(string text, out KwCabinSide side)
        {
            side = default;
            string[] two = text.Split(',');
            if (two.Length != 2 || !Num(two[0], out float under) || !Num(two[1], out float inset))
                return false;
            side = new KwCabinSide(under, inset);
            return true;
        }

        // The sections from part i on, each further along than the last.
        private static bool Sections(string[] p, int from, out List<Station> stations)
        {
            stations = new List<Station>();
            for (int i = from; i < p.Length; i++)
            {
                if (!Section(p[i], out Station station) || (stations.Count > 0 && station.X <= stations[stations.Count - 1].X))
                    return false;
                stations.Add(station);
            }
            return stations.Count >= 2;
        }

        // "x:y,z;y,z;...": one section, at least two points.
        private static bool Section(string text, out Station station)
        {
            station = default;
            string[] at = text.Split(':');
            if (at.Length != 2 || !Num(at[0], out float x))
                return false;
            string[] points = at[1].Split(';');
            Vector2[] half = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                string[] yz = points[i].Split(',');
                if (yz.Length != 2 || !Num(yz[0], out float y) || !Num(yz[1], out float z))
                    return false;
                half[i] = new Vector2(y, z);
            }
            station = new Station(x, half);
            return half.Length >= 2;
        }

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool Num(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
