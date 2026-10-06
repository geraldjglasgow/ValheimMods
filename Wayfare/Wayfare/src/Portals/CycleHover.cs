using Wayfare.Core;

namespace Wayfare.Portals
{
    /// <summary>The Alt+E line a portal's and a sea gate pillar's hover text end with ("[Alt + E] Cycle access mode:
    /// Public"). A hover text is asked for every frame the player looks at the piece, so the line is localized once per
    /// mode and built again only when the language (<see cref="Language.Revision"/>) or the input device, whose key
    /// names it shows, changes.</summary>
    internal static class CycleHover
    {
        private static readonly string[] lines = new string[3];
        private static int revision = -1;
        private static int device = -1;

        // The portal hover text last extended, and the result: the game's own hover text comes out of its localizer's
        // cache as the same string while nothing changes, so the joined text is reused too.
        private static string lastHover;
        private static string lastLine;
        private static string lastJoined;

        /// <summary>The line for a mode, without a line break before it.</summary>
        internal static string Line(PortalMode mode)
        {
            int now = Device();
            if (revision != Language.Revision || device != now)
            {
                System.Array.Clear(lines, 0, lines.Length);
                revision = Language.Revision;
                device = now;
            }
            int index = (int)mode;
            if (index < 0 || index >= lines.Length)
                return Build(mode);
            return lines[index] ?? (lines[index] = Build(mode));
        }

        /// <summary>A hover text with the line for a mode on a line of its own below it.</summary>
        internal static string Append(string hover, PortalMode mode)
        {
            string line = Line(mode);
            if (lastJoined == null || !ReferenceEquals(hover, lastHover) || !ReferenceEquals(line, lastLine))
            {
                lastHover = hover;
                lastLine = line;
                lastJoined = hover + "\n" + line;
            }
            return lastJoined;
        }

        /// <summary>Which key names the line shows: the gamepad's alt keys with a non-classic layout in use, else the
        /// AltPlace key (<see cref="ModeCycle.AltUseKeys"/>, read the same way).</summary>
        private static int Device() => ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() ? 1 : 0;

        private static string Build(PortalMode mode)
        {
            Localization localization = Localization.instance;
            string label = localization.Localize(ModeCycle.ModeLabel(mode));
            return localization.Localize(ModeCycle.AltUseKeys) + string.Format(localization.Localize(Words.HoverCycle), label);
        }
    }
}
