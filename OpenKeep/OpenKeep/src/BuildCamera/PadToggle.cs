using System.Linq;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// Gamepad Toggle: the game's own button names (<c>ZInput</c>, so the player's rebinding applies) joined with +; the
    /// last goes down this frame while the others are held. Read only while a gamepad is the active input; an unknown
    /// name never counts as pressed (the game's lookup returns false). The default, hold the left trigger and click the
    /// right stick, is free in the game's default layout: with the trigger held the click hides nothing.
    /// </summary>
    public static class PadToggle
    {
        private static string parsedFrom;
        private static string[] buttons = new string[0];

        public static bool Pressed()
        {
            if (!ZInput.IsGamepadActive())
                return false;
            string[] names = Buttons();
            if (names.Length == 0 || !ZInput.GetButtonDown(names[names.Length - 1]))
                return false;
            for (int i = 0; i < names.Length - 1; i++)
            {
                if (!ZInput.GetButton(names[i]))
                    return false;
            }
            return true;
        }

        /// <summary>The setting split into names, parsed again only when its text changes.</summary>
        private static string[] Buttons()
        {
            string value = CameraPrefs.GamepadToggle.Value ?? "";
            if (value == parsedFrom)
                return buttons;
            parsedFrom = value;
            buttons = value.Split('+').Select(name => name.Trim()).Where(name => name.Length > 0).ToArray();
            return buttons;
        }
    }
}
