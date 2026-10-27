using System;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A railing's own segment at the end of its saved span (<see cref="KwSpan.Rail"/>): "posts=0.9", how far apart its
    /// posts stand along the ship in metres, from <see cref="Least"/> to <see cref="Most"/>, evenly from one end to the
    /// other (<see cref="KwRailing"/>), and "sides=l" or "sides=r" for a railing on one side of the hull only (the user's
    /// request, 2026-10-25: a cabin drawn in on one side keeps the railing there): the part's own left or right, looking
    /// along it from its start rib (its kit +y or -y), which is the ship's port or starboard as the part runs to the bow or
    /// back; a part's text knows nothing of the bow, so the windows turn port and starboard into these letters
    /// (<see cref="KwRailBow"/>). "posts=0.9|sides=l" has both, "sides=r" only the side (posts at the stations). A
    /// railing saved without them, or with one that does not read, keeps its posts as railings were first built (at its
    /// stations, about a metre apart) and both sides. Pure.
    /// </summary>
    public static class KwRailText
    {
        /// <summary>The closest and furthest posts may stand, and the railing window's first spacing (about a metre, as before).</summary>
        public const float Least = 0.4f, Most = 3f, Usual = 1f;

        /// <summary>Which sides a railing stands on, as letters: both (as every railing before), its left only, its right only.</summary>
        public const string Both = "lr", Left = "l", Right = "r";

        private const string Tag = "posts=", SidesTag = "sides=";

        /// <summary>A saved text without its railing segment, and the segment ("" for none).</summary>
        public static string Strip(string text, out string rail)
        {
            rail = "";
            string all = text ?? "";
            int posts = all.LastIndexOf("|" + Tag, StringComparison.Ordinal), sides = all.LastIndexOf("|" + SidesTag, StringComparison.Ordinal);
            int at = posts < 0 ? sides : sides < 0 ? posts : Mathf.Min(posts, sides);
            if (at < 0)
                return all;
            rail = all.Substring(at + 1);
            return all.Substring(0, at);
        }

        /// <summary>A text with a railing segment after it (none for "").</summary>
        public static string Join(string text, string rail) => string.IsNullOrEmpty(rail) ? text : text + "|" + rail;

        /// <summary>The segment for a spacing, kept within the limits.</summary>
        public static string Write(float spacing) => Tag + Mathf.Clamp(spacing, Least, Most).ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>The segment for a spacing (0: posts at the stations) and sides (both written as nothing); "" for neither.</summary>
        public static string Write(float spacing, string sides)
        {
            string posts = spacing > 0f ? Write(spacing) : "", side = Normal(sides) == Both ? "" : SidesTag + Normal(sides);
            return posts.Length > 0 && side.Length > 0 ? posts + "|" + side : posts + side;
        }

        /// <summary>A segment's spacing; 0 for none, or one that does not read (posts at the stations, as before).</summary>
        public static float Spacing(string rail)
        {
            string value = Field(rail, Tag);
            bool read = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float spacing);
            return value.Length > 0 && read && spacing >= Least - 0.005f && spacing <= Most + 0.005f ? spacing : 0f;
        }

        /// <summary>A segment's sides: <see cref="Left"/>, <see cref="Right"/>, or <see cref="Both"/> for none or one that does not read.</summary>
        public static string Sides(string rail)
        {
            string value = Field(rail, SidesTag);
            return value == Left || value == Right ? value : Both;
        }

        /// <summary>Letters as one of the three: both letters, or none, is both sides.</summary>
        public static string Normal(string sides)
        {
            bool left = (sides ?? "").IndexOf('l') >= 0, right = (sides ?? "").IndexOf('r') >= 0;
            return left == right ? Both : left ? Left : Right;
        }

        /// <summary>Whether a side (+1 the part's left, its kit +y; -1 its right) stands in these letters.</summary>
        public static bool Has(string sides, float side) => Normal(sides).IndexOf(side > 0f ? 'l' : 'r') >= 0;

        /// <summary>How much of a both-sided railing's material a segment's railing takes: a half for one side.</summary>
        public static float Share(string rail) => Sides(rail) == Both ? 1f : 0.5f;

        /// <summary>A railing's span text with its posts this far apart (0: at its stations, as before), its sides kept.</summary>
        public static string With(string saved, float spacing)
        {
            string plain = Strip(saved, out string rail);
            return Join(plain, Write(spacing, Sides(rail)));
        }

        /// <summary>A railing's span text with its posts this far apart (0: at its stations) on these sides.</summary>
        public static string With(string saved, float spacing, string sides) => Join(Strip(saved, out _), Write(spacing, sides));

        // One field's value in a segment ("" for none): "posts=0.9|sides=l" has "0.9" for posts= and "l" for sides=.
        private static string Field(string rail, string tag)
        {
            foreach (string part in (rail ?? "").Split('|'))
            {
                if (part.StartsWith(tag, StringComparison.Ordinal))
                    return part.Substring(tag.Length);
            }
            return "";
        }
    }
}
