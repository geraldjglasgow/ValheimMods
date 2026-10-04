using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Capture
{
    /// <summary>A tiny built-in font five pixels high (digits, '.', '#', 's', '-' and the letters of GAME), drawn scaled.</summary>
    internal static class PixelFont
    {
        internal const int Height = 5;

        // Rows top to bottom, separated by '/'; '1' is ink.
        private static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['0'] = "111/101/101/101/111",
            ['1'] = "010/110/010/010/111",
            ['2'] = "111/001/111/100/111",
            ['3'] = "111/001/111/001/111",
            ['4'] = "101/101/111/001/001",
            ['5'] = "111/100/111/001/111",
            ['6'] = "111/100/111/101/111",
            ['7'] = "111/001/001/010/010",
            ['8'] = "111/101/111/101/111",
            ['9'] = "111/101/111/001/111",
            ['.'] = "0/0/0/0/1",
            ['-'] = "000/000/111/000/000",
            ['#'] = "01010/11111/01010/11111/01010",
            ['s'] = "000/111/110/011/111",
            ['G'] = "111/100/101/101/111",
            ['A'] = "111/101/111/101/101",
            ['M'] = "10001/11011/10101/10001/10001",
            ['E'] = "111/100/111/100/111",
            [' '] = "00/00/00/00/00",
        };

        /// <summary>The text's width in font pixels, one blank column between characters.</summary>
        internal static int Width(string text)
        {
            int width = 0;
            foreach (char c in text) width += Glyph(c)[0].Length + 1;
            return Mathf.Max(0, width - 1);
        }

        /// <summary>Draws the text with its top-left corner at (x, top), each font pixel scale by scale; unknown characters are blank.</summary>
        internal static void Draw(Canvas canvas, int x, int top, string text, int scale, Color32 ink)
        {
            foreach (char c in text)
            {
                string[] rows = Glyph(c);
                for (int row = 0; row < Height; row++)
                    for (int column = 0; column < rows[row].Length; column++)
                        if (rows[row][column] == '1') canvas.Fill(x + column * scale, top + row * scale, scale, scale, ink);
                x += (rows[0].Length + 1) * scale;
            }
        }

        private static string[] Glyph(char c) => (Glyphs.TryGetValue(c, out string glyph) ? glyph : Glyphs[' ']).Split('/');
    }
}
