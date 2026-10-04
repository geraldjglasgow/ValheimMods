using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// Each player's own keys of section "11. Build Camera": the toggle, how fast the camera flies, the head light it
    /// carries and where the pickup panel sits. Bound unsynced; read at use time.
    /// </summary>
    public static class CameraPrefs
    {
        public const string Section = CameraModule.Section;

        public static ConfigEntry<KeyboardShortcut> ToggleKey { get; private set; }
        public static ConfigEntry<string> GamepadToggle { get; private set; }
        public static ConfigEntry<float> Speed { get; private set; }
        public static ConfigEntry<float> RunMultiplier { get; private set; }
        public static ConfigEntry<bool> CircletLight { get; private set; }
        public static ConfigEntry<float> CircletIntensity { get; private set; }
        public static ConfigEntry<float> CircletRange { get; private set; }
        public static ConfigEntry<float> CircletSpotAngle { get; private set; }
        public static ConfigEntry<bool> PickupPanel { get; private set; }
        public static ConfigEntry<Vector2> PickupPanelPosition { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            ToggleKey = synced.Bind(Section, "Toggle Key", new KeyboardShortcut(KeyCode.B),
                "Brings the build camera out and back while a hammer, hoe or cultivator is in hand.", false);
            GamepadToggle = synced.Bind(Section, "Gamepad Toggle", "JoyAltKeys + JoyRStick",
                "The same on a gamepad: the game's button names joined with +, the last pressed while the others are held (by default hold the left trigger and click the right stick). Empty: none.", false);
            Speed = synced.Bind(Section, "Speed", 10f, "Metres per second the camera flies.", false,
                new AcceptableValueRange<float>(1f, 100f));
            RunMultiplier = synced.Bind(Section, "Run Multiplier", 3f, "How much faster the camera flies while Run (Shift) is held.", false,
                new AcceptableValueRange<float>(1f, 10f));
            BindLight(synced);
            PickupPanel = synced.Bind(Section, "Pickup Panel", true,
                "While items lie by the camera and Camera Pickup's comfort needs are not met, a panel near the top of the screen says what is missing.", false);
            PickupPanelPosition = synced.Bind(Section, "Pickup Panel Position", new Vector2(0f, -120f),
                "Where that panel sits: pixels from the top centre of the screen (x right, y up).", false);
        }

        private static void BindLight(SyncedConfiguration synced)
        {
            CircletLight = synced.Bind(Section, "Circlet Light", true,
                "A light worn on the head (the Dvergr circlet, also in another mod's extra slot) shines from the camera too while it is out, with the circlet's own settings unless the three below change them.", false);
            CircletIntensity = synced.Bind(Section, "Circlet Intensity", 0f, "Brightness of the camera's copy. 0: the circlet's own.", false,
                new AcceptableValueRange<float>(0f, 10f));
            CircletRange = synced.Bind(Section, "Circlet Range", 0f, "Metres the camera's copy reaches. 0: the circlet's own.", false,
                new AcceptableValueRange<float>(0f, 100f));
            CircletSpotAngle = synced.Bind(Section, "Circlet Spot Angle", 0f, "Width of the camera's copy's beam in degrees. 0: the circlet's own.", false,
                new AcceptableValueRange<float>(0f, 179f));
        }
    }
}
