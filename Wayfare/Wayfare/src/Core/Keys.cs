using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace Wayfare.Core
{
    /// <summary>Hotkey polling. A shortcut with modifiers uses BepInEx's own <c>KeyboardShortcut.IsDown</c>; a
    /// single key is the key going down this frame with no Shift, Ctrl or Alt held, so it does not fire together
    /// with a modified shortcut sharing the same main key. Nothing fires while a text field has focus.</summary>
    public static class Keys
    {
        public static bool Pressed(ConfigEntry<KeyboardShortcut> key)
        {
            if (key == null || TextInputActive)
                return false;
            KeyboardShortcut shortcut = key.Value;
            if (shortcut.MainKey == KeyCode.None)
                return false;
            if (shortcut.Modifiers.Any())
                return shortcut.IsDown();
            return Input.GetKeyDown(shortcut.MainKey) && !ModifierHeld(shortcut.MainKey);
        }

        private static bool TextInputActive => Chat.instance != null && Chat.instance.HasFocus();

        private static readonly KeyCode[] ModifierKeys =
        {
            KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt,
        };

        private static bool ModifierHeld(KeyCode except)
        {
            foreach (KeyCode modifier in ModifierKeys)
            {
                if (modifier != except && Input.GetKey(modifier))
                    return true;
            }
            return false;
        }
    }
}
