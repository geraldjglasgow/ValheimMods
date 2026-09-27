using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>When the camera ignores the mouse wheel while a terrain tool is out.</summary>
    public enum ZoomBlock
    {
        /// <summary>Only while the wheel changes a brush value (the modifier is held, or plain wheel adjusting is on).</summary>
        WhileAdjusting = 0,
        /// <summary>Whenever a terrain entry is selected.</summary>
        Always = 1,
        /// <summary>Never: the camera also zooms while a value changes.</summary>
        Off = 2,
    }

    /// <summary>
    /// Section "10. Controls", the brush keys: every key a player presses to change the brush, the wheel capture,
    /// the camera zoom block and hold-to-repeat. All local: each player picks their own keys.
    /// </summary>
    public static class ControlSettings
    {
        public static ConfigEntry<KeyboardShortcut> AdjustModifier { get; private set; }
        public static ConfigEntry<KeyboardShortcut> FastModifier { get; private set; }
        public static ConfigEntry<bool> PlainWheel { get; private set; }
        public static ConfigEntry<ZoomBlock> ZoomBlocking { get; private set; }
        public static ConfigEntry<KeyboardShortcut> IncreaseKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> DecreaseKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> NextValueKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ShapeKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> RotateRightKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> RotateLeftKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ResetRotationKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SnapKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> GridKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> EdgeKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> StyleKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> PaintKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HardLevelKey { get; private set; }
        public static ConfigEntry<bool> HoldToRepeat { get; private set; }
        public static ConfigEntry<float> RepeatDelay { get; private set; }
        public static ConfigEntry<float> RepeatRate { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindWheel(synced);
            BindBrushKeys(synced);
            BindRepeat(synced);
            TargetKeys.Bind(synced);
            GamepadSettings.Bind(synced);
        }

        private static void BindWheel(SyncedConfiguration synced)
        {
            AdjustModifier = Key(synced, "Adjust Modifier", KeyCode.LeftAlt,
                "Hold this and turn the mouse wheel to change the selected brush value (LeftAlt, LeftControl, LeftShift, ...). Set to None to let the plain wheel change it.");
            FastModifier = Key(synced, "Fast Modifier", KeyCode.LeftControl,
                "Hold this while changing a value for bigger steps (the fast step multiplier; 2 m for the exact target height).");
            PlainWheel = synced.Bind(Sections.Controls, "Plain Wheel Adjusts", false,
                "The mouse wheel alone changes the selected brush value while a terrain entry is selected (the camera then does not zoom).", synced: false);
            ZoomBlocking = synced.Bind(Sections.Controls, "Camera Zoom Block", ZoomBlock.WhileAdjusting,
                "When the camera ignores the mouse wheel while a terrain entry is selected. WhileAdjusting: only while the wheel changes a brush value. Always: the whole time. Off: never (the camera zooms while you change values).",
                synced: false);
        }

        private static void BindBrushKeys(SyncedConfiguration synced)
        {
            IncreaseKey = Key(synced, "Increase Key", KeyCode.RightBracket, "Increases the selected brush value (hold to repeat).");
            DecreaseKey = Key(synced, "Decrease Key", KeyCode.LeftBracket, "Decreases the selected brush value (hold to repeat).");
            NextValueKey = Key(synced, "Select Value Key", KeyCode.B, "Selects the next brush value the wheel and the increase/decrease keys change: size, amount, hardness, rotation, depth, target height.");
            ShapeKey = Key(synced, "Shape Key", KeyCode.N, "Cycles the brush shape: circle, square, rectangle, ring, frame (the ramp tool uses it for its profiles).");
            RotateRightKey = Key(synced, "Rotate Right Key", KeyCode.RightArrow, "Turns the footprint clockwise by the rotation step (hold to repeat).");
            RotateLeftKey = Key(synced, "Rotate Left Key", KeyCode.LeftArrow, "Turns the footprint anticlockwise by the rotation step (hold to repeat).");
            ResetRotationKey = Key(synced, "Reset Rotation Key", KeyCode.Home, "Turns the footprint back to north.");
            SnapKey = Key(synced, "Snap Hold Key", KeyCode.Z, "While held, the brush centre snaps to the world's 1 m grid.");
            GridKey = Key(synced, "Grid Mode Key", KeyCode.I, "Toggles grid mode: centre and size on whole metres, no rotation, full effect on every covered point.");
            EdgeKey = Key(synced, "Aim At Edge Key", KeyCode.O, "Toggles aiming at the edge: the crosshair marks the near edge of the footprint instead of its centre.");
            StyleKey = Key(synced, "Level Style Key", KeyCode.L, "Cycles how levelling approaches the target: Ease, Step, Instant.");
            PaintKey = Key(synced, "Paint Key", KeyCode.P, "Cycles the paint: the entry's own, dirt, paved, cultivated, grass, original, vegetation, clear vegetation, keep.");
            HardLevelKey = Key(synced, "Hard Level Key", KeyCode.F9, "With a level or raise entry selected: one instant, hard-edged level to the current target height (costs as a normal click).");
        }

        private static void BindRepeat(SyncedConfiguration synced)
        {
            HoldToRepeat = synced.Bind(Sections.Controls, "Hold To Repeat", true,
                "Holding the place button keeps applying a brush entry, and holding a size or height key keeps stepping.", synced: false);
            RepeatDelay = synced.Bind(Sections.Controls, "Repeat Start Delay", 0.35f,
                "Seconds a button or key is held before it starts repeating.", synced: false, acceptableValues: new AcceptableValueRange<float>(0.05f, 3f));
            RepeatRate = synced.Bind(Sections.Controls, "Repeat Rate", 0.15f,
                "Seconds between repeats while held.", synced: false, acceptableValues: new AcceptableValueRange<float>(0.05f, 3f));
        }

        internal static ConfigEntry<KeyboardShortcut> Key(SyncedConfiguration synced, string name, KeyCode key, string text)
        {
            return synced.Bind(Sections.Controls, name, new KeyboardShortcut(key), text, synced: false);
        }
    }
}
