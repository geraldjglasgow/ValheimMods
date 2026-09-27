using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Brush
{
    /// <summary>
    /// The shape key: cycles the entry's shape through the shapes the server allows ("Allowed Shapes") and the held
    /// tool's level has unlocked. A remembered shape that is no longer usable falls back to the first usable one
    /// (circle when none is).
    /// </summary>
    public static class ShapeCycle
    {
        private static readonly ChoiceList<BrushShape> allowed = new ChoiceList<BrushShape>(() => BrushSettings.AllowedShapes);

        public static bool Usable(BrushShape shape) => allowed.Contains(shape) && LevelGate.Shape(shape);

        /// <summary>The shape the brush uses for these values.</summary>
        public static BrushShape Effective(BrushValues values)
        {
            if (Usable(values.Shape))
                return values.Shape;
            foreach (BrushShape shape in allowed.Values)
            {
                if (LevelGate.Shape(shape))
                    return shape;
            }
            return BrushShape.Circle;
        }

        public static void Next(BrushValues values)
        {
            var list = allowed.Values;
            int start = list.IndexOf(Effective(values));
            for (int i = 1; i <= list.Count; i++)
            {
                BrushShape candidate = list[(start + i + list.Count) % list.Count];
                if (!LevelGate.Shape(candidate))
                    continue;
                values.Shape = candidate;
                BrushAnnounce.Toggle(BrushWords.ShapeName(candidate));
                return;
            }
        }
    }

    /// <summary>The level style key: Ease, Step, Instant (those the held tool's level has unlocked), for entries that level.</summary>
    public static class StyleCycle
    {
        private static readonly LevelStyle[] Order = { LevelStyle.Ease, LevelStyle.Step, LevelStyle.Instant };

        public static bool Applies(ToolAction action) => !EntryKinds.IsPath(action) && action.Height == HeightOp.Level;

        /// <summary>The style the brush uses: the remembered one, or the first unlocked one (Ease when none is).</summary>
        public static LevelStyle Effective(BrushValues values)
        {
            if (LevelGate.Style(values.Style))
                return values.Style;
            foreach (LevelStyle style in Order)
            {
                if (LevelGate.Style(style))
                    return style;
            }
            return LevelStyle.Ease;
        }

        public static void Next(ToolAction action, BrushValues values)
        {
            if (!Applies(action))
                return;
            int start = System.Array.IndexOf(Order, Effective(values));
            for (int i = 1; i <= Order.Length; i++)
            {
                LevelStyle candidate = Order[(start + i) % Order.Length];
                if (!LevelGate.Style(candidate))
                    continue;
                values.Style = candidate;
                BrushAnnounce.Toggle(BrushWords.StyleName(candidate));
                return;
            }
        }
    }
}
