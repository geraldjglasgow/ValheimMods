using System;
using System.Collections.Generic;
using System.Globalization;

namespace Broadside
{
    /// <summary>
    /// How a part saves its openings (<see cref="KwOpening"/>): a last segment of its saved text,
    /// "open=1,p,square,2.4,1.1,0.6,0.6,plain,down;2,...", each opening its number, wall, shape, place along, sill, width,
    /// height, frame and cover. A planking part's span (<see cref="KwSpan"/>) and a cabin's text (<see cref="KwCabinSpec"/>)
    /// strip it before reading the rest, so every text saved before openings reads as it did, with none. An entry that does
    /// not read is left out. Pure.
    /// </summary>
    public static class KwOpeningText
    {
        private const string Tag = "open=";
        private static readonly CultureInfo Plain = CultureInfo.InvariantCulture;

        /// <summary>A saved text without its openings segment, and the segment ("" for none).</summary>
        public static string Strip(string text, out string openings)
        {
            openings = "";
            int at = (text ?? "").LastIndexOf("|" + Tag, StringComparison.Ordinal);
            if (at < 0)
                return text ?? "";
            int end = text!.IndexOf('|', at + 1);
            openings = end < 0 ? text.Substring(at + 1) : text.Substring(at + 1, end - at - 1);
            return end < 0 ? text.Substring(0, at) : text.Substring(0, at) + text.Substring(end);
        }

        /// <summary>A text with an openings segment after it (none for "").</summary>
        public static string Join(string text, string openings) => string.IsNullOrEmpty(openings) ? text : text + "|" + openings;

        /// <summary>The segment for some openings; "" for none.</summary>
        public static string Write(IReadOnlyList<KwOpening> openings)
        {
            if (openings.Count == 0)
                return "";
            List<string> entries = new List<string>();
            foreach (KwOpening o in openings)
                entries.Add(string.Join(",", o.Id.ToString(Plain), o.Wall, o.Shape, F(o.U), F(o.Sill), F(o.Width), F(o.Height), o.Frame, o.Cover));
            return Tag + string.Join(";", entries);
        }

        /// <summary>A segment's openings; none for "" or one that is not an openings segment.</summary>
        public static List<KwOpening> Read(string segment)
        {
            List<KwOpening> openings = new List<KwOpening>();
            if (string.IsNullOrEmpty(segment) || !segment.StartsWith(Tag, StringComparison.Ordinal))
                return openings;
            foreach (string entry in segment.Substring(Tag.Length).Split(';'))
            {
                KwOpening? opening = Entry(entry);
                if (opening != null)
                    openings.Add(opening);
            }
            return openings;
        }

        /// <summary>A whole saved text's openings (its segment found and read).</summary>
        public static List<KwOpening> Of(string saved)
        {
            Strip(saved, out string segment);
            return Read(segment);
        }

        /// <summary>A whole saved text with its openings put in place of the ones it had.</summary>
        public static string With(string saved, IReadOnlyList<KwOpening> openings) => Join(Strip(saved, out _), Write(openings));

        // "id,wall,shape,u,sill,width,height,frame,cover".
        private static KwOpening? Entry(string entry)
        {
            string[] f = entry.Split(',');
            if (f.Length != 9 || !int.TryParse(f[0], NumberStyles.Integer, Plain, out int id) || !Num(f[3], out float u) || !Num(f[4], out float sill)
                || !Num(f[5], out float width) || !Num(f[6], out float height) || width <= 0.01f || height <= 0.01f)
                return null;
            if (!KwOpeningShape.Known(f[2]) || !Known(f[7], KwOpening.Plain, KwOpening.Heavy, KwOpening.Iron) || !KwOpeningShape.Covers(f[2], f[8]))
                return null;
            return new KwOpening(id, f[1], f[2], u, sill, width, height, f[7], f[8]);
        }

        private static bool Known(string value, params string[] names) => Array.IndexOf(names, value) >= 0;

        private static string F(float value) => value.ToString("0.###", Plain);

        private static bool Num(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, Plain, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
