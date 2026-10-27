using System;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What is painted on a sail's cloth (<see cref="KwSailLooks"/>): nothing (plain), stripes up the sail or across it
    /// (how many stripes of the colour, the cloth showing between them and at both edges), quarters (the upper quarter at
    /// the mast's side and the lower one away from it in the colour), a border round its edges, or an emblem in its middle
    /// on a round field of the colour (<see cref="KwSailEmblems"/>: one of the game's banners, or the player's own image).
    /// Each pattern takes one colour from a small muted palette (<see cref="KwSailDye"/>). Saved after a sail's cloth in its
    /// mast's text, and after a bowsprit's jib cloth, as "kind:colour" with ":stripes" for stripes and ":emblem" for an
    /// emblem ("vstripes:red:3", "border:black", "emblem:white:piece_banner02", "emblem:red:img/raven.png"); a plain sail
    /// writes nothing, so its text is as it was before patterns, and a text this does not know reads as plain.
    /// </summary>
    public readonly struct KwSailPattern : IEquatable<KwSailPattern>
    {
        public const int Plain = 0, Upright = 1, Across = 2, Quarters = 3, Border = 4, Emblem = 5;
        public const int MinStripes = 1, MaxStripes = 6, Stripes = 3;
        public static readonly string[] Kinds = { "plain", "vstripes", "hstripes", "quarters", "border", "emblem" };
        public static readonly string[] Labels = { "Plain", "Vertical stripes", "Horizontal stripes", "Quarters", "Border", "Emblem" };

        public KwSailPattern(int kind, int colour, int stripes, string emblem)
        {
            Kind = Mathf.Clamp(kind, 0, Kinds.Length - 1);
            Colour = Mathf.Clamp(colour, 0, KwSailDye.Count - 1);
            count = Mathf.Clamp(stripes <= 0 ? Stripes : stripes, MinStripes, MaxStripes);
            art = Safe(emblem) ? emblem : "";
        }

        private readonly int count;
        private readonly string? art;

        public int Kind { get; }

        /// <summary>The colour's place in the palette (<see cref="KwSailDye"/>).</summary>
        public int Colour { get; }

        /// <summary>How many stripes of the colour a striped sail has.</summary>
        public int Count => count > 0 ? count : Stripes;

        /// <summary>The emblem's id (<see cref="KwSailEmblems"/>); empty when none is picked.</summary>
        public string Art => art ?? "";

        public bool IsPlain => Kind == Plain;
        public bool Striped => Kind == Upright || Kind == Across;
        public bool HasEmblem => Kind == Emblem;

        /// <summary>Whether the painting depends on the sail's proportions (a border's even width, an emblem's round field).</summary>
        public bool Shaped => Kind == Border || Kind == Emblem;

        /// <summary>The same pattern in another kind, its colour, stripes and emblem kept for when they are wanted again.</summary>
        public KwSailPattern As(int kind) => new KwSailPattern(kind, Colour, Count, Art);

        /// <summary>The pattern as its sail's text writes it; empty for plain.</summary>
        public override string ToString()
        {
            if (IsPlain)
                return "";
            string head = Kinds[Kind] + ":" + KwSailDye.Code(Colour);
            return Striped ? head + ":" + Count.ToString(CultureInfo.InvariantCulture) : HasEmblem ? head + ":" + Art : head;
        }

        /// <summary>A pattern from its saved text; plain for an empty text or one this does not know.</summary>
        public static KwSailPattern Read(string? text)
        {
            string[] p = (text ?? "").Split(':');
            int kind = Array.IndexOf(Kinds, p[0]);
            if (kind <= 0 || p.Length < 2)
                return default;
            int stripes = kind == Upright || kind == Across ? Number(p.Length > 2 ? p[2] : "") : Stripes;
            string emblem = kind == Emblem && p.Length > 2 ? string.Join(":", p, 2, p.Length - 2) : "";
            return new KwSailPattern(kind, KwSailDye.Index(p[1]), stripes, emblem);
        }

        /// <summary>
        /// Whether an emblem's id can stand in a sail's text: no separator of the texts (a comma, a bar, a colon), no quote,
        /// backslash or control character, at most 80 characters.
        /// </summary>
        public static bool Safe(string? id)
        {
            if (string.IsNullOrEmpty(id) || id!.Length > 80)
                return false;
            foreach (char c in id)
            {
                if (c < ' ' || ",|:;\"\\".IndexOf(c) >= 0)
                    return false;
            }
            return true;
        }

        private static int Number(string text) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : Stripes;

        public bool Equals(KwSailPattern other) => ToString() == other.ToString();

        public override bool Equals(object? obj) => obj is KwSailPattern other && Equals(other);

        public override int GetHashCode() => ToString().GetHashCode();
    }

    /// <summary>
    /// The sail patterns' colours: a small palette, muted as the game's dyed cloth is (its banners, its sails), saved by code;
    /// a code this does not know reads as the first.
    /// </summary>
    public static class KwSailDye
    {
        private static readonly (string Code, string Label, Color32 Colour)[] Dyes =
        {
            ("red", "Red", new Color32(140, 42, 32, 255)),
            ("blue", "Blue", new Color32(46, 68, 118, 255)),
            ("green", "Green", new Color32(60, 94, 50, 255)),
            ("yellow", "Yellow", new Color32(196, 156, 62, 255)),
            ("black", "Black", new Color32(32, 29, 26, 255)),
            ("white", "White", new Color32(236, 228, 212, 255)),
            ("brown", "Brown", new Color32(98, 64, 38, 255)),
        };

        public static int Count => Dyes.Length;

        public static string Code(int index) => Dyes[Mathf.Clamp(index, 0, Dyes.Length - 1)].Code;

        public static Color32 Colour(int index) => Dyes[Mathf.Clamp(index, 0, Dyes.Length - 1)].Colour;

        public static string[] Labels => Array.ConvertAll(Dyes, d => d.Label);

        public static int Index(string code) => Math.Max(0, Array.FindIndex(Dyes, d => d.Code == code));
    }
}
