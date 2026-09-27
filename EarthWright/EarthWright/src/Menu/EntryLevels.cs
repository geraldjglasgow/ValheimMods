using EarthWright.Brush;

namespace EarthWright.Menu
{
    /// <summary>
    /// The tool levels' "Level Unlocks" (Gear module, handed over as <see cref="BrushCaps.LevelUnlocks"/>) for
    /// EarthWright's own entries: an entry's feature name is its id without "ew_" (lower, smooth, paint, reset,
    /// groundbreaker, terraform, till, dig ...; a custom entry is custom_&lt;id&gt;). Custom entries check it before each
    /// run of their command (repeats included); clicks and edits of every entry are gated by the Gear module's level
    /// guard and the placement hook's entry refusal.
    /// </summary>
    public static class EntryLevels
    {
        /// <summary>The entry with this piece name may be used with the held tool's level (true for anything else).</summary>
        public static bool Unlocked(string pieceName)
        {
            if (pieceName == null || !pieceName.StartsWith("ew_") || !EntryRegistry.IsOurs(pieceName))
                return true;
            return BrushCaps.LevelUnlocks(pieceName.Substring(3));
        }
    }
}
