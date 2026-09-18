using BepInEx.Configuration;
using HaloMenu.API;
using UnityEngine;

namespace HaloMenu.Config
{
    // SegmentColor / HighlightColor are stored as "#RRGGBBAA" hex strings, not ConfigEntry<Color>: matches how
    // colors are already bound elsewhere in this workspace (Party.PartyColor), parsed with
    // ColorUtility.TryParseHtmlString at use time.

    /// <summary>The factory-default values a freshly bound <see cref="RingSettings"/> section gets. One instance
    /// is the default ring's own defaults (from the feature sheet); <see cref="HaloMenuServiceImpl.CreateRing"/>
    /// hands every API-created ring a copy so a mod can still edit its own generated config section by hand.</summary>
    public sealed class RingDefaults
    {
        // Fully qualified on the right: a field named the same as its enum type would otherwise shadow the type
        // in simple-name lookup and make "ActivationMode.Hold" resolve as a (nonexistent) member of the field.
        public KeyboardShortcut Hotkey = new KeyboardShortcut(KeyCode.LeftAlt);
        public ActivationMode ActivationMode = HaloMenu.API.ActivationMode.Hold;
        public bool GamepadEnabled = true;
        public GamepadStick GamepadStick = HaloMenu.API.GamepadStick.Right;

        public int SegmentCount = 8;
        public float InnerRadius = 90f;
        public float OuterRadius = 190f;
        public float GapDegrees = 3f;
        public float StartAngleOffset = -90f;
        public float DeadZoneRadius = 40f;
        public float IconPadding = 0.7f;
        public float MaxIconSize = 64f;

        public float HoverScale = 1.08f;
        public float AnimationDuration = 0.08f;
        public string SegmentColor = "#141416CC";
        public string HighlightColor = "#D4AF37FF";
        public bool ShowCenterLabel = true;
        public float UIScale = 1f;

        public static RingDefaults Default() => new RingDefaults();
    }
}
