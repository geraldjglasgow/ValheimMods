using BepInEx.Configuration;
using EarthWright.Core;
using EarthWright.Terrain;
using SyncedConfig;

namespace EarthWright.Brush
{
    /// <summary>
    /// Section "1. Brush": the ranges every brush value is clamped to, the starting-size multiplier, the skill cap and
    /// the shapes and paints players may pick. Ranges and multipliers change the world, so they are synced; the step
    /// sizes and the announcement only change how the player's own keys feel, so they are local.
    /// </summary>
    public static class BrushSettings
    {
        public static ConfigEntry<float> MinRadius { get; private set; }
        public static ConfigEntry<float> MaxRadius { get; private set; }
        public static ConfigEntry<float> SizeMultiplier { get; private set; }
        public static ConfigEntry<bool> MultiplyLevel { get; private set; }
        public static ConfigEntry<bool> MultiplyRaise { get; private set; }
        public static ConfigEntry<bool> MultiplySmooth { get; private set; }
        public static ConfigEntry<bool> MultiplyPaint { get; private set; }
        public static ConfigEntry<bool> ResizeModded { get; private set; }
        public static ConfigEntry<float> MinAmount { get; private set; }
        public static ConfigEntry<float> MaxAmount { get; private set; }
        public static ConfigEntry<LevelStyle> DefaultStyle { get; private set; }
        public static ConfigEntry<string> AllowedShapes { get; private set; }
        public static ConfigEntry<string> AllowedPaints { get; private set; }
        public static ConfigEntry<float> RadiusStep { get; private set; }
        public static ConfigEntry<float> AmountStep { get; private set; }
        public static ConfigEntry<float> HardnessStep { get; private set; }
        public static ConfigEntry<float> RotationStep { get; private set; }
        public static ConfigEntry<float> DepthStep { get; private set; }
        public static ConfigEntry<float> FastMultiplier { get; private set; }
        public static ConfigEntry<bool> Announce { get; private set; }
        public static ConfigEntry<bool> AimThroughObjects { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindRanges(synced);
            BindMultipliers(synced);
            BindChoices(synced);
            BindSteps(synced);
            SkillCapSettings.Bind(synced);
        }

        private static void BindRanges(SyncedConfiguration synced)
        {
            MinRadius = synced.Bind(Sections.Brush, "Minimum Radius", 0.5f,
                "The smallest brush radius (metres) any terrain entry can be set to. An entry in EarthWright.Brushes.yml may set its own.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 100f));
            MaxRadius = synced.Bind(Sections.Brush, "Maximum Radius", 20f,
                "The largest brush radius (metres) any terrain entry can be set to. An entry in EarthWright.Brushes.yml may set its own; tool levels and the skill cap may lower it further.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 100f));
            MinAmount = synced.Bind(Sections.Brush, "Minimum Amount", 0.05f,
                "The smallest height (metres) one click of Raise or Lower can move the ground.",
                acceptableValues: new AcceptableValueRange<float>(0.01f, 50f));
            MaxAmount = synced.Bind(Sections.Brush, "Maximum Amount", 8f,
                "The largest height (metres) one click of Raise or Lower can move the ground.",
                acceptableValues: new AcceptableValueRange<float>(0.05f, 50f));
        }

        private static void BindMultipliers(SyncedConfiguration synced)
        {
            SizeMultiplier = synced.Bind(Sections.Brush, "Size Multiplier", 1f,
                "Multiplies the starting radius of every terrain entry (the game's own sizes). Entries with a radius in EarthWright.Brushes.yml keep that radius.",
                acceptableValues: new AcceptableValueRange<float>(0.1f, 10f));
            MultiplyLevel = synced.Bind(Sections.Brush, "Multiply Level", true, "The size multiplier applies to levelling entries.");
            MultiplyRaise = synced.Bind(Sections.Brush, "Multiply Raise", true, "The size multiplier applies to raise and lower entries.");
            MultiplySmooth = synced.Bind(Sections.Brush, "Multiply Smooth", true, "The size multiplier applies to smoothing entries.");
            MultiplyPaint = synced.Bind(Sections.Brush, "Multiply Paint", true, "The size multiplier applies to paint-only entries (path, replant, cultivate without height).");
            ResizeModded = synced.Bind(Sections.Brush, "Resize Modded Terrain Pieces", true,
                "The brush size keys also resize terrain pieces added by other mods. Off: those pieces keep their own size.");
        }

        private static void BindChoices(SyncedConfiguration synced)
        {
            DefaultStyle = synced.Bind(Sections.Brush, "Default Level Style", LevelStyle.Ease,
                "How levelling entries approach the target until a player picks another style with the style key. Ease: gently, like the unmodded game. Step: straight toward it, at most the max step per click. Instant: one click makes a flat plateau. Also applies to the game's own Level ground.");
            AllowedShapes = synced.Bind(Sections.Brush, "Allowed Shapes", "Circle, Square, Rectangle, Ring, Frame",
                "The brush shapes the shape key cycles through, separated by commas. Possible: Circle, Square, Rectangle, Ring, Frame.");
            AllowedPaints = synced.Bind(Sections.Brush, "Allowed Paints", "Dirt, Paved, Cultivated, Grass, Original, Vegetation, ClearVegetation, Keep",
                "The paints the paint key cycles through besides the entry's own, separated by commas. Possible: Dirt, Paved, Cultivated, Grass, Original, Vegetation, ClearVegetation, Keep.");
            AimThroughObjects = synced.Bind(Sections.Brush, "Aim Through Objects", true,
                "When the crosshair is on a rock, tree, cliff or water instead of the ground, the brush goes to the ground behind or under it instead of the game refusing the click. Buildings are never looked through.",
                synced: false);
        }

        private static void BindSteps(SyncedConfiguration synced)
        {
            RadiusStep = Step(synced, "Radius Step", 0.5f, "Metres the radius changes per wheel notch or key press.", 10f);
            AmountStep = Step(synced, "Amount Step", 0.1f, "Metres the raise/lower amount changes per step; also the step of strength and density, and of the level max step below 2 m.", 5f);
            HardnessStep = Step(synced, "Hardness Step", 0.05f, "How much the edge hardness (0 soft to 1 hard) changes per step.", 1f);
            RotationStep = Step(synced, "Rotation Step", 22.5f, "Degrees the footprint turns per step and per arrow key press.", 180f);
            DepthStep = Step(synced, "Depth Step", 0.5f, "Metres the rectangle depth, ring inner radius or frame band width changes per step.", 10f);
            FastMultiplier = synced.Bind(Sections.Brush, "Fast Step Multiplier", 5f,
                "Steps are this many times larger while the fast modifier (Ctrl) is held; the height keys use it with Shift.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(1f, 20f));
            Announce = synced.Bind(Sections.Brush, "Announce Changes", false,
                "Also shows every brush value change as a message in the middle of the screen.", synced: false);
        }

        private static ConfigEntry<float> Step(SyncedConfiguration synced, string key, float value, string text, float limit)
        {
            return synced.Bind(Sections.Brush, key, value, text + " A negative value reverses the mouse wheel direction.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(-limit, limit));
        }
    }
}
