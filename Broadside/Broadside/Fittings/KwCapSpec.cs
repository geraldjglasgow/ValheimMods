using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A post cap the Post cap tool built (<see cref="KwCapTool"/>), saved in its piece's ZDO (<see cref="KwGenerated.Key"/>)
    /// as "cap|style|width|depth|length|lean|facing": its style's id (<see cref="KwCaps.Styles"/>), the base it covers, in
    /// metres across the ship and along the keel (its post's own section when built: <see cref="KwKeelBody.PostHalf"/> twice
    /// and <see cref="KwKeelBody.PostDepth"/>, times the edit window's size), how far its neck is stretched (1 as carved,
    /// <see cref="MinLength"/> to <see cref="MaxLength"/>), how far a long neck leans (degrees, toward the way the cap looks
    /// when positive, back when negative, at most <see cref="MaxLean"/>; drawn only by the styles that lean,
    /// <see cref="KwCapStyle.Leans"/>) and which way it looks: "out" along the keel past its post, or "in" board. A text
    /// saved before lean and facing ("cap|style|width|depth|length") reads as lean 0, facing out; a lean or facing that does
    /// not read is taken as those too, and anything after them is left alone. Its biome tier is its keel's, in the ZDO beside
    /// it (<see cref="KwBiomeTiers.Key"/>). An old hammer cap (<see cref="KwCaps.Prefix"/>) reads as one of its style at its
    /// post's section (<see cref="Old"/>). Kit axes: the origin on the middle of the post's top, x outward along the keel.
    /// </summary>
    public readonly struct KwCapSpec
    {
        public const string Tool = "cap", Made = KwToolPieces.PartPrefix + Tool;
        public const float MinLength = 0.5f, MaxLength = 2f, MinBase = 0.03f, MaxBase = 3f, MaxLean = 35f;
        private const string Tag = "cap", Out = "out", In = "in";

        public KwCapSpec(string style, float width, float depth, float length, float lean = 0f, bool inboard = false)
        {
            Style = KwCaps.Find(style ?? "") != null ? style! : KwCaps.Styles[0].Id;
            (Width, Depth) = (Mathf.Clamp(width, MinBase, MaxBase), Mathf.Clamp(depth, MinBase, MaxBase));
            Length = Mathf.Clamp(length, MinLength, MaxLength);
            float leans = Mathf.Clamp(lean, -MaxLean, MaxLean);
            Lean = leans == 0f ? 0f : leans;
            Inboard = inboard;
        }

        public string Style { get; }

        /// <summary>The base it covers across the ship and along the keel, in metres.</summary>
        public float Width { get; }
        public float Depth { get; }

        /// <summary>How far its neck is stretched: 1 as carved.</summary>
        public float Length { get; }

        /// <summary>How far a long neck leans, in degrees: positive toward the way the cap looks, negative back.</summary>
        public float Lean { get; }

        /// <summary>Whether it looks in over its ship rather than out along the keel.</summary>
        public bool Inboard { get; }

        /// <summary>Its style's name: "Dragon head".</summary>
        public string Name => (KwCaps.Find(Style) ?? KwCaps.Styles[0]).Name;

        /// <summary>Its size against the standard post's (<see cref="KwCapForm.Of"/>): 1 on a 0.4 by 0.3 m post.</summary>
        public float Size => KwCapForm.Of(Width, Depth);

        /// <summary>A cap of a style on a keel's post: its base the post's own section.</summary>
        public static KwCapSpec On(string style, KwKeelBody keel, float length) => new KwCapSpec(style, keel.PostHalf * 2f, keel.PostDepth, length);

        /// <summary>The same cap on another base, or stretched otherwise, its lean and facing kept.</summary>
        public KwCapSpec With(string style, float width, float depth, float length) => new KwCapSpec(style, width, depth, length, Lean, Inboard);

        public override string ToString() => string.Join("|", Tag, Style, F(Width), F(Depth), F(Length), F(Lean), Inboard ? In : Out);

        /// <summary>A cap from its saved text; false when it does not read or its base or length is past the limits.</summary>
        public static bool TryParse(string text, out KwCapSpec spec)
        {
            spec = default;
            string[] p = (text ?? "").Split('|');
            if (p.Length < 5 || p[0] != Tag || KwCaps.Find(p[1]) == null || !Num(p[2], out float width) || !Num(p[3], out float depth) || !Num(p[4], out float length))
                return false;
            float lean = p.Length > 5 && Num(p[5], out float read) ? read : 0f;
            spec = new KwCapSpec(p[1], width, depth, length, lean, p.Length > 6 && p[6] == In);
            return Within(width, MinBase, MaxBase) && Within(depth, MinBase, MaxBase) && Within(length, MinLength, MaxLength);
        }

        /// <summary>An old hammer cap's style from its prefab ("KW_cap_dragon"); false for any other prefab.</summary>
        public static bool Old(string prefab, out string style)
        {
            style = prefab.StartsWith(KwCaps.Prefix, System.StringComparison.Ordinal) ? prefab.Substring(KwCaps.Prefix.Length) : "";
            return KwCaps.Find(style) != null;
        }

        /// <summary>The cap's part drawn by the code (<see cref="KwCaps.Draw"/>; a carved head where its model is loaded is <see cref="KwCapMesh"/>'s).</summary>
        public KwDef Def()
        {
            KwCapSpec spec = this;
            return new KwDef(Made, "Post cap", Tool) { Wood = true, Shape = b => KwCaps.Draw(b, spec) };
        }

        /// <summary>What it costs built from the hammer: one wood on the standard post at length 1, more by its base's area and its length.</summary>
        public List<(string Item, int Amount)> Cost() =>
            new List<(string Item, int Amount)> { ("Wood", Mathf.Max(1, Mathf.RoundToInt(Size * Size * Length))) };

        /// <summary>The cap with every size times s, for a ship scaled as a whole; null and why past the limits.</summary>
        public string? Scaled(float s, out string? error)
        {
            float width = Width * s, depth = Depth * s;
            if (!KwBpPartText.Within(width, MinBase, MaxBase, "a post cap would be {0} m wide; post caps are {1} to {2} m", out error)
                || !KwBpPartText.Within(depth, MinBase, MaxBase, "a post cap would be {0} m deep; post caps are {1} to {2} m", out error))
                return null;
            return With(Style, width, depth, Length).ToString();
        }

        /// <summary>What the player reads of it: "Dragon head, length 1.0×, leaning 10° forward, facing inboard".</summary>
        public string Words => $"{Name}, length {Length.ToString("0.0", CultureInfo.InvariantCulture)}×{Leaning}{(Inboard ? ", facing inboard" : "")}";

        /// <summary>", leaning 10° forward" for a style that leans and does; empty otherwise.</summary>
        public string Leaning => (KwCaps.Find(Style)?.Leans ?? false) && Mathf.Abs(Lean) >= 0.5f
            ? $", leaning {Mathf.Abs(Lean).ToString("0", CultureInfo.InvariantCulture)}° {(Lean > 0f ? "forward" : "back")}"
            : "";

        private static bool Within(float value, float least, float most) => value >= least - 0.005f && value <= most + 0.005f;

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool Num(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
