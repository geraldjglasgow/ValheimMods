using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The ship panel's lines for other Broadside code: the panel on the HUD while the local player is aboard a launched
    /// ship (<see cref="KwShipPanelView"/>) shows its own lines (speed, speed multiplier, explore radius, ship health),
    /// then these, in the order they were first asked for, then the helmsman's Sailing abilities.
    /// <para>
    /// Use: ask for a line once by an id of your own and keep it (<c>static readonly KwPanelLine port =
    /// KwShipPanel.Row("cannons.port");</c>), then, while it should show, call <see cref="KwPanelLine.Set"/> with its words
    /// (label, value, and optionally a tooltip topic and text) at least once a second, or <see cref="KwPanelLine.Keep"/>
    /// when the words did not change; <see cref="KwPanelLine.SetBar"/> draws a thin progress bar along its bottom (a
    /// reload). A line not set or kept for <see cref="KwPanelLine.Linger"/> seconds hides by itself, and
    /// <see cref="KwPanelLine.Hide"/> hides it at once. The panel draws the lines four times a second, writing only what
    /// changed, so setting the same words every frame costs only the caller's own strings. Values may use the game's rich
    /// text (<see cref="KwPanelLook.Orange"/>, <see cref="KwPanelLook.Red"/>). Main thread only; local display only,
    /// nothing is sent: set lines from what the local player's game already knows (synced ZDO values).
    /// </para>
    /// </summary>
    public static class KwShipPanel
    {
        private static readonly List<KwPanelLine> lines = new List<KwPanelLine>();
        private static readonly Dictionary<string, KwPanelLine> byId = new Dictionary<string, KwPanelLine>();

        /// <summary>The line with this id, made the first time it is asked for; the same object every later time.</summary>
        public static KwPanelLine Row(string id)
        {
            if (byId.TryGetValue(id, out KwPanelLine line))
                return line;
            line = new KwPanelLine(id, lines.Count);
            lines.Add(line);
            byId.Add(id, line);
            return line;
        }

        /// <summary>Every line asked for so far, in that order (the panel's).</summary>
        internal static List<KwPanelLine> Lines => lines;
    }
}
