using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>The paint a ramp or road puts on its surface when the player's paint choice is the entry's own.</summary>
    public enum PathPaint
    {
        None = 0,
        Dirt = 1,
        Paved = 2,
        Cultivated = 3,
        Grass = 4,
    }

    /// <summary>
    /// Section "4. Ramps and Roads". The caps and the shape constants change the world, so they are synced and
    /// lockable; the player's own choices (profile, blended ends, width by cursor), the preview and the keys are local.
    /// </summary>
    public static class PathSettings
    {
        /// <summary>No ramp or road may be steeper than this, whatever the setting says.</summary>
        public const float AbsoluteMaxSlope = 75f;

        private const string S = Sections.Paths;

        public static ConfigEntry<int> MaxPoints { get; private set; }
        public static ConfigEntry<float> MaxSlope { get; private set; }
        public static ConfigEntry<float> MinWidth { get; private set; }
        public static ConfigEntry<float> MaxWidth { get; private set; }
        public static ConfigEntry<float> MaxLength { get; private set; }
        public static ConfigEntry<bool> RefusePastLimit { get; private set; }
        public static ConfigEntry<bool> QuickRampAllowed { get; private set; }
        public static ConfigEntry<float> ShoulderWidth { get; private set; }
        public static ConfigEntry<float> EndBlendLength { get; private set; }
        public static ConfigEntry<float> SoftJoinLength { get; private set; }
        public static ConfigEntry<PathPaint> RampPaint { get; private set; }
        public static ConfigEntry<PathPaint> RoadPaint { get; private set; }
        public static ConfigEntry<RampProfile> Profile { get; private set; }
        public static ConfigEntry<bool> WidthFromCursor { get; private set; }
        public static ConfigEntry<bool> ClearWhenDeselected { get; private set; }
        public static ConfigEntry<bool> QuickRampBlendEnds { get; private set; }
        public static ConfigEntry<bool> RoadBlendEnds { get; private set; }
        public static ConfigEntry<float> WarningSlope { get; private set; }
        public static ConfigEntry<float> CartGreenSlope { get; private set; }
        public static ConfigEntry<float> CartYellowSlope { get; private set; }
        public static ConfigEntry<bool> ShowVertices { get; private set; }
        public static ConfigEntry<bool> PreviewOnTop { get; private set; }
        public static ConfigEntry<bool> QuickRampPreview { get; private set; }
        public static ConfigEntry<KeyboardShortcut> QuickRampKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> CarveKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> CarvePavedKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> RemoveKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OneSideModifier { get; private set; }
        public static ConfigEntry<KeyboardShortcut> BlendEndsModifier { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindCaps(synced);
            BindShape(synced);
            BindChoices(synced);
            BindDisplay(synced);
            BindKeys(synced);
        }

        /// <summary>The narrowest allowed width, never above the widest.</summary>
        public static float MinWidthValue => Mathf.Min(MinWidth.Value, MaxWidth.Value);

        public static float ClampWidth(float width) => Mathf.Clamp(width, MinWidthValue, MaxWidth.Value);

        /// <summary>The steepest allowed slope in degrees, never above the absolute cap.</summary>
        public static float MaxSlopeValue => Mathf.Min(MaxSlope.Value, AbsoluteMaxSlope);

        private static void BindCaps(SyncedConfiguration synced)
        {
            MaxPoints = synced.Bind(S, "Max Points", 1024,
                "The most terrain points (one per square metre, shoulders included) one ramp or road may change. A bigger one is refused.",
                acceptableValues: new AcceptableValueRange<int>(16, 8192));
            MaxSlope = synced.Bind(S, "Max Slope", 75f,
                "The steepest slope in degrees a ramp or road may have anywhere along its middle. A steeper one is refused. 75 is the absolute cap.",
                acceptableValues: new AcceptableValueRange<float>(5f, AbsoluteMaxSlope));
            MinWidth = synced.Bind(S, "Min Width", 1f,
                "The narrowest ramp or road in metres. The width follows the brush size (twice its radius) within these limits.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 20f));
            MaxWidth = synced.Bind(S, "Max Width", 20f, "The widest ramp or road in metres.",
                acceptableValues: new AcceptableValueRange<float>(1f, 50f));
            MaxLength = synced.Bind(S, "Max Length", 128f,
                "The longest ramp or road in metres, measured on the map. A longer one is refused.",
                acceptableValues: new AcceptableValueRange<float>(4f, 1024f));
            RefusePastLimit = synced.Bind(S, "Refuse Past Height Limit", true,
                "On: a ramp or road that would raise or dig the ground past the height limits is refused. Off: it is built and the ground stops at the limit.");
            QuickRampAllowed = synced.Bind(S, "Quick Ramp", true,
                "Allows the quick ramp key: a ramp from your feet to the aimed point, built at once.");
        }

        private static void BindShape(SyncedConfiguration synced)
        {
            ShoulderWidth = synced.Bind(S, "Shoulder Width", 2f,
                "Metres beside a ramp or road over which its surface blends into the ground next to it, cutting or filling as needed. 0 leaves a sharp edge.",
                acceptableValues: new AcceptableValueRange<float>(0f, 10f));
            EndBlendLength = synced.Bind(S, "End Blend Length", 3f,
                "Metres past each end over which a ramp built with the blend modifier (or a road or quick ramp set to blend its ends) fades into the ground.",
                acceptableValues: new AcceptableValueRange<float>(0f, 20f));
            SoftJoinLength = synced.Bind(S, "Soft Join Length", 2f,
                "Length in metres of the rounded joins at both ends of the 'straight with soft joins' ramp profile.",
                acceptableValues: new AcceptableValueRange<float>(0.5f, 10f));
            RampPaint = synced.Bind(S, "Ramp Paint", PathPaint.Dirt,
                "The paint a ramp puts on its surface while your paint choice is the entry's own.");
            RoadPaint = synced.Bind(S, "Road Paint", PathPaint.Dirt,
                "The paint the carve key puts on a road while your paint choice is the entry's own. The paved carve key always paves.");
        }

        private static void BindChoices(SyncedConfiguration synced)
        {
            Profile = synced.Bind(S, "Ramp Profile", RampProfile.Straight,
                "Your ramp profile: Straight, SoftJoins (straight with rounded joins), SoftEnds (gentle landings) or SCurve (flat ends, steep middle). The brush's shape key (section 10) cycles it while the ramp entry is selected.",
                synced: false);
            WidthFromCursor = synced.Bind(S, "Width From Cursor", false,
                "On: after the second ramp click, the width follows how far the cursor is to the side of the ramp's middle instead of the brush size.",
                synced: false);
            ClearWhenDeselected = synced.Bind(S, "Clear Points When Deselected", true,
                "On: ramp points and road waypoints are forgotten when you select another entry or put the tool away. They are always forgotten on death and logout.",
                synced: false);
            QuickRampBlendEnds = synced.Bind(S, "Quick Ramp Blends Ends", true,
                "On: the quick ramp blends both of its ends into the ground past them.", synced: false);
            RoadBlendEnds = synced.Bind(S, "Road Blends Ends", true,
                "On: a carved road blends its first and last metres into the ground past its ends.", synced: false);
        }

        private static void BindDisplay(SyncedConfiguration synced)
        {
            WarningSlope = synced.Bind(S, "Warning Slope", 38f,
                "The HUD warns when a ramp or road is steeper than this many degrees. Players slide down ground steeper than 38 degrees.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(1f, AbsoluteMaxSlope));
            CartGreenSlope = synced.Bind(S, "Cart Easy Slope", 20f,
                "Preview segments up to this slope in degrees are green: easy for carts.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(1f, AbsoluteMaxSlope));
            CartYellowSlope = synced.Bind(S, "Cart Hard Slope", 25f,
                "Preview segments up to this slope in degrees are yellow; steeper ones are red: too steep for carts.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(1f, AbsoluteMaxSlope));
            ShowVertices = synced.Bind(S, "Show Changed Points", true,
                "Draws a dot on every terrain point the ramp or road will change: blue where the ground is filled, orange where it is cut, red past the height limit.",
                synced: false);
            PreviewOnTop = synced.Bind(S, "Preview Through Ground", true,
                "Draws the ramp and road preview through the ground, so a line cutting into a hill stays visible.", synced: false);
            QuickRampPreview = synced.Bind(S, "Quick Ramp Preview", true,
                "While the ramp entry is selected and no point is set, faintly shows the ramp the quick ramp key would build.", synced: false);
        }

        private static void BindKeys(SyncedConfiguration synced)
        {
            QuickRampKey = Key(synced, "Quick Ramp Key", new KeyboardShortcut(KeyCode.J),
                "Builds a ramp from your feet to the aimed point at once, while any terrain entry of a terrain tool is selected.");
            CarveKey = Key(synced, "Carve Road Key", new KeyboardShortcut(KeyCode.H), "Carves the planned road with the current paint.");
            CarvePavedKey = Key(synced, "Carve Paved Road Key", new KeyboardShortcut(KeyCode.H, KeyCode.LeftShift), "Carves the planned road paved.");
            RemoveKey = Key(synced, "Remove Last Point Key", new KeyboardShortcut(KeyCode.Backspace),
                "Removes the last ramp point or road waypoint (the undo key does the same first).");
            OneSideModifier = Key(synced, "One Side Modifier", new KeyboardShortcut(KeyCode.LeftControl),
                "Hold while setting a ramp's width or clicking to build it: the whole width goes to the side the cursor is on.");
            BlendEndsModifier = Key(synced, "Blend Ends Modifier", new KeyboardShortcut(KeyCode.LeftAlt),
                "Hold while clicking to build a ramp: its ends blend into the ground past them.");
        }

        private static ConfigEntry<KeyboardShortcut> Key(SyncedConfiguration synced, string key, KeyboardShortcut value, string description)
        {
            return synced.Bind(S, key, value, description, synced: false);
        }
    }
}
