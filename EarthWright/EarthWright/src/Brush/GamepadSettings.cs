using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Brush
{
    /// <summary>
    /// Section "10. Controls", the gamepad buttons, by the game's own input names (ZInput). Limited on purpose: the
    /// game uses nearly every button, so EarthWright only listens while the game's alternate-keys button is held.
    /// Local: each player picks their own buttons.
    /// </summary>
    public static class GamepadSettings
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> Modifier { get; private set; }
        public static ConfigEntry<string> ValueUp { get; private set; }
        public static ConfigEntry<string> ValueDown { get; private set; }
        public static ConfigEntry<string> NextValue { get; private set; }
        public static ConfigEntry<string> Shape { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Sections.Controls, "Gamepad Controls", true,
                "Lets a gamepad change the brush while the gamepad modifier is held. Limited: only these four buttons; everything else stays on the keyboard.",
                synced: false);
            Modifier = Button(synced, "Gamepad Modifier", "JoyAltKeys",
                "The gamepad button held to use the four brush buttons (the game's alternate-keys button, the left trigger in the default layout). The camera does not zoom while it is held with a terrain entry selected.");
            ValueUp = Button(synced, "Gamepad Value Up", "JoyDPadUp", "Increases the selected brush value while the gamepad modifier is held.");
            ValueDown = Button(synced, "Gamepad Value Down", "JoyDPadDown", "Decreases the selected brush value while the gamepad modifier is held.");
            NextValue = Button(synced, "Gamepad Select Value", "JoyDPadRight",
                "Selects the next brush value while the gamepad modifier is held (in the default layout this also zooms the minimap in one step).");
            Shape = Button(synced, "Gamepad Shape", "JoyDPadLeft",
                "Cycles the brush shape while the gamepad modifier is held (in the default layout this also zooms the minimap out one step).");
        }

        private static ConfigEntry<string> Button(SyncedConfiguration synced, string key, string value, string text)
        {
            return synced.Bind(Sections.Controls, key, value,
                text + " Uses the game's input names (JoyDPadUp, JoyButtonX, JoyLBumper, ...); empty turns it off.", synced: false);
        }
    }
}
