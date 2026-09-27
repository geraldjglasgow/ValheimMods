using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>Where the world grid takes its origin and direction from.</summary>
    public enum GridAnchor
    {
        /// <summary>Lines on the world's own axes, through the world origin (whole metres at spacing 1).</summary>
        World,
        /// <summary>Lines through the last building piece you aimed at, turned with it.</summary>
        AimedPiece,
    }

    /// <summary>
    /// Section "9. Preview and HUD", second half: the text next to the crosshair, the controls hint, the locked badge,
    /// the world grid, piece highlighting, dust, and the panel. All personal preferences and keys, so all local.
    /// </summary>
    public static class HudSettings
    {
        public static ConfigEntry<bool> ShowHud { get; private set; }
        public static ConfigEntry<float> HudScale { get; private set; }
        public static ConfigEntry<float> HudOffsetX { get; private set; }
        public static ConfigEntry<float> HudOffsetY { get; private set; }
        public static ConfigEntry<bool> ShowReadout { get; private set; }
        public static ConfigEntry<bool> ShowHint { get; private set; }
        public static ConfigEntry<float> HintHeight { get; private set; }
        public static ConfigEntry<bool> ShowLockedBadge { get; private set; }

        public static ConfigEntry<KeyboardShortcut> GridKey { get; private set; }
        public static ConfigEntry<float> GridRadius { get; private set; }
        public static ConfigEntry<float> GridSpacing { get; private set; }
        public static ConfigEntry<float> GridLineWidth { get; private set; }
        public static ConfigEntry<Color> GridColour { get; private set; }
        public static ConfigEntry<Color> GridMajorColour { get; private set; }
        public static ConfigEntry<int> GridMajorEvery { get; private set; }
        public static ConfigEntry<GridAnchor> GridAnchorMode { get; private set; }

        public static ConfigEntry<bool> HighlightPieces { get; private set; }
        public static ConfigEntry<bool> RemoveDust { get; private set; }

        public static ConfigEntry<KeyboardShortcut> PanelKey { get; private set; }
        public static ConfigEntry<Vector2> PanelPosition { get; private set; }
        public static ConfigEntry<float> PanelScale { get; private set; }
        public static ConfigEntry<string> RaisePresets { get; private set; }

        private static SyncedConfiguration config;

        public static void Bind(SyncedConfiguration synced)
        {
            config = synced;
            BindHud();
            BindGrid();
            BindOther();
        }

        private static void BindHud()
        {
            ShowHud = Local("Show HUD", true, "Shows EarthWright's lines (brush, target, costs, points) next to the crosshair while a terrain tool is out.");
            HudScale = Local("HUD Scale", 1f, "Size of the text next to the crosshair, the controls hint and the badge.", new AcceptableValueRange<float>(0.5f, 3f));
            HudOffsetX = Local("HUD Offset X", 40f, "Horizontal distance of the text block from the crosshair, in pixels (negative: to the left).", new AcceptableValueRange<float>(-2000f, 2000f));
            HudOffsetY = Local("HUD Offset Y", 24f, "Vertical distance of the text block from the crosshair, in pixels (negative: upward).", new AcceptableValueRange<float>(-2000f, 2000f));
            ShowReadout = Local("Show Cursor Readout", true, "Adds a line with the tile coordinate, the ground height under the crosshair (and how far it is from the world's original height) and the target height.");
            ShowHint = Local("Show Controls Hint", true, "Shows the controls of the selected entry at the bottom of the screen, above the build bar.");
            HintHeight = Local("Hint Height", 190f, "Distance of the controls hint from the bottom edge of the screen, in pixels.", new AcceptableValueRange<float>(0f, 2000f));
            ShowLockedBadge = Local("Show Locked Badge", true, "Shows a small note while the server has locked EarthWright's settings.");
        }

        private static void BindGrid()
        {
            GridKey = Local("World Grid Key", new KeyboardShortcut(KeyCode.F8), "Shows or hides the world grid on the ground around the crosshair while building.");
            GridRadius = Local("World Grid Radius", 10f, "How far around the crosshair the world grid reaches, in metres.", new AcceptableValueRange<float>(2f, 50f));
            GridSpacing = Local("World Grid Spacing", 1f, "Distance between two grid lines in metres.", new AcceptableValueRange<float>(0.25f, 10f));
            GridLineWidth = Local("World Grid Line Width", 0.04f, "Width of a grid line in metres.", new AcceptableValueRange<float>(0.01f, 0.3f));
            GridColour = Local("World Grid Colour", new Color(1f, 1f, 1f, 0.35f), "Colour of the grid lines.");
            GridMajorColour = Local("World Grid Major Colour", new Color(1f, 0.82f, 0.3f, 0.6f), "Colour of every major grid line.");
            GridMajorEvery = Local("World Grid Major Every", 5, "Every how many lines a major line is drawn; 0 draws none.", new AcceptableValueRange<int>(0, 50));
            GridAnchorMode = Local("World Grid Anchor", GridAnchor.World, "World: the grid follows the world's axes. AimedPiece: the grid runs through the last building piece you aimed at and turns with it.");
        }

        private static void BindOther()
        {
            HighlightPieces = Local("Highlight Pieces", true, "Highlights building pieces standing inside the brush footprint.");
            RemoveDust = Local("Remove Dust", false, "Removes the dust and pebble effects of the hoe, the cultivator and the shovel on terrain entries (the sounds stay). Other players then see no dust from your tool either.");
            PanelKey = Local("Panel Key", new KeyboardShortcut(KeyCode.F6), "Opens or closes the EarthWright panel with typed brush values, presets and module settings (also in the Esc menu).");
            PanelPosition = Local("Panel Position", new Vector2(60f, 120f), "Where the panel was last dragged to (pixels from the top left corner).");
            PanelScale = Local("Panel Scale", 1f, "Size of the panel.", new AcceptableValueRange<float>(0.5f, 3f));
            RaisePresets = Local("Raise Presets", "1x2, 5x2, 5x3, 8x3", "Preset buttons of the panel, each 'amount x radius' in metres, separated by commas. A click sets the raise or lower amount and the brush radius.");
        }

        private static ConfigEntry<T> Local<T>(string key, T value, string description, AcceptableValueBase range = null)
        {
            return config.Bind(Sections.Preview, key, value, description, synced: false, acceptableValues: range);
        }
    }
}
