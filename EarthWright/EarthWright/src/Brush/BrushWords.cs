using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Brush
{
    /// <summary>The Brush module's words (English built in, translatable through the language files), as $tokens.</summary>
    public static class BrushWords
    {
        public static string Size, Edge, Hardness, Amount, MaxStep, Strength, Density, Rotation, Paint, Target, Grid, AimEdge;
        public static string Inner, Depth, Band, Metres;
        public static string HardLevelNeeds, LevelLocked, NoFloor, FloorCopied, Locked, Released, TargetMode;
        public static string Wheel, HintValue, HintChange, HintShape, HintStyle, HintPaint, HintLock, HintTarget, HintGrid, HintEdge, HintHard;
        private static string[] shapes, styles, paints, sources;
        private static string on, off, noFlat;

        public static void Register()
        {
            RegisterValues();
            RegisterMessages();
            RegisterHints();
            shapes = new[] { W("shape_circle", "Circle"), W("shape_square", "Square"), W("shape_rectangle", "Rectangle"), W("shape_ring", "Ring"), W("shape_frame", "Frame") };
            styles = new[] { W("style_ease", "Ease"), W("style_step", "Step"), W("style_instant", "Instant") };
            paints = new[]
            {
                W("paint_own", "entry's own"), W("paint_dirt", "Dirt"), W("paint_paved", "Paved"), W("paint_cultivated", "Cultivated"),
                W("paint_grass", "Grass"), W("paint_original", "Original"), W("paint_vegetation", "Vegetation"),
                W("paint_clearvegetation", "Clear vegetation"), W("paint_keep", "Keep"),
            };
            sources = new[] { W("src_feet", "feet"), W("src_aimed", "aimed"), W("src_locked", "locked"), W("src_exact", "exact"), W("src_continued", "continued"), W("src_floor", "floor") };
        }

        private static void RegisterValues()
        {
            Size = W("size", "Size");
            Edge = W("edge", "edge");
            Hardness = W("hardness", "Hard");
            Amount = W("amount", "Amount");
            MaxStep = W("maxstep", "Max step");
            Strength = W("strength", "Strength");
            Density = W("density", "Density");
            Rotation = W("rotation", "Turn");
            Paint = W("paint", "Paint");
            Target = W("target", "Target");
            Grid = W("grid", "Grid");
            AimEdge = W("aimedge", "Aim at edge");
            Inner = W("inner", "Inner");
            Depth = W("depth", "Depth");
            Band = W("band", "Band");
            Metres = "m";
        }

        private static void RegisterMessages()
        {
            HardLevelNeeds = W("hardlevel_needs", "Hard level works with a level or raise entry selected");
            LevelLocked = W("levellocked", "Your tool's level does not allow this yet");
            NoFloor = W("nofloor", "Aim at a building piece to copy its height");
            FloorCopied = W("floorcopied", "Target copied from the floor:");
            Locked = W("locked", "Target height locked:");
            Released = W("released", "Target height released");
            TargetMode = W("targetmode", "Target from");
            on = W("on", "on");
            off = W("off", "off");
            noFlat = W("noflat", "no flat found");
        }

        private static void RegisterHints()
        {
            Wheel = W("wheel", "Wheel");
            HintValue = W("hint_value", "value");
            HintChange = W("hint_change", "change");
            HintShape = W("hint_shape", "shape");
            HintStyle = W("hint_style", "style");
            HintPaint = W("hint_paint", "paint");
            HintLock = W("hint_lock", "lock height");
            HintTarget = W("hint_target", "target");
            HintGrid = W("hint_grid", "grid");
            HintEdge = W("hint_edge", "edge");
            HintHard = W("hint_hard", "hard level");
        }

        private static string W(string key, string english) => Language.Add("ew_brush_" + key, english);

        public static string ShapeName(BrushShape shape) => Pick(shapes, (int)shape);

        public static string StyleName(LevelStyle style) => Pick(styles, (int)style);

        public static string PaintName(PaintChoice paint) => Pick(paints, (int)paint);

        public static string SourceName(TargetSource source) => Pick(sources, (int)source);

        public static string OnOff(bool value) => value ? on : off;

        public static string NoFlat => noFlat;

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
