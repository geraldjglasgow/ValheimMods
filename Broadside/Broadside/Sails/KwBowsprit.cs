using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A bowsprit (made with <see cref="KwBowspritTool"/>): a spar resting on the top of the bow's stem post and angled up
    /// and out over the bow, and the jibs set from it. Saved in its piece's ZDO (<see cref="KwGenerated.Key"/>) as
    /// "bowsprit|length|rise|jibs|cloth": its length from the post's top to its tip, its rise over level in degrees, how
    /// many jibs it sets (none, one or two) and their cloth (<see cref="KwSailCloth"/>), and "|pattern" after it when its
    /// jibs have one painted on (<see cref="KwSailPattern"/>; a text without it reads plain). Its tip, where the jib stay comes
    /// down to, is in that text (<see cref="Tip"/>); the stay's upper end, on the foremast, is found from the ship's pieces
    /// when the bowsprit is built, on the dock and on a launched ship (<see cref="KwJibStay"/>). The spar tapers to its tip,
    /// lashed down on the post by two rope rings and banded near its end. Kit axes: the origin on the middle of the post's
    /// top, x out over the bow, y across, z up.
    /// </summary>
    public readonly struct KwBowsprit
    {
        public const string Tool = "bowsprit", Made = KwToolPieces.PartPrefix + Tool;
        public const float MinLength = 1.5f, MaxLength = 10f, MaxRise = 40f, Heel = 0.35f;
        public const int MostJibs = 2;
        private const string Tag = "bowsprit";

        public KwBowsprit(float length, float rise, int jibs, string cloth, KwSailPattern pattern = default)
        {
            (Length, Rise) = (Mathf.Clamp(length, MinLength, MaxLength), Mathf.Clamp(rise, 0f, MaxRise));
            (Jibs, Cloth, Pattern) = (Mathf.Clamp(jibs, 0, MostJibs), cloth ?? "", pattern);
        }

        public float Length { get; }

        /// <summary>The rise over level, in degrees.</summary>
        public float Rise { get; }

        public int Jibs { get; }
        public string Cloth { get; }

        /// <summary>What is painted on its jibs; plain for a bowsprit saved before patterns.</summary>
        public KwSailPattern Pattern { get; }

        /// <summary>The spar's radius at its heel, from its length.</summary>
        public float Radius => Mathf.Clamp(0.06f + 0.012f * Length, 0.08f, 0.18f);

        /// <summary>The way the spar runs, out and up.</summary>
        public Vector3 Direction => new Vector3(Mathf.Cos(Rise * Mathf.Deg2Rad), 0f, Mathf.Sin(Rise * Mathf.Deg2Rad));

        /// <summary>A point on the spar's axis that far out from the post's top (it rests on the post, its axis its radius over it).</summary>
        public Vector3 Along(float out1) => new Vector3(0f, 0f, Radius) + Direction * out1;

        /// <summary>The spar's tip on its axis.</summary>
        public Vector3 Tip => Along(Length);

        /// <summary>A point on the spar's top that far out, where a stay or a sail's tack is made fast.</summary>
        public Vector3 Top(float out1) => Along(out1) + new Vector3(0f, 0f, Radius * Mathf.Lerp(1f, 0.7f, out1 / Length) + 0.02f);

        /// <summary>Another bowsprit, its jibs' pattern kept.</summary>
        public KwBowsprit With(float length, float rise, int jibs, string cloth) => new KwBowsprit(length, rise, jibs, cloth, Pattern);

        public override string ToString() =>
            string.Join("|", Tag, KwSailSpec.F(Length), KwSailSpec.F(Rise), Jibs.ToString(CultureInfo.InvariantCulture), Cloth) + (Pattern.IsPlain ? "" : "|" + Pattern);

        /// <summary>A bowsprit from its saved text; false when it does not read.</summary>
        public static bool TryParse(string text, out KwBowsprit spec)
        {
            spec = default;
            string[] p = (text ?? "").Split('|');
            if (p.Length < 5 || p.Length > 6 || p[0] != Tag || !KwSailSpec.Num(p[1], out float length) || !KwSailSpec.Num(p[2], out float rise) || !int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int jibs))
                return false;
            spec = new KwBowsprit(length, rise, jibs, p[4], KwSailPattern.Read(p.Length == 6 ? p[5] : ""));
            return length >= MinLength - 0.01f && length <= MaxLength + 0.01f;
        }

        /// <summary>The spar's part: its mesh, in the plank wood.</summary>
        public KwDef Def()
        {
            KwBowsprit spec = this;
            return new KwDef(Made, "Bowsprit", Tool) { Wood = true, Shape = b => Shape(b, spec) };
        }

        // The spar from just inboard of the post to its tip, the lashings over the post and the band near the tip.
        private static void Shape(MeshBuilder b, KwBowsprit s)
        {
            Vector3 dir = s.Direction;
            KwRound.Taper(b, s.Along(-Heel), s.Tip, s.Radius, s.Radius * 0.7f, 10);
            foreach (float at in new[] { -0.12f, 0.12f })
                KwRound.Ring(b, s.Along(at), dir, s.Radius + 0.02f, 0.022f, 10, 4);
            KwRound.Ring(b, s.Along(s.Length - 0.25f), dir, s.Radius * 0.72f + 0.012f, 0.02f, 10, 4);
        }
    }
}
