using BepInEx.Configuration;
using HaloMenu.API;

namespace HaloMenu.Config
{
    /// <summary>
    /// One ring's live config entries: Input, Layout and Visual, each read at use time (never cached), so a
    /// change from the ConfigurationManager UI or a file reload takes effect on the ring's next open. The default
    /// ring binds under the bare section names from the feature sheet ("Input", "Layout", "Visual"); a ring made
    /// through <see cref="HaloMenu.API.HaloMenuAPI.CreateRing"/> gets its own subsection, named after its ring id,
    /// so two rings never collide on the same key.
    /// </summary>
    public sealed class RingSettings
    {
        public ConfigEntry<KeyboardShortcut> Hotkey;
        public ConfigEntry<ActivationMode> ActivationMode;
        public ConfigEntry<bool> GamepadEnabled;
        public ConfigEntry<GamepadStick> GamepadStick;

        public ConfigEntry<int> SegmentCount;
        public ConfigEntry<float> InnerRadius;
        public ConfigEntry<float> OuterRadius;
        public ConfigEntry<float> GapDegrees;
        public ConfigEntry<float> StartAngleOffset;
        public ConfigEntry<float> DeadZoneRadius;
        public ConfigEntry<float> IconPadding;
        public ConfigEntry<float> MaxIconSize;

        public ConfigEntry<float> HoverScale;
        public ConfigEntry<float> AnimationDuration;
        public ConfigEntry<string> SegmentColor;
        public ConfigEntry<string> HighlightColor;
        public ConfigEntry<bool> ShowCenterLabel;
        public ConfigEntry<float> UIScale;

        public static RingSettings Bind(ConfigFile config, string ringLabel, RingDefaults d)
        {
            RingSettings s = new RingSettings();
            string input = SectionName("Input", ringLabel);
            string layout = SectionName("Layout", ringLabel);
            string visual = SectionName("Visual", ringLabel);
            s.BindInput(config, input, d);
            s.BindLayout(config, layout, d);
            s.BindVisual(config, visual, d);
            return s;
        }

        private static string SectionName(string baseName, string ringLabel) =>
            string.IsNullOrEmpty(ringLabel) ? baseName : $"{baseName} ({ringLabel})";

        private void BindInput(ConfigFile c, string s, RingDefaults d)
        {
            Hotkey = c.Bind(s, "Hotkey", d.Hotkey, "Hold (or press, in Toggle mode) to open this ring.");
            ActivationMode = c.Bind(s, "ActivationMode", d.ActivationMode,
                "Hold: hold to open, release to select. Toggle: press to open, press again or left-click to select, right-click or Escape to cancel.");
            GamepadEnabled = c.Bind(s, "GamepadEnabled", d.GamepadEnabled, "Feed the gamepad stick into selection the same way the mouse does.");
            GamepadStick = c.Bind(s, "GamepadStick", d.GamepadStick, "Which analogue stick drives selection while a gamepad is active.");
        }

        private void BindLayout(ConfigFile c, string s, RingDefaults d)
        {
            SegmentCount = c.Bind(s, "SegmentCount", d.SegmentCount, new ConfigDescription(
                "How many segments the ring is divided into.", new AcceptableValueRange<int>(2, 16)));
            InnerRadius = c.Bind(s, "InnerRadius", d.InnerRadius, new ConfigDescription(
                "Pixels, at 1080p reference height.", new AcceptableValueRange<float>(20f, 400f)));
            OuterRadius = c.Bind(s, "OuterRadius", d.OuterRadius, new ConfigDescription(
                "Pixels, at 1080p reference height. Must stay above InnerRadius; an invalid pair is clamped and logged.",
                new AcceptableValueRange<float>(40f, 600f)));
            GapDegrees = c.Bind(s, "GapDegrees", d.GapDegrees, new ConfigDescription(
                "Visual gap between segments. Selection still uses the full segment width.", new AcceptableValueRange<float>(0f, 20f)));
            StartAngleOffset = c.Bind(s, "StartAngleOffset", d.StartAngleOffset, new ConfigDescription(
                "Degrees. -90 puts a segment center at 12 o'clock.", new AcceptableValueRange<float>(0f, 360f)));
            DeadZoneRadius = c.Bind(s, "DeadZoneRadius", d.DeadZoneRadius, new ConfigDescription(
                "Pixels. Release inside this radius cancels.", new AcceptableValueRange<float>(0f, 200f)));
            IconPadding = c.Bind(s, "IconPadding", d.IconPadding, new ConfigDescription(
                "Fraction of the fitted icon square actually filled by the icon.", new AcceptableValueRange<float>(0.3f, 1.0f)));
            MaxIconSize = c.Bind(s, "MaxIconSize", d.MaxIconSize, new ConfigDescription(
                "Pixels, at 1080p reference height. Caps icon size on rings with very few segments.", new AcceptableValueRange<float>(16f, 256f)));
        }

        private void BindVisual(ConfigFile c, string s, RingDefaults d)
        {
            HoverScale = c.Bind(s, "HoverScale", d.HoverScale, new ConfigDescription(
                "How much a hovered segment grows, about its own centroid.", new AcceptableValueRange<float>(1.0f, 1.5f)));
            AnimationDuration = c.Bind(s, "AnimationDuration", d.AnimationDuration, new ConfigDescription(
                "Seconds. 0 snaps hover feedback instantly.", new AcceptableValueRange<float>(0f, 0.5f)));
            SegmentColor = c.Bind(s, "SegmentColor", d.SegmentColor, "Segment fill, \"#RRGGBBAA\".");
            HighlightColor = c.Bind(s, "HighlightColor", d.HighlightColor, "Hover fill lift and rim highlight, \"#RRGGBBAA\".");
            ShowCenterLabel = c.Bind(s, "ShowCenterLabel", d.ShowCenterLabel, "Show the hovered entry's label at ring center.");
            UIScale = c.Bind(s, "UIScale", d.UIScale, new ConfigDescription(
                "Extra scale on top of Valheim's own UI scale setting.", new AcceptableValueRange<float>(0.5f, 2.0f)));
        }
    }
}
