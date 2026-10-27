using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One jib as it is cut and set (<see cref="KwJibPlan"/>), in the bowsprit's kit axes: its tack where its stay comes down
    /// to the bowsprit, its stay up to the foremast, its luff up the stay and its foot aft, rising a little, both as long
    /// as the bowsprit makes them, and its area (the triangle's own, its luff and foot not square to each other); and how
    /// much cloth its texture spans (<see cref="Cloth"/>).
    /// </summary>
    public sealed class KwJibCut
    {
        public KwJibCut(Vector3 tack, Vector3 stayTop, Vector3 forward, float luff, float foot, float alpha, float beta, float cloth = 0f)
        {
            (Tack, StayTop, Forward, Luff, Foot, Alpha, Beta, Cloth) = (tack, stayTop, forward, luff, foot, alpha, beta, cloth);
        }

        /// <summary>
        /// The metres of cloth the texture spans on it: as on its foremast's lowest sail (the mean of its width and height),
        /// so the jib shows the cloth's panels the size that sail does (<see cref="KwSailFrame.Scale"/>); 0 to fit the jib.
        /// </summary>
        public float Cloth { get; }

        public Vector3 Tack { get; }
        public Vector3 StayTop { get; }

        /// <summary>Level, from the stay's top toward its foot: the jib's plane's forward.</summary>
        public Vector3 Forward { get; }

        public float Luff { get; }
        public float Foot { get; }

        /// <summary>The stay's rise over level, and the foot's rise aft (radians).</summary>
        public float Alpha { get; }
        public float Beta { get; }

        /// <summary>The plane's turn from the bowsprit's forward toward port (radians).</summary>
        public float Yaw => Mathf.Atan2(Forward.y, Forward.x);

        public Vector3 StayDir => -Forward * Mathf.Cos(Alpha) + new Vector3(0f, 0f, Mathf.Sin(Alpha));
        public Vector3 FootDir => -Forward * Mathf.Cos(Beta) + new Vector3(0f, 0f, Mathf.Sin(Beta));
        public Vector3 Head => Tack + StayDir * Luff;
        public Vector3 Clew => Tack + FootDir * Foot;
        public float Area => 0.5f * Foot * Luff * Mathf.Sin(Alpha - Beta);
    }

    /// <summary>
    /// A bowsprit's jibs cut to the bowsprit as it is and the stay found for it (<see cref="KwJibStay"/>), so a longer
    /// bowsprit sets longer, bigger jibs and a shorter one smaller jibs, whatever the foremast's height. One jib sets on the
    /// stay from the foremast to the bowsprit's tip: its luff up the stay twice the bowsprit's length, its foot rising aft
    /// 10° (less on a low stay) 1.1 times it. A second jib sets inside it, on an inner stay from four fifths of the outer
    /// one's height on the mast down to the bowsprit a third of the way out, its luff 1.7 and its foot 1 times the
    /// bowsprit's length; the outer jib's foot then stops short of the inner stay, so the two never overlap. Each jib is
    /// held to its room: its head at most nine tenths of the way up its stay, and its head and clew, so its whole after
    /// edge, half a metre forward of all the foremast carries forward of its axis (<see cref="Reach"/>: its square sails'
    /// yards and full bellies, a crow's nest, a fighting top before it), so it never crosses the mast's sails. A jib with no room (a stay under a
    /// metre high or long, or rising under 15°, a foot under 0.6 m) is left out.
    /// </summary>
    public static class KwJibPlan
    {
        private const float OuterLuff = 2f, OuterFoot = 1.1f, InnerLuff = 1.7f, InnerFoot = 1f;
        private const float Highest = 0.9f, Margin = 0.5f, Clear = 0.9f, InnerOut = 0.35f, InnerHead = 0.8f;
        private static readonly float Rise = 10f * Mathf.Deg2Rad, Least = 15f * Mathf.Deg2Rad;

        public static List<KwJibCut> Of(KwBowsprit spec, KwJibStay stay)
        {
            List<KwJibCut> cuts = new List<KwJibCut>();
            if (spec.Jibs <= 0)
                return cuts;
            float length = spec.Length;
            Vector3 outer = spec.Top(length - 0.15f), inner = spec.Top(InnerOut * length);
            Vector3 innerTop = stay.Head - new Vector3(0f, 0f, (1f - InnerHead) * (stay.Head.z - inner.z));
            (Vector3, Vector3)? within = spec.Jibs > 1 ? (inner, innerTop) : ((Vector3, Vector3)?)null;
            KwJibCut? first = Cut(outer, stay.Head, (OuterLuff * length, OuterFoot * length), stay, within);
            if (first != null)
                cuts.Add(first);
            KwJibCut? second = spec.Jibs > 1 ? Cut(inner, innerTop, (InnerLuff * length, InnerFoot * length), stay, null) : null;
            if (second != null)
                cuts.Add(second);
            return cuts;
        }

        /// <summary>The jibs' sail area together; none with no stay.</summary>
        public static float Area(KwBowsprit spec, KwJibStay? stay)
        {
            float area = 0f;
            foreach (KwJibCut cut in stay != null ? Of(spec, stay) : new List<KwJibCut>())
                area += cut.Area;
            return area;
        }

        /// <summary>A blueprint's bowsprit's jibs' power: their area times their cloth's drive (<see cref="KwBpBow.Pull"/>).</summary>
        public static float Power(KwBlueprint blueprint, KwBpPiece piece) =>
            KwBowsprit.TryParse(KwShipParts.Saved(piece), out KwBowsprit spec) ? Area(spec, KwJibStay.InBlueprint(blueprint, piece)) * KwSailCloth.Drive(spec.Cloth) : 0f;

        // How much cloth a jib's texture spans: as the foremast's lowest sail's does (the mean of its width and height).
        private static float Cloth(KwMastSpec mast) => mast.Sails.Count > 0 ? (mast.Sails[0].Width + mast.Sails[0].Height) / 2f : 0f;

        // A jib on the stay from its tack up to its top, its luff and foot as the bowsprit asks (want) within its room: its
        // head under the stay's top, its head and clew clear of the foremast's sails, its foot of an inner stay; null with
        // no room for it.
        private static KwJibCut? Cut(Vector3 tack, Vector3 top, (float Luff, float Foot) want, KwJibStay stay, (Vector3 Foot, Vector3 Top)? inner)
        {
            Vector3 run = top - tack;
            float d = new Vector2(run.x, run.y).magnitude, h = run.z;
            if (d < 1f || h < 1f || Mathf.Atan2(h, d) < Least)
                return null;
            Vector3 forward = new Vector3(-run.x, -run.y, 0f) / d;
            float alpha = Mathf.Atan2(h, d), beta = Mathf.Max(0f, Mathf.Min(Rise, alpha - Least));
            float room = Vector3.Dot(tack - stay.Foot, forward) - Reach(stay.Mast) - Margin;
            float foot = Mathf.Min(want.Foot, room / Mathf.Cos(beta));
            if (inner != null)
                foot = Mathf.Min(foot, Clear * Crossing(tack, forward, beta, inner.Value));
            float luff = Mathf.Min(want.Luff, Mathf.Min(Highest * run.magnitude, room / Mathf.Cos(alpha)));
            return foot < 0.6f || luff < 0.8f ? null : new KwJibCut(tack, top, forward, luff, foot, alpha, beta, Cloth(stay.Mast));
        }

        /// <summary>
        /// How far forward of a mast's axis what it carries stands: its square sails' yards and their cloth bellied full, a
        /// crow's nest, a fighting top on its fore side; its own radius at least.
        /// </summary>
        public static float Reach(KwMastSpec mast)
        {
            float reach = KwSailLayout.RadiusOf(mast.Mast);
            foreach (KwSailLayout sail in KwSailLayout.Of(mast))
            {
                if (sail.Square)
                    reach = Mathf.Max(reach, sail.YardX + sail.BellyAt(sail.Height));
            }
            if (mast.Tops.Nest)
                reach = Mathf.Max(reach, KwMastTops.NestRadius(mast.Mast));
            if (mast.Tops.HasTop && KwMastTops.TopWay(mast) > 0f)
                reach = Mathf.Max(reach, KwMastTops.TopHalf(mast.Mast));
            return reach;
        }

        // How far along a foot line (from the tack, aft and rising by beta, in the plane of forward and up) an inner stay
        // crosses it; endless when it does not.
        private static float Crossing(Vector3 tack, Vector3 forward, float beta, (Vector3 Foot, Vector3 Top) stay)
        {
            Vector2 a = Plane(stay.Foot - tack, forward), e = Plane(stay.Top - tack, forward) - a;
            Vector2 f = new Vector2(-Mathf.Cos(beta), Mathf.Sin(beta));
            float den = -f.x * e.y + f.y * e.x;
            if (Mathf.Abs(den) < 1e-5f)
                return float.PositiveInfinity;
            float t = (-a.x * e.y + a.y * e.x) / den, s = (f.x * a.y - f.y * a.x) / den;
            return t > 0f && s > -0.05f && s < 1.05f ? t : float.PositiveInfinity;
        }

        private static Vector2 Plane(Vector3 v, Vector3 forward) => new Vector2(Vector3.Dot(v, forward), v.z);
    }
}
