using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One painted band on planking: whole strakes From to To (0 the strake whose foot is level with the keel line on the
    /// sides, as <see cref="KwGirth"/> counts them) in one colour of <see cref="KwPaint"/>.
    /// </summary>
    public readonly struct KwBand
    {
        public KwBand(int from, int to, int colour)
        {
            From = Mathf.Max(0, Mathf.Min(from, to));
            To = Mathf.Max(from, to);
            Colour = colour;
        }

        public int From { get; }
        public int To { get; }
        public int Colour { get; }
    }

    /// <summary>
    /// Painted bands along the hull: the palette (six muted colours the game's own paint could be: red, blue, black, white,
    /// yellow, green) and how a planking part saves its bands, a last segment of its saved text after the span
    /// (<see cref="KwSpan"/>): "paint=3-3:red,6-7:blue", each band its strakes counted from 1 at the keel line and its colour. A
    /// text without the segment is unpainted, so every text saved before bands reads as it did; a segment that does not
    /// read paints nothing. At most <see cref="MaxBands"/> bands, each at most <see cref="MaxWide"/> strakes wide.
    /// </summary>
    public static class KwPaint
    {
        public const int MaxBands = 2, MaxWide = 4;
        private const string Tag = "paint=";

        /// <summary>Each colour's saved name, its name in the window and its paint (sRGB), in the window's order.</summary>
        public static readonly string[] Names = { "red", "blue", "black", "white", "yellow", "green" };
        public static readonly string[] Labels = { "Red", "Blue", "Black", "White", "Yellow", "Green" };
        public static readonly Color[] Colours =
        {
            new Color(0.5f, 0.15f, 0.11f), new Color(0.17f, 0.26f, 0.38f), new Color(0.1f, 0.09f, 0.08f),
            new Color(0.8f, 0.76f, 0.66f), new Color(0.76f, 0.58f, 0.22f), new Color(0.23f, 0.35f, 0.2f),
        };

        /// <summary>A saved text without its paint segment, and the segment ("" for none).</summary>
        public static string Strip(string text, out string paint)
        {
            paint = "";
            int at = (text ?? "").LastIndexOf("|" + Tag, StringComparison.Ordinal);
            if (at < 0)
                return text ?? "";
            paint = text!.Substring(at + 1);
            return text.Substring(0, at);
        }

        /// <summary>A span's text with a paint segment after it (none for "").</summary>
        public static string Join(string text, string paint) => string.IsNullOrEmpty(paint) ? text : text + "|" + paint;

        /// <summary>The paint segment for some bands; "" for none.</summary>
        public static string Write(IEnumerable<KwBand> bands)
        {
            List<string> parts = new List<string>();
            foreach (KwBand band in bands)
            {
                if (parts.Count < MaxBands && band.Colour >= 0 && band.Colour < Names.Length)
                    parts.Add($"{band.From + 1}-{Mathf.Min(band.To, band.From + MaxWide - 1) + 1}:{Names[band.Colour]}");
            }
            return parts.Count == 0 ? "" : Tag + string.Join(",", parts);
        }

        /// <summary>The bands a paint segment holds (any it cannot read left out); none for "".</summary>
        public static List<KwBand> Read(string paint)
        {
            List<KwBand> bands = new List<KwBand>();
            if (string.IsNullOrEmpty(paint) || !paint.StartsWith(Tag, StringComparison.Ordinal))
                return bands;
            foreach (string part in paint.Substring(Tag.Length).Split(','))
            {
                if (bands.Count < MaxBands && Band(part, out KwBand band))
                    bands.Add(band);
            }
            return bands;
        }

        // "3-4:red": strakes 3 to 4 counted from 1, in red.
        private static bool Band(string text, out KwBand band)
        {
            band = default;
            string[] p = text.Split(':');
            string[] range = p[0].Split('-');
            int colour = p.Length == 2 ? Array.IndexOf(Names, p[1]) : -1;
            if (colour < 0 || range.Length != 2 || !Whole(range[0], out int from) || !Whole(range[1], out int to) || from < 1 || to < from)
                return false;
            band = new KwBand(from - 1, Mathf.Min(to, from + MaxWide - 1) - 1, colour);
            return true;
        }

        private static bool Whole(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
