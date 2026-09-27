using System;
using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Brush
{
    /// <summary>
    /// The tool-level unlocks the brush asks for (<see cref="BrushCaps.LevelUnlocks"/>, answered by the Gear module
    /// from its "Level Unlocks" setting). Feature names: the shapes (circle, square, rectangle, ring, frame), the level
    /// styles (ease, step, instant; the hard level key is an instant level) and entries by piece name, with or without
    /// EarthWright's "ew_" prefix (lower, smooth, raise_v2, ...). A locked shape or style is skipped by its key and falls
    /// back when remembered. Locked entries, shapes and styles in edits are refused by the Gear module's sender guard.
    /// Runs on the player's machine: the level is the held tool's.
    /// </summary>
    public static class LevelGate
    {
        private static readonly string[] ShapeNames = { "circle", "square", "rectangle", "ring", "frame" };
        private static readonly string[] StyleNames = { "ease", "step", "instant" };


        public static bool Shape(BrushShape shape) => Unlocked(Name(ShapeNames, (int)shape));

        public static bool Style(LevelStyle style) => Unlocked(Name(StyleNames, (int)style));

        public static bool Entry(ToolAction action)
        {
            if (action == null || string.IsNullOrEmpty(action.Id))
                return true;
            bool prefixed = action.Id.StartsWith("ew_", StringComparison.Ordinal);
            return Unlocked(action.Id) && (!prefixed || Unlocked(action.Id.Substring(3)));
        }

        private static bool Unlocked(string feature)
        {
            if (feature == null)
                return true;
            try
            {
                return BrushCaps.LevelUnlocks(feature);
            }
            catch (Exception e)
            {
                BrushLog.Error("tool level unlocks", e);
                return true;
            }
        }

        private static string Name(string[] names, int index) => index >= 0 && index < names.Length ? names[index] : null;
    }
}
