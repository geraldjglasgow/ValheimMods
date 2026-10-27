using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The stations of every piece kind, in the piece's own x (starting at 0): hull runs, tapers, stems and transoms from
    /// the joint table; a deck outline from a piece's own section
    /// at a height. Pure numbers.
    /// </summary>
    public static class KwShapes
    {
        /// <summary>How high a flat stern climbs when its span saves no rise of its own (the first flat sterns' rise).</summary>
        public const float TransomRise = 0.5f;

        // how far a deck's edge keeps clear of the planking's inside face; how far a flat stern's sheer sweeps up
        private const float Clear = 0.015f, TransomSheer = 0.3f;

        // How far a flat stern's planking may stand off its keel between two stations: the first flat sterns' 3.5 mm
        // (0.5 m in six stretches), well inside the planking's lap, which closes it on the keel.
        private const float TransomSag = TransomRise / (4f * 6f * 6f);

        public static List<Station> Run(string family, KwFrame frame, float length)
        {
            Vector2[] half = KwJoints.Section(family, frame);
            return new List<Station> { new Station(0f, half), new Station(length, half) };
        }

        public static List<Station> Taper(string family, KwFrame from, KwFrame to, float length, int steps = 6)
        {
            Vector2[] a = KwJoints.Section(family, from), b = KwJoints.Section(family, to);
            List<Station> stations = new List<Station>();
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                stations.Add(new Station(length * t, KwJoints.Blend(a, b, KwJoints.Smoothstep(t))));
            }
            return stations;
        }

        /// <summary>
        /// A stem: narrows to a sliver at the tip while keel and sheer rise. bow: tip at the far end. rise: how high the keel
        /// climbs to the tip; postTop: the highest the sheer may reach there (just under the post's top).
        /// </summary>
        public static List<Station> Stem(string family, KwFrame frame, float length, bool bow, KwStemEnd end, int steps = 12)
        {
            List<Station> stations = new List<Station>();
            float whole = end.Whole(length), s0 = end.From, tip = Tip(frame), face = KwKeel.PostFace(whole, end.PostDepth), last = Inside(whole, end);
            float under = end.Rise * KwKeel.Curve(KwKeelCurve.Quick, s0);
            for (int i = 0; i <= steps; i++)
            {
                float t = Mathf.Lerp(s0, last, (float)i / steps);
                float sheer = StemSheer(frame.Height - end.Low, KwJoints.StemRise(family) * (t * t - s0 * s0), end.PostTop - under, (t - s0) / Mathf.Max(last - s0, 1e-4f));
                float width = tip + (1f - tip) * (1f - Mathf.Pow(Mathf.Clamp01((t - s0) / Mathf.Max(face - s0, 0.01f)), 2.2f));
                Vector2[] half = KwJoints.Shape(family, frame, width, end.Rise * KwKeel.Curve(KwKeelCurve.Quick, t) - under, sheer);
                stations.Add(new Station(bow ? whole * (t - s0) : length - whole * (t - s0), half));
            }
            stations.Sort((a, b) => a.X.CompareTo(b.X));
            return stations;
        }

        /// <summary>
        /// An oval stem, the game's own ships' curves: the plan narrows as a parabola from the joint, the sheer and the keel
        /// stay almost level until late and then sweep up (sheer by <see cref="KwJoints.OvalRise"/> of the depth as t^5,
        /// keel by the keel's oval curve, <see cref="KwKeel.Curve"/>). Stations close up towards the tip, where the curves are steep.
        /// </summary>
        public static List<Station> OvalStem(string family, KwFrame frame, float length, bool bow, KwStemEnd end)
        {
            int steps = Mathf.Max(12, Mathf.CeilToInt(length / 0.6f));
            List<Station> stations = new List<Station>();
            float whole = end.Whole(length), s0 = end.From, last = Inside(whole, end);
            for (int i = 0; i <= steps; i++)
            {
                float t = s0 + Mathf.Sin((float)i / steps * Mathf.PI / 2f) * (last - s0);
                stations.Add(new Station(bow ? whole * (t - s0) : length - whole * (t - s0), OvalHalf(family, frame, whole, t, end)));
            }
            stations.Sort((a, b) => a.X.CompareTo(b.X));
            return stations;
        }

        private static Vector2[] OvalHalf(string family, KwFrame frame, float length, float t, KwStemEnd end)
        {
            float s0 = end.From, under = end.Rise * KwKeel.Curve(KwKeelCurve.Oval, s0), keel = end.Rise * KwKeel.Curve(KwKeelCurve.Oval, t) - under;
            float tip = Tip(frame), along = (t - s0) / Mathf.Max(Inside(length, end) - s0, 1e-4f);
            float sheer = StemSheer(frame.Height - end.Low, KwJoints.OvalRise * frame.Height * (Mathf.Pow(t, 5f) - Mathf.Pow(s0, 5f)), end.PostTop - under, along);
            float face = Mathf.Clamp01((t - s0) / Mathf.Max(KwKeel.PostFace(length, end.PostDepth) - s0, 0.01f));
            return KwJoints.Shape(family, frame, tip + (1f - tip) * (1f - face * face), keel, sheer);
        }

        // A stem's top edge a share along from its rib to its tip: the rib's top (start) and as much as the stem climbs
        // there, kept under its post's top (cap, the part's frame). The cap is eased in from the rib, so a post whose top
        // is lower than the rib's starts the edge at the rib's top all the same and brings it down under the post by the tip.
        private static float StemSheer(float start, float climb, float cap, float along)
        {
            float w = KwJoints.Smoothstep(Mathf.Clamp01(along));
            return Mathf.Min(start + climb, Mathf.Lerp(Mathf.Max(start, cap), cap, w));
        }

        // How wide (a share of the frame's reach past the keel) a stem's hull stays at its tip: a sliver whose laps still
        // fit inside the post (KwKeelBody.PostHalf).
        private static float Tip(KwFrame frame)
        {
            float reach = Mathf.Max(frame.Beam / 2f - frame.KeelHalf, 0.1f), room = Mathf.Max(frame.KeelHalf / 3f, 0.04f);
            return Mathf.Clamp((room - KwPlanks.Lap - 0.005f) / reach, 0.003f, 0.03f);
        }

        // Where along a stem (its share, 0 to 1) the hull stops: halfway into the post, so the post hides its ends.
        private static float Inside(float length, KwStemEnd end) => Mathf.Clamp01(1f - end.PostDepth / 2f / length);

        /// <summary>
        /// A transom: the stern sweeps up and in a little to a flat end at x = 0, its keel climbing by rise as the flat
        /// stern's keel does (<see cref="KwKeel.Curve"/>), and its sheer 0.3 m and as much more as the keel climbs past
        /// <see cref="TransomRise"/>, so a higher stern keeps the hull as deep at its transom. Drawn in
        /// <see cref="TransomSteps"/> stretches, so the planking keeps to its keel however high the stern climbs. low: how
        /// far under the frame's height its top edge starts at its rib (the level keel line under that rib, <see cref="KwSeat"/>).
        /// </summary>
        public static List<Station> Transom(string family, KwFrame frame, float length, float rise, float low = 0f)
        {
            List<Station> stations = new List<Station>();
            int steps = TransomSteps(rise, length);
            float climb = TransomSheer + Mathf.Max(0f, rise - TransomRise);
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float keel = rise * KwKeel.Curve(KwKeelCurve.Flat, t);
                float sheer = frame.Height - low + climb * t * t;
                Vector2[] half = KwJoints.Shape(family, frame, 1f - 0.22f * t * t, keel, sheer);
                stations.Add(new Station(length * (1f - t), half));
            }
            stations.Sort((a, b) => a.X.CompareTo(b.X));
            return stations;
        }

        /// <summary>
        /// How many stretches a flat stern climbing by rise is drawn in: six, as the first 0.5 m flat sterns were, and more
        /// for a higher one. The planking's bottom edge runs straight from station to station while its keel climbs as t²,
        /// so between n even stations it stands up to rise / (4 n²) off the keel; past the planking's lap that opens a slit
        /// along the keel's top edges (a 2 m stern in six stood 1.4 cm off, an 8 m one 5.6 cm). Here never further off than
        /// on the first flat sterns (<see cref="TransomSag"/>), with no stretch shorter than 10 cm (the colliders leave
        /// out shorter ones), which only a very short, very steep stern meets.
        /// </summary>
        public static int TransomSteps(float rise, float length)
        {
            int close = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(0f, rise) / (4f * TransomSag)) - 0.001f);
            return Mathf.Max(6, Mathf.Min(close, Mathf.FloorToInt(length / 0.1f)));
        }

        /// <summary>Half width of a column at height z; 0 below its bottom, its top width above its top.</summary>
        public static float WidthAt(Vector2[] column, float z)
        {
            if (z < column[0].y)
                return 0f;
            for (int i = 0; i + 1 < column.Length; i++)
            {
                Vector2 a = column[i], b = column[i + 1];
                if (a.y <= z && z <= b.y && b.y > a.y)
                    return a.x + (b.x - a.x) * (z - a.y) / (b.y - a.y);
            }
            return column[column.Length - 1].x;
        }

        /// <summary>
        /// A deck's port edge at height z inside the stations, as (x, half width); where the keel rises above the deck
        /// the deck ends at the point they meet, and where the hull's inside closes over a thin keel (a stem's tip) it ends
        /// where its width runs out, never past the stations.
        /// </summary>
        public static List<Vector2> DeckEdge(List<Station> under, float z) => DeckEdge(under, _ => z);

        /// <summary>The same for a deck whose height changes along it: z at each station's x.</summary>
        public static List<Vector2> DeckEdge(List<Station> under, System.Func<float, float> zAt) => DeckEdge(under, zAt, WidthAt);

        /// <summary>The same with a width of its own at each station (the deck's: <see cref="InsideWidth"/>).</summary>
        public static List<Vector2> DeckEdge(List<Station> under, System.Func<float, float> zAt, System.Func<Vector2[], float, float> width)
        {
            List<Vector2> edge = new List<Vector2>();
            for (int i = 0; i < under.Count; i++)
            {
                Station s = under[i];
                float z = zAt(s.X), w = width(s.Half, z);
                if (w > 0f)
                    edge.Add(new Vector2(s.X, w));
                if (i + 1 < under.Count && (w > 0f) != (width(under[i + 1].Half, zAt(under[i + 1].X)) > 0f))
                    edge.Add(new Vector2(Crossing(s, under[i + 1], zAt, width), 0f));
            }
            return edge;
        }

        /// <summary>
        /// A deck's half width at its top z: inside the planking (whose section line is its outside face), the narrower of
        /// its inside face at the deck's top and at its underside, so no part of the deck's edge shows through the hull, a
        /// little clear of it. Low down, where the underside would be under the planking's inside, the top alone.
        /// </summary>
        public static float InsideWidth(Vector2[] half, float z)
        {
            Vector2[] inside = KwPlanks.Inside(half);
            float top = WidthAt(inside, z), under = WidthAt(inside, z - KwPlanks.Thick);
            return Mathf.Max(0f, (under > 0f ? Mathf.Min(top, under) : top) - Clear);
        }

        // Where the deck ends between two stations, one with it and one without (either way along): where the keel line
        // climbs through it, when it does between them. Else the keel is still under the deck and the hull's inside closed
        // over it (at a stem's tip, on a keel whose half width is under the planking's thickness), and the deck ends where
        // its width runs out; the keel line carried on from there would meet the deck far past the hull's end.
        private static float Crossing(Station a, Station b, System.Func<float, float> zAt, System.Func<Vector2[], float, float> width)
        {
            float ka = a.Half[0].y, kb = b.Half[0].y;
            float t = Mathf.Approximately(ka, kb) ? -1f : (zAt(a.X) - ka) / (kb - ka);
            return t >= 0f && t <= 1f ? a.X + (b.X - a.X) * t : RunsOut(a, b, zAt, width);
        }

        // Where between two stations, the one with the deck's width and the other without, that width runs out: their
        // sections blended, the way between them halved.
        private static float RunsOut(Station a, Station b, System.Func<float, float> zAt, System.Func<Vector2[], float, float> width)
        {
            bool wide = width(a.Half, zAt(a.X)) > 0f;
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 12; i++)
            {
                float t = (lo + hi) / 2f;
                if (width(KwJoints.Blend(a.Half, b.Half, t), zAt(Mathf.Lerp(a.X, b.X, t))) > 0f == wide)
                    lo = t;
                else
                    hi = t;
            }
            return Mathf.Lerp(a.X, b.X, (lo + hi) / 2f);
        }
    }
}
