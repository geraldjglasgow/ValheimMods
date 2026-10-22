using EarthWright.Terrain;
using Translations;

namespace EarthWright.Brush
{
    /// <summary>The Brush module's words as $tokens (English in translations/English.yml); the HUD and messages localize them.</summary>
    public static class BrushWords
    {
        public static readonly string Size = Lang.Token("ew_brush_size"), Edge = Lang.Token("ew_brush_edge");
        public static readonly string Hardness = Lang.Token("ew_brush_hardness"), Amount = Lang.Token("ew_brush_amount");
        public static readonly string MaxStep = Lang.Token("ew_brush_maxstep"), Strength = Lang.Token("ew_brush_strength");
        public static readonly string Density = Lang.Token("ew_brush_density"), Rotation = Lang.Token("ew_brush_rotation");
        public static readonly string Paint = Lang.Token("ew_brush_paint"), Target = Lang.Token("ew_brush_target");
        public static readonly string Grid = Lang.Token("ew_brush_grid"), AimEdge = Lang.Token("ew_brush_aimedge");
        public static readonly string Inner = Lang.Token("ew_brush_inner"), Depth = Lang.Token("ew_brush_depth");
        public static readonly string Band = Lang.Token("ew_brush_band");
        public static readonly string HardLevelNeeds = Lang.Token("ew_brush_hardlevel_needs");
        public static readonly string LevelLocked = Lang.Token("ew_brush_levellocked");
        public static readonly string NoFloor = Lang.Token("ew_brush_nofloor");
        public static readonly string FloorCopied = Lang.Token("ew_brush_floorcopied");
        public static readonly string Locked = Lang.Token("ew_brush_locked");
        public static readonly string Released = Lang.Token("ew_brush_released");
        public static readonly string TargetMode = Lang.Token("ew_brush_targetmode");

        private static readonly string[] shapes =
        {
            Lang.Token("ew_brush_shape_circle"), Lang.Token("ew_brush_shape_square"), Lang.Token("ew_brush_shape_rectangle"),
            Lang.Token("ew_brush_shape_ring"), Lang.Token("ew_brush_shape_frame"),
        };

        private static readonly string[] styles =
        {
            Lang.Token("ew_brush_style_ease"), Lang.Token("ew_brush_style_step"), Lang.Token("ew_brush_style_instant"),
        };

        private static readonly string[] paints =
        {
            Lang.Token("ew_brush_paint_own"), Lang.Token("ew_brush_paint_dirt"), Lang.Token("ew_brush_paint_paved"),
            Lang.Token("ew_brush_paint_cultivated"), Lang.Token("ew_brush_paint_grass"), Lang.Token("ew_brush_paint_original"),
            Lang.Token("ew_brush_paint_vegetation"), Lang.Token("ew_brush_paint_clearvegetation"), Lang.Token("ew_brush_paint_keep"),
        };

        private static readonly string[] sources =
        {
            Lang.Token("ew_brush_src_feet"), Lang.Token("ew_brush_src_aimed"), Lang.Token("ew_brush_src_locked"),
            Lang.Token("ew_brush_src_exact"), Lang.Token("ew_brush_src_continued"), Lang.Token("ew_brush_src_floor"),
        };

        private static readonly string on = Lang.Token("ew_brush_on"), off = Lang.Token("ew_brush_off");

        public static string ShapeName(BrushShape shape) => Pick(shapes, (int)shape);

        public static string StyleName(LevelStyle style) => Pick(styles, (int)style);

        public static string PaintName(PaintChoice paint) => Pick(paints, (int)paint);

        public static string SourceName(TargetSource source) => Pick(sources, (int)source);

        public static string OnOff(bool value) => value ? on : off;

        public static string NoFlat { get; } = Lang.Token("ew_brush_noflat");

        private static string Pick(string[] names, int index)
        {
            return names != null && index >= 0 && index < names.Length ? names[index] : "?";
        }

        /// <summary>The words of the second-dimension value for a shape: rectangle depth, ring inner radius, frame band.</summary>
        public static string DepthName(BrushShape shape)
        {
            return shape == BrushShape.Ring ? Inner : shape == BrushShape.Frame ? Band : Depth;
        }
    }
}
