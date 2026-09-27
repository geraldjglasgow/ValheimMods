using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Gear
{
    /// <summary>
    /// Enforces "Level Unlocks" on terrain edits: a sender guard refuses an edit made with an entry, shape or level style
    /// the held tool's level has not unlocked (brush clicks, hold-to-repeat, the hard-level key, ramps and roads), and
    /// while such an entry is selected the preview shows why (<see cref="PreviewStatus"/>). Edits that are not an
    /// entry's (commands, resets) and undo or redo are never gated. Special entries that send no terrain edit check
    /// <see cref="BrushCaps.LevelUnlocks"/> themselves (Paths for ramp and road, the uproot action).
    /// </summary>
    public static class LevelGate
    {
        private const string StatusKey = "tool level";

        public static void Install()
        {
            EditGuards.AddSender("EarthWright tool level", context => Refusal(context.Edit));
            Ticker.OnUpdate("EarthWright tool level", Report);
        }

        private static string Refusal(TerrainEdit edit)
        {
            // Undo and redo carry the original entry as their source; putting ground back is never gated.
            ToolAction action = edit != null && !edit.Has(EditFlags.IsRestore) ? ActionCatalog.ById(edit.Source) : null;
            if (action == null)
                return null;
            if (edit.Kind != EditKind.Stroke || edit.Stroke == null)
                return LevelCaps.Refusal(action.Id, null, null);
            BrushStroke stroke = edit.Stroke;
            return LevelCaps.Refusal(action.Id, stroke.Shape, stroke.Height == HeightOp.Level ? stroke.Style : (LevelStyle?)null);
        }

        /// <summary>The preview's reason while the selected entry (or its shape or style) is locked for the held tool.</summary>
        private static void Report()
        {
            ToolAction action = BrushState.Active ? BrushState.Action : null;
            string reason = null;
            if (action != null && action.IsSpecial)
                reason = LevelCaps.Refusal(action.Id, null, null);
            else if (action != null)
                reason = LevelCaps.Refusal(action.Id, BrushState.Shape, action.Height == HeightOp.Level ? BrushState.Style : (LevelStyle?)null);
            PreviewStatus.Report(StatusKey, reason);
        }
    }
}
