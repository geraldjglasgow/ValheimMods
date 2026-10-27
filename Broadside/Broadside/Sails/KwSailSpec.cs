using System;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One sail on a mast (<see cref="KwMastSpec"/>): its shape (a square sail on a yard across the ship, a triangle or a
    /// gaff sail along it), its width and height, the game material its cloth wears (<see cref="KwSailCloth"/>), and where
    /// the mast has it: the mast's height, how high its top is (a square sail's yard, a sail along the ship's head) and,
    /// for a square sail, its place in the stack (0 the course, the lowest, then the topsail, the topgallant, the royal);
    /// and what is painted on it (<see cref="KwSailPattern"/>). A sail is written in its mast's saved text as
    /// "shape,width,height,cloth", with ",pattern" after it when it is not plain (<see cref="Part"/>). A sail made before
    /// masts carried several was saved alone as "sail|shape|mast|width|height|cloth" (<see cref="TryParse"/>): it hangs
    /// from a yard 0.45 m under the masthead, or sets from 0.3 m under it down to a boom, and stops 0.6 m under the top.
    /// </summary>
    public readonly struct KwSailSpec
    {
        public const float MinMast = 2f, MaxMast = 20f, MinSize = 1f, MaxSize = 16f, Least = 0.5f;
        public static readonly string[] Shapes = { "square", "triangle", "gaff" };
        private const string Tag = "sail";

        /// <summary>A sail alone on its mast, as one was before masts carried several: at the masthead.</summary>
        public KwSailSpec(int shape, float mast, float width, float height, string cloth)
            : this(shape, mast, width, Mathf.Min(height, mast - 0.6f), cloth, LoneTop(shape, mast), 0)
        {
        }

        /// <summary>A sail placed on its mast: its top (a yard's height, or a head's) and its place in the square stack; plain unless a pattern is given.</summary>
        public KwSailSpec(int shape, float mast, float width, float height, string cloth, float top, int tier, KwSailPattern pattern = default)
        {
            Shape = Mathf.Clamp(shape, 0, Shapes.Length - 1);
            (Mast, Width, Height, Cloth, Top, Tier) = (mast, width, height, cloth ?? "", top, Mathf.Max(0, tier));
            Pattern = pattern;
        }

        public int Shape { get; }
        public float Mast { get; }
        public float Width { get; }
        public float Height { get; }
        public string Cloth { get; }

        /// <summary>What is painted on the cloth (<see cref="KwSailPattern"/>); plain for a sail saved before patterns.</summary>
        public KwSailPattern Pattern { get; }

        /// <summary>A square sail's yard height, or the height a sail along the ship sets up to (a gaff sail's peak).</summary>
        public float Top { get; }

        /// <summary>A square sail's place in its mast's stack: 0 the course, 1 the topsail and so on.</summary>
        public int Tier { get; }

        public bool Square => Shape == 0;

        /// <summary>The cloth's area, for the ship's drive: a triangle is half its box, a gaff sail most of it.</summary>
        public float Area => Width * Height * (Shape == 0 ? 1f : Shape == 1 ? 0.5f : 0.875f);

        /// <summary>A sail's power: its area times its cloth's drive.</summary>
        public float Power => Area * KwSailCloth.Drive(Cloth);

        /// <summary>Where the sail is fastened: the yard (square, the cloth hangs below it) or the boom (the cloth stands above it).</summary>
        public float Fastened => Square ? Top : Top - Height;

        /// <summary>The same sail, other sizes or another place (the cloth and pattern kept).</summary>
        public KwSailSpec With(int shape, float width, float height) => new KwSailSpec(shape, Mast, width, height, Cloth, Top, Tier, Pattern);

        /// <summary>The same sail placed on a mast of that height, at that top and tier, at that height.</summary>
        public KwSailSpec Placed(float mast, float height, float top, int tier) => new KwSailSpec(Shape, mast, Width, height, Cloth, top, tier, Pattern);

        /// <summary>The same sail in another cloth.</summary>
        public KwSailSpec Dressed(string cloth) => new KwSailSpec(Shape, Mast, Width, Height, cloth, Top, Tier, Pattern);

        /// <summary>The same sail with another pattern painted on.</summary>
        public KwSailSpec Painted(KwSailPattern pattern) => new KwSailSpec(Shape, Mast, Width, Height, Cloth, Top, Tier, pattern);

        // A lone sail's top: a yard 0.45 m under the masthead, or a head 0.3 m under it.
        private static float LoneTop(int shape, float mast) => shape == 0 ? mast - 0.45f : mast - 0.3f;

        /// <summary>The sail as its mast's saved text writes it: "shape,width,height,cloth", and ",pattern" when it has one.</summary>
        public string Part() => string.Join(",", Shapes[Shape], F(Width), F(Height), Cloth) + (Pattern.IsPlain ? "" : "," + Pattern);

        /// <summary>A sail from its part of a mast's saved text, not yet placed (plain when it has no pattern); false when it does not read.</summary>
        public static bool TryPart(string text, out KwSailSpec spec)
        {
            spec = default;
            string[] p = (text ?? "").Split(',');
            int shape = p.Length == 4 || p.Length == 5 ? Array.IndexOf(Shapes, p[0]) : -1;
            if (shape < 0 || !Num(p[1], out float width) || !Num(p[2], out float height) || !Sized(width) || height < Least - 0.01f)
                return false;
            KwSailPattern pattern = KwSailPattern.Read(p.Length == 5 ? p[4] : "");
            spec = new KwSailSpec(shape, MaxMast, width, Mathf.Min(height, MaxSize), p[3], 0f, 0, pattern);
            return true;
        }

        /// <summary>A lone sail saved before masts carried several: "sail|shape|mast|width|height|cloth".</summary>
        public static bool TryParse(string text, out KwSailSpec spec)
        {
            spec = default;
            string[] p = (text ?? "").Split('|');
            int shape = p.Length == 6 ? Array.IndexOf(Shapes, p[1]) : -1;
            if (p.Length != 6 || p[0] != Tag || shape < 0 || !Num(p[2], out float mast) || !Num(p[3], out float width) || !Num(p[4], out float height))
                return false;
            spec = new KwSailSpec(shape, mast, width, height, p[5]);
            return mast >= MinMast - 0.01f && mast <= MaxMast + 0.01f && Sized(width) && spec.Height >= Least;
        }

        private static bool Sized(float width) => width >= MinSize - 0.01f && width <= MaxSize + 0.01f;

        public static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public static bool Num(string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
