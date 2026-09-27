using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>What happens to the game's own ghost visuals (the dust ring and marker pole) of a terrain entry.</summary>
    public enum GhostMode
    {
        /// <summary>The ring grows and shrinks with the brush radius (hidden for shapes other than the circle).</summary>
        Resize,
        /// <summary>The game's ghost visuals are hidden; only EarthWright's outline shows the footprint.</summary>
        Hide,
        /// <summary>The ghost stays exactly as the game draws it.</summary>
        Leave,
    }

    /// <summary>
    /// Section "9. Preview and HUD", first half: the world visuals of the brush (outline, changed points, volume, the
    /// game's ghost). Everything here is a personal display preference, so every key is local (never synced).
    /// </summary>
    public static class PreviewSettings
    {
        public static ConfigEntry<bool> ShowOutline { get; private set; }
        public static ConfigEntry<float> OutlineWidth { get; private set; }
        public static ConfigEntry<Color> OutlineColour { get; private set; }
        public static ConfigEntry<Color> PaintOutlineColour { get; private set; }
        public static ConfigEntry<Color> BlockedColour { get; private set; }
        public static ConfigEntry<Color> OutOfReachColour { get; private set; }
        public static ConfigEntry<GhostMode> GhostVisuals { get; private set; }

        public static ConfigEntry<bool> ShowPoints { get; private set; }
        public static ConfigEntry<int> PointLimit { get; private set; }
        public static ConfigEntry<float> PointSize { get; private set; }
        public static ConfigEntry<Color> RaiseColour { get; private set; }
        public static ConfigEntry<Color> LowerColour { get; private set; }
        public static ConfigEntry<Color> LimitedColour { get; private set; }
        public static ConfigEntry<Color> PaintCellColour { get; private set; }

        public static ConfigEntry<bool> ShowVolume { get; private set; }
        public static ConfigEntry<Color> VolumeColour { get; private set; }
        public static ConfigEntry<Color> VolumeBeyondReachColour { get; private set; }
        public static ConfigEntry<float> VolumeOpacity { get; private set; }

        private static SyncedConfiguration config;

        public static void Bind(SyncedConfiguration synced)
        {
            config = synced;
            BindOutline();
            BindPoints();
            BindVolume();
        }

        private static void BindOutline()
        {
            ShowOutline = Local("Show Outline", true, "Draws the outline of the brush footprint on the ground while a terrain entry is selected.");
            OutlineWidth = Local("Outline Width", 0.08f, "Width of the outline lines in metres.", new AcceptableValueRange<float>(0.01f, 0.5f));
            OutlineColour = Local("Outline Colour", new Color(1f, 0.82f, 0.3f, 0.9f), "Colour of the outline when the click would go through (hex RRGGBBAA; the last pair is the opacity).");
            PaintOutlineColour = Local("Paint Outline Colour", new Color(0.78f, 0.63f, 0.43f, 0.7f), "Colour of the second outline that shows where the paint reaches when it differs from the height footprint.");
            BlockedColour = Local("Blocked Colour", new Color(1f, 0.23f, 0.19f, 0.9f), "Colour of the outline and volume while the click would be refused (a ward, missing materials, a too steep ramp, ...). The reason is shown next to the crosshair.");
            OutOfReachColour = Local("Out Of Reach Colour", new Color(0.62f, 0.62f, 0.62f, 0.7f), "Colour of the outline while the aimed point is beyond your reach.");
            GhostVisuals = Local("Ghost Visuals", GhostMode.Resize, "The game's own ghost ring of a terrain entry: Resize makes it follow the brush radius (hidden for shapes other than the circle), Hide removes it, Leave keeps it as the game draws it.");
        }

        private static void BindPoints()
        {
            ShowPoints = Local("Show Changed Points", true, "Marks every terrain point the click would change (and, for paint-only entries, every painted cell).");
            PointLimit = Local("Point Limit", 4000, "Most points marked at once; larger brushes show the first ones only.", new AcceptableValueRange<int>(100, 20000));
            PointSize = Local("Point Size", 0.15f, "Size of a point marker in metres.", new AcceptableValueRange<float>(0.03f, 0.5f));
            RaiseColour = Local("Raise Colour", new Color(0.3f, 0.9f, 0.35f, 0.9f), "Marker colour of a point that would rise.");
            LowerColour = Local("Lower Colour", new Color(1f, 0.6f, 0.2f, 0.9f), "Marker colour of a point that would sink.");
            LimitedColour = Local("Limited Colour", new Color(1f, 0.2f, 0.33f, 0.9f), "Marker colour of a point held back by a height limit.");
            PaintCellColour = Local("Paint Cell Colour", new Color(0.85f, 0.7f, 0.5f, 0.7f), "Marker colour of a cell a paint-only entry would paint.");
        }

        private static void BindVolume()
        {
            ShowVolume = Local("Show Volume", true, "Draws a see-through body between the current ground and the height the click aims for (level, raise, lower and the admin height operations).");
            VolumeColour = Local("Volume Colour", new Color(0.3f, 0.65f, 1f, 1f), "Colour of the volume while the aimed point is within reach.");
            VolumeBeyondReachColour = Local("Volume Beyond Reach Colour", new Color(1f, 0.4f, 0.2f, 1f), "Colour of the volume while the aimed point is beyond your reach.");
            VolumeOpacity = Local("Volume Opacity", 0.25f, "Opacity of the volume, multiplied with the colours' own opacity (0 invisible, 1 solid).", new AcceptableValueRange<float>(0.02f, 1f));
        }

        private static ConfigEntry<T> Local<T>(string key, T value, string description, AcceptableValueBase range = null)
        {
            return config.Bind(Sections.Preview, key, value, description, synced: false, acceptableValues: range);
        }
    }
}
