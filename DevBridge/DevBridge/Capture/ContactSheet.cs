using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DevBridge.Capture
{
    /// <summary>Lays a burst's cells out on one image in a grid, left to right and top to bottom, each labelled.</summary>
    internal static class ContactSheet
    {
        private const int Gap = 4;
        private static readonly Color32 Background = new Color32(24, 24, 24, 255);
        private static readonly Color32 Ink = new Color32(255, 255, 255, 255);

        /// <summary>The number of columns that makes the sheet closest to square for cells of this shape.</summary>
        internal static int Columns(int count, int cellWidth, int cellHeight) =>
            Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(count * (float)cellHeight / cellWidth)), 1, count);

        internal static Vector2Int Size(int columns, int rows, Vector2Int cell) =>
            new Vector2Int(columns * (cell.x + Gap) + Gap, rows * (cell.y + Gap) + Gap);

        /// <summary>The sheet, with every cell the size of the first; labels show game time when gameTime is set.</summary>
        internal static Canvas Compose(List<Shot> shots, int columns, bool gameTime)
        {
            var cell = new Vector2Int(shots[0].Image.Width, shots[0].Image.Height);
            Vector2Int size = Size(columns, (shots.Count + columns - 1) / columns, cell);
            var sheet = new Canvas(size.x, size.y, Background);
            int scale = Mathf.Clamp(Mathf.RoundToInt(cell.x / 150f), 1, 5);
            foreach (Shot shot in shots)
            {
                int x = Gap + shot.Index % columns * (cell.x + Gap), top = Gap + shot.Index / columns * (cell.y + Gap);
                sheet.Paste(shot.Image, x, top, cell.x, cell.y);
                Label(sheet, x, top, Caption(shot, gameTime), scale);
            }
            return sheet;
        }

        /// <summary>"#3 0.36s", or "#3 0.09s GAME" when the time is game time.</summary>
        private static string Caption(Shot shot, bool gameTime) =>
            $"#{shot.Index} " + (gameTime ? shot.Game : shot.Real).ToString("0.00", CultureInfo.InvariantCulture) + "s" + (gameTime ? " GAME" : "");

        private static void Label(Canvas sheet, int x, int top, string text, int scale)
        {
            sheet.Shade(x, top, (PixelFont.Width(text) + 2) * scale, (PixelFont.Height + 2) * scale);
            PixelFont.Draw(sheet, x + scale, top + scale, text, scale, Ink);
        }
    }
}
