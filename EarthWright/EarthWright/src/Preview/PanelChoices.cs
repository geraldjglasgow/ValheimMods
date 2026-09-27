using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Terrain;

namespace EarthWright.Preview
{
    /// <summary>
    /// The choice grids of the panel: shape, level style and paint, offering exactly what the Brush module's keys cycle
    /// through (the shapes and paints the server allows; "keep the paint" only for entries that change the height), and
    /// written into the entry's remembered <see cref="BrushValues"/> as the keys do. Captions are the Brush module's words.
    /// </summary>
    internal static class PanelChoices
    {
        private static readonly ChoiceList<BrushShape> shapes = new ChoiceList<BrushShape>(() => BrushSettings.AllowedShapes);
        private static readonly ChoiceList<PaintChoice> paints = new ChoiceList<PaintChoice>(() => BrushSettings.AllowedPaints);
        private static readonly LevelStyle[] Styles = { LevelStyle.Ease, LevelStyle.Step, LevelStyle.Instant };

        /// <summary>Draws the grids; true when the player picked something new.</summary>
        public static bool Draw(ToolAction action, BrushValues values)
        {
            bool changed = false;
            List<BrushShape> allowed = shapes.Values;
            if (allowed.Count > 1)
                changed |= Pick("$ew_preview_shape", allowed, ref values.Shape, ShapeCycle.Effective(values), s => BrushWords.ShapeName(s));
            if (StyleCycle.Applies(action))
                changed |= Pick("$ew_preview_style", new List<LevelStyle>(Styles), ref values.Style, values.Style, s => BrushWords.StyleName(s));
            List<PaintChoice> offered = Paints(action);
            if (offered.Count > 1)
                changed |= Pick("$ew_preview_paint", offered, ref values.Paint, PaintCycle.Effective(action, values), p => BrushWords.PaintName(p));
            return changed;
        }

        /// <summary>The entry's own paint, then every allowed paint that applies to the entry.</summary>
        private static List<PaintChoice> Paints(ToolAction action)
        {
            List<PaintChoice> offered = new List<PaintChoice> { PaintChoice.Own };
            foreach (PaintChoice choice in paints.Values)
            {
                bool keepUseless = choice == PaintChoice.Keep && action.Height == HeightOp.None;
                if (choice != PaintChoice.Own && !keepUseless)
                    offered.Add(choice);
            }
            return offered;
        }

        private static bool Pick<T>(string labelToken, List<T> options, ref T field, T current, System.Func<T, string> caption)
        {
            string[] tokens = options.ConvertAll(o => caption(o)).ToArray();
            int index = options.IndexOf(current);
            int chosen = PanelFields.Choice(labelToken, index, tokens, 3);
            if (chosen == index || chosen < 0 || chosen >= options.Count)
                return false;
            field = options[chosen];
            return true;
        }
    }
}
