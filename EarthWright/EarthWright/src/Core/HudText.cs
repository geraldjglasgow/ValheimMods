using System.Collections.Generic;
using System.Linq;

namespace EarthWright.Core
{
    /// <summary>
    /// The lines of text EarthWright shows next to the crosshair while a terrain tool is in use. Any module sets its own
    /// line by key (for example "brush", "target", "cost", "ramp"); the Preview module draws them, ordered by the given
    /// order, and hides the whole block when the terrain tool is put away. Lines may contain $tokens and rich-text colours.
    /// </summary>
    public static class HudText
    {
        private sealed class Line
        {
            public int Order;
            public string Text;
        }

        private static readonly Dictionary<string, Line> lines = new Dictionary<string, Line>();

        /// <summary>Sets or replaces a line. Null or empty text removes it.</summary>
        public static void Set(string key, string text, int order = 100)
        {
            if (string.IsNullOrEmpty(text))
            {
                lines.Remove(key);
                return;
            }
            lines[key] = new Line { Order = order, Text = text };
        }

        public static void Clear(string key) => lines.Remove(key);

        /// <summary>The current lines, in order, localized.</summary>
        public static List<string> Current()
        {
            return lines.Values.OrderBy(l => l.Order).Select(l => Language.Localize(l.Text)).ToList();
        }
    }
}
