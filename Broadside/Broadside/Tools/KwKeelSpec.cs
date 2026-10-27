using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a made keel climbs to its far end: the quick curve (for straight-sided ships), the oval's late sweep (the
    /// game's own ships'), or a flat stern's, which ends square at a transom with its post just behind it.
    /// </summary>
    public enum KwKeelCurve
    {
        Quick,
        Oval,
        Flat,
    }

    /// <summary>
    /// A keel the keel tool made, saved in its ZDO as "keel|length|rise|curve|post|post height|width|depth": its length, how
    /// high its far end climbs above its start (0 for a straight keel), the curve it climbs by (quick "q", oval "o" or a
    /// flat stern "f", <see cref="KwKeelCurve"/>), whether a post stands at its far end (always on a flat stern), how far
    /// that post rises over the keel there, and its own thickness (the standard 0.3 m by 0.35 m when not saved). Its start
    /// (x = 0) is the end the player clicked; it runs and climbs away from it.
    /// </summary>
    public readonly struct KwKeelSpec
    {
        public const float MinLength = 1f, MaxLength = 50f, MaxRise = 8f, MinPost = 1f, MaxPost = 12f;
        private const string Tag = "keel";

        public KwKeelSpec(float length, float rise, KwKeelCurve curve, bool post, float postAbove, KwKeelBody body)
        {
            Length = length;
            Rise = rise;
            Curve = curve;
            Post = post || curve == KwKeelCurve.Flat;
            PostAbove = postAbove;
            Body = body;
        }

        public KwKeelBody Body { get; }

        public float Length { get; }
        public float Rise { get; }
        public KwKeelCurve Curve { get; }
        public bool Post { get; }
        public float PostAbove { get; }

        /// <summary>Whether it ends in a flat stern: square at its far end, its post just past it.</summary>
        public bool Flat => Curve == KwKeelCurve.Flat;

        /// <summary>The keel line's height at x along it.</summary>
        public float RiseAt(float x) => Rise * KwKeel.Curve(Curve, Mathf.Clamp01(x / Length));

        public override string ToString() =>
            string.Join("|", Tag, F(Length), F(Rise), Letter(Curve), Post ? "1" : "0", F(PostAbove), F(Body.Half * 2f), F(Body.Depth));

        private static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static string Letter(KwKeelCurve curve) => curve == KwKeelCurve.Oval ? "o" : curve == KwKeelCurve.Flat ? "f" : "q";

        private static KwKeelCurve Of(string letter) => letter == "o" ? KwKeelCurve.Oval : letter == "f" ? KwKeelCurve.Flat : KwKeelCurve.Quick;

        public static bool TryParse(string text, out KwKeelSpec spec)
        {
            spec = default;
            string[] p = (text ?? "").Split('|');
            if ((p.Length != 6 && p.Length != 8) || p[0] != Tag || !Num(p[1], out float length) || !Num(p[2], out float rise) || !Num(p[5], out float post))
                return false;
            KwKeelBody body = KwKeel.Standard;
            if (p.Length == 8 && (!Num(p[6], out float width) || !Num(p[7], out float depth) || !Thickness(width, depth, out body)))
                return false;
            spec = new KwKeelSpec(length, rise, Of(p[3]), p[4] == "1", post, body);
            return length >= MinLength - 0.01f && length <= MaxLength + 0.01f && rise >= 0f && rise <= MaxRise + 0.01f
                && post >= MinPost - 0.01f && post <= MaxPost + 0.01f;
        }

        private static bool Thickness(float width, float depth, out KwKeelBody body)
        {
            body = new KwKeelBody(width / 2f, depth);
            return width >= KwKeelBody.MinWidth - 0.01f && width <= KwKeelBody.MaxWidth + 0.01f
                && depth >= KwKeelBody.MinDepth - 0.01f && depth <= KwKeelBody.MaxDepth + 0.01f;
        }

        private static bool Num(string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
