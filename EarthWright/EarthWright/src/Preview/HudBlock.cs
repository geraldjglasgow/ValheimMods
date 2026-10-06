using System.Collections.Generic;
using System.Globalization;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The lines of the HUD's text block (out of reach, why the click would be refused, every module's
    /// <see cref="HudText"/> line, the cursor readout) with the shadow text of each line beside it. The block is built
    /// again only when one of its inputs changed: the HudText lines, the language, the refusal, the readout, or the
    /// out-of-reach distances in tenths of a metre. A repaint only reads the two lists.
    /// </summary>
    internal static class HudBlock
    {
        private static readonly List<string> lines = new List<string>();
        private static readonly List<string> shadows = new List<string>();

        private static int revision = -1, language = -1, distance = -1, reach = -1;
        private static string reason, readout;

        /// <summary>The lines to draw, in order; read them, do not keep or change the list.</summary>
        public static List<string> Lines => lines;

        /// <summary>The shadow text of each line (colour tags removed), by the same index.</summary>
        public static List<string> Shadows => shadows;

        public static void Refresh()
        {
            List<string> module = HudText.Current();
            bool far = PreviewFrame.BrushVisible && PreviewFrame.Tone == PreviewTone.OutOfReach;
            int newDistance = far ? Mathf.RoundToInt(PreviewFrame.Distance * 10f) : -1;
            int newReach = far ? Mathf.RoundToInt(PreviewFrame.Reach * 10f) : -1;
            string newReason = PreviewStatus.FirstReason;
            if (HudText.Revision == revision && Language.Version == language && newDistance == distance && newReach == reach
                && newReason == reason && CursorReadout.Text == readout)
                return;
            revision = HudText.Revision;
            language = Language.Version;
            distance = newDistance;
            reach = newReach;
            reason = newReason;
            readout = CursorReadout.Text;
            Build(module);
        }

        private static void Build(List<string> module)
        {
            lines.Clear();
            shadows.Clear();
            if (distance >= 0)
                Add(string.Format(CultureInfo.InvariantCulture, "<color=#b8b8b8>{0} ({1:0.0} m / {2:0.0} m)</color>",
                    Language.Localize("$ew_preview_outofreach"), distance / 10f, reach / 10f));
            if (!string.IsNullOrEmpty(reason) && !Mentions(module, reason))
                Add("<color=#ff5a4a>" + reason + "</color>");
            foreach (string line in module)
                Add(line);
            if (readout != null)
                Add("<color=#c8c8c8>" + readout + "</color>");
        }

        private static bool Mentions(List<string> module, string text)
        {
            foreach (string line in module)
                if (line.Contains(text))
                    return true;
            return false;
        }

        private static void Add(string line)
        {
            lines.Add(line);
            shadows.Add(HudStyles.Plain(line));
        }
    }
}
