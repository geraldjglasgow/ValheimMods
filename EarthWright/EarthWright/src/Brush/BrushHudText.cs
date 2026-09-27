using System.Globalization;
using System.Text;
using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Brush
{
    /// <summary>
    /// Builds the brush's HUD lines (with $tokens; the HUD localizes them): size and feather width, shape, rotation,
    /// hardness, the amount value, level style, paint and the grid/edge flags, with the selected value highlighted;
    /// and the target height line. Only values that apply to the entry are shown, to keep the block short.
    /// </summary>
    public static class BrushHudText
    {
        private const string Highlight = "<color=#FFD24A>";
        private const string Separator = "  ·  ";
        private static readonly StringBuilder line = new StringBuilder();

        public static string BrushLine(ToolAction action, BrushValues values)
        {
            line.Clear();
            Part(BrushValue.Radius, action, values, SizeText(action));
            if (!EntryKinds.IsPath(action))
                AddShapeParts(action, values);
            PaintChoice paint = PaintCycle.Effective(action, values);
            if (paint != PaintChoice.Own)
                Plain(BrushWords.Paint + " " + BrushWords.PaintName(paint));
            if (BrushState.GridMode)
                Plain(BrushWords.Grid);
            if (BrushState.AimAtEdge && !EntryKinds.IsPath(action))
                Plain(BrushWords.AimEdge);
            return line.ToString();
        }

        private static void AddShapeParts(ToolAction action, BrushValues values)
        {
            BrushShape shape = BrushState.Shape;
            Plain(BrushWords.ShapeName(shape));
            if (ValueSelector.HasDepth(shape))
                Part(BrushValue.Depth, action, values, BrushWords.DepthName(shape) + " " + M(BrushState.Radius2));
            if (ValueSelector.Applies(BrushValue.Rotation, action, values))
                Part(BrushValue.Rotation, action, values, BrushWords.Rotation + " " + F(BrushState.Rotation, "0.#") + "°");
            if (!BrushState.GridMode)
                Part(BrushValue.Hardness, action, values, BrushWords.Hardness + " " + Percent(BrushState.Hardness));
            string amount = AmountText(action, values);
            if (amount != null)
                Part(BrushValue.Amount, action, values, amount);
            if (StyleCycle.Applies(action))
                Plain(BrushWords.StyleName(BrushState.Style));
        }

        /// <summary>The target line, or null when the entry does not level and no height is locked (ramps and roads: only a set height).</summary>
        public static string TargetLine(ToolAction action, BrushValues values)
        {
            bool fixedMode = TargetState.IsFixed;
            bool wanted = TargetHeight.Used(action) || HardLevel.Applies(action) || fixedMode;
            if (!wanted || (EntryKinds.IsPath(action) && !fixedMode))
                return null;
            line.Clear();
            Part(BrushValue.Target, action, values, TargetText());
            return line.ToString();
        }

        /// <summary>The selected value alone, for the optional announcement.</summary>
        public static string SelectedValue(ToolAction action, BrushValues values)
        {
            switch (BrushState.Selected)
            {
                case BrushValue.Radius: return SizeText(action);
                case BrushValue.Amount: return AmountText(action, values);
                case BrushValue.Hardness: return BrushWords.Hardness + " " + Percent(BrushState.Hardness);
                case BrushValue.Rotation: return BrushWords.Rotation + " " + F(BrushState.Rotation, "0.#") + "°";
                case BrushValue.Depth: return BrushWords.DepthName(BrushState.Shape) + " " + M(BrushState.Radius2);
                case BrushValue.Target: return TargetText();
                default: return null;
            }
        }

        private static string SizeText(ToolAction action)
        {
            string text = BrushWords.Size + " " + M(BrushState.Radius);
            float feather = BrushState.Radius * (1f - BrushState.Hardness);
            if (!EntryKinds.IsPath(action) && !BrushState.GridMode && feather >= 0.05f)
                text += " (" + BrushWords.Edge + " " + M(feather) + ")";
            return text;
        }

        private static string AmountText(ToolAction action, BrushValues values)
        {
            switch (ValueSelector.AmountOf(action, values))
            {
                case AmountKind.Amount: return BrushWords.Amount + " " + F(BrushState.Amount, "0.00") + " m";
                case AmountKind.MaxStep: return BrushWords.MaxStep + " " + F(BrushState.MaxStep, "0.0#") + " m";
                case AmountKind.Strength: return BrushWords.Strength + " " + Percent(BrushState.Strength);
                case AmountKind.Density: return BrushWords.Density + " " + Percent(BrushState.Density);
                default: return null;
            }
        }

        private static string TargetText()
        {
            string source = BrushWords.SourceName(BrushState.TargetSource);
            if (BrushState.TargetMode == TargetSource.Continued && BrushState.TargetSource != TargetSource.Continued)
                source = BrushWords.SourceName(TargetSource.Continued) + ": " + BrushWords.NoFlat;
            return BrushWords.Target + " " + F(BrushState.TargetHeight, "0.00") + " m (" + source + ")";
        }

        private static void Part(BrushValue value, ToolAction action, BrushValues values, string text)
        {
            bool selected = BrushState.Selected == value && ValueSelector.Applies(value, action, values);
            if (line.Length > 0)
                line.Append(Separator);
            if (selected)
                line.Append(Highlight);
            line.Append(text);
            if (selected)
                line.Append("</color>");
        }

        private static void Plain(string text)
        {
            if (line.Length > 0)
                line.Append(Separator);
            line.Append(text);
        }

        private static string M(float metres) => F(metres, "0.0#") + " m";

        private static string Percent(float share) => F(share * 100f, "0") + "%";

        private static string F(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
    }
}
