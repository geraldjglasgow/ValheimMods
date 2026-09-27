using EarthWright.Terrain;

namespace EarthWright.Actions
{
    /// <summary>Which tool a menu entry belongs to (for settings that differ between them).</summary>
    public enum ToolFamily : byte
    {
        Hoe = 0,
        Cultivator = 1,
        /// <summary>A terrain piece added by another mod; its action is read from its own TerrainOp.</summary>
        Modded = 3,
    }

    /// <summary>
    /// What clicking with one build-menu entry does. Brush strokes are built from the action plus the brush state by
    /// <see cref="EditFactory"/>; a special action (ramp, road, clearing, a custom entry) is handled entirely by the
    /// module that registered it with <see cref="SpecialActions"/>.
    /// </summary>
    public sealed class ToolAction
    {
        /// <summary>The piece prefab name ("mud_road_v2", "ew_lower", ...).</summary>
        public string Id;

        public ToolFamily Family;

        public HeightOp Height = HeightOp.None;

        public LevelStyle Style = LevelStyle.Ease;

        public PaintOp Paint = PaintOp.None;

        /// <summary>The vanilla radius of the effect, the brush's starting radius for this entry.</summary>
        public float BaseRadius = 2f;

        /// <summary>Paint radius as a share of the brush radius (vanilla ratios: level 1.0, raise 1.25, paved 0.73).</summary>
        public float PaintRatio = 1f;

        /// <summary>Starting raise or lower amount in metres.</summary>
        public float Amount = 1f;

        /// <summary>Starting largest change per click for Ease and Step levelling.</summary>
        public float MaxStep = 1f;

        /// <summary>Starting edge hardness.</summary>
        public float Hardness = 0.3f;

        /// <summary>Starting smoothing strength (Smooth only).</summary>
        public float Strength = 0.5f;

        /// <summary>The entry levels to a target height, so the target height modes apply.</summary>
        public bool UsesTarget;

        /// <summary>The entry raises or lowers by an amount, so the amount keys apply.</summary>
        public bool UsesAmount;

        /// <summary>The brush size keys resize this entry.</summary>
        public bool Resizable = true;

        /// <summary>Only paint where the ground is not above the reference height (vanilla's paint height check).</summary>
        public bool PaintHeightCheck;

        /// <summary>Null for a brush stroke, otherwise the special action's key ("ramp", "road", "clear", "uproot", "custom:&lt;id&gt;").</summary>
        public string Special;

        /// <summary>Only admins may use this entry; its edits are privileged (relayed and checked by the server).</summary>
        public bool AdminOnly;

        public bool IsSpecial => !string.IsNullOrEmpty(Special);

        /// <summary>A ramp or road entry: its clicks are points of a line, not brush strokes (every other entry uses the whole brush).</summary>
        public bool IsPathTool => Special == "ramp" || Special == "road";
    }
}
