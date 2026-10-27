namespace Broadside
{
    /// <summary>
    /// A tool's description in the hammer's panel at the bottom of the screen: one action a line, its key or click first
    /// in the game's own key style (yellow and bold in brackets, as the game's hover texts show their keys), so each line
    /// reads at a glance. The tool's name stands above it on its own.
    /// </summary>
    public static class KwToolText
    {
        public const string Click = "Click", ShiftClick = "Shift + click", Wheel = "Wheel", ShiftWheel = "Shift + wheel";

        /// <summary>The lines, each "[Key] what it does".</summary>
        public static string Lines(params (string Key, string What)[] lines) =>
            string.Join("\n", System.Array.ConvertAll(lines, line => $"[<color=yellow><b>{line.Key}</b></color>] {line.What}"));

        /// <summary>Every tool's last line: a click on another tool's part switches to that tool and changes the part (<see cref="KwToolSwitch"/>).</summary>
        public static string OtherParts => Lines((Click, "any other part: switch to its tool and change it"));

        /// <summary>The lines, then the two every tool that edits its parts ends with: click one to change it, Shift + click every one.</summary>
        public static string WithEdits(string one, string many, params (string Key, string What)[] lines) =>
            Lines(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(lines, new[] { (Click, $"{one}: change it"), (ShiftClick, $"{one}: change every {many} on the dock") })));
    }
}
