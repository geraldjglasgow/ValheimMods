using System.Collections.Generic;

namespace EarthWright.Core
{
    /// <summary>
    /// The lines of text EarthWright shows next to the crosshair while a terrain tool is in use. Any module sets its own
    /// line by key (for example "brush", "target", "cost", "ramp"); the Preview module draws them, ordered by the given
    /// order, and hides the whole block when the terrain tool is put away. Lines may contain $tokens and rich-text colours.
    /// The ordered, localized list is built again only when a line or the language changed, not on every repaint, and
    /// only a changed line is localized again (token by token, <see cref="TokenText"/>: lines carry numbers).
    /// </summary>
    public static class HudText
    {
        private sealed class Line
        {
            public int Order;
            public string Text;
            public string Localized;
        }

        private static readonly Dictionary<string, Line> lines = new Dictionary<string, Line>();
        private static readonly List<Line> ordered = new List<Line>();
        private static readonly List<string> current = new List<string>();
        private static bool dirty = true;
        private static int localizedFor = -1;

        /// <summary>Counts the times <see cref="Current"/> changed, for the overlay's own cache.</summary>
        public static int Revision { get; private set; }

        /// <summary>Sets or replaces a line. Null or empty text removes it. Setting the same text again costs nothing.</summary>
        public static void Set(string key, string text, int order = 100)
        {
            if (string.IsNullOrEmpty(text))
            {
                Clear(key);
                return;
            }
            if (!lines.TryGetValue(key, out Line line))
                lines[key] = line = new Line();
            else if (line.Order == order && line.Text == text)
                return;
            line.Order = order;
            line.Text = text;
            line.Localized = null;
            dirty = true;
        }

        public static void Clear(string key)
        {
            if (lines.Remove(key))
                dirty = true;
        }

        /// <summary>The current lines, in order, localized. The list is shared: read it, do not keep or change it.</summary>
        public static List<string> Current()
        {
            if (!dirty && localizedFor == Language.Version)
                return current;
            bool relocalize = localizedFor != Language.Version;
            dirty = false;
            localizedFor = Language.Version;
            Order();
            current.Clear();
            foreach (Line line in ordered)
            {
                if (line.Localized == null || relocalize)
                    line.Localized = TokenText.Localize(line.Text);
                current.Add(line.Localized);
            }
            Revision++;
            return current;
        }

        // Insertion by order, stable for equal orders (the way OrderBy kept them), into a list reused every time.
        private static void Order()
        {
            ordered.Clear();
            foreach (Line line in lines.Values)
            {
                int at = ordered.Count;
                while (at > 0 && ordered[at - 1].Order > line.Order)
                    at--;
                ordered.Insert(at, line);
            }
        }
    }
}
