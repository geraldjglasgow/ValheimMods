using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace Hotkeys
{
    /// <summary>
    /// A shortcut read the same way in every mod. With modifiers it is its main key going down this frame with all its
    /// modifiers held and no other Shift, Ctrl or Alt; other keys may be held, so it fires while the player walks with W
    /// (BepInEx's own <c>IsDown</c> refuses a shortcut while any other key at all is held). A single key is the key going
    /// down with no Shift, Ctrl or Alt held, so it never fires together with a modified shortcut on the same key (Z next to
    /// Ctrl + Z). Nothing counts while the player types (<see cref="Typing.Active"/>). No key (None) never fires.
    /// </summary>
    public static class Hotkey
    {
        private static readonly KeyCode[] ModifierKeys =
        {
            KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt,
        };

        /// <summary>Down this frame, modifiers honoured, nothing being typed.</summary>
        public static bool Pressed(ConfigEntry<KeyboardShortcut>? key)
        {
            if (key == null || Typing.Active)
            {
                return false;
            }
            KeyboardShortcut shortcut = key.Value;
            if (shortcut.MainKey == KeyCode.None)
            {
                return false;
            }
            if (shortcut.Modifiers.Any())
            {
                return Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey) && !OtherModifierHeld(shortcut);
            }
            return Input.GetKeyDown(shortcut.MainKey) && !ModifierHeld(shortcut.MainKey);
        }

        /// <summary>The main key held with its modifiers (for modifier settings such as LeftShift), nothing being typed.</summary>
        public static bool Held(ConfigEntry<KeyboardShortcut>? key)
        {
            if (key == null || Typing.Active)
            {
                return false;
            }
            KeyboardShortcut shortcut = key.Value;
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        /// <summary>A Shift, Ctrl or Alt key that is not part of the shortcut is held.</summary>
        private static bool OtherModifierHeld(KeyboardShortcut shortcut)
        {
            foreach (KeyCode modifier in ModifierKeys)
            {
                if (modifier != shortcut.MainKey && !shortcut.Modifiers.Contains(modifier) && Input.GetKey(modifier))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Any Shift, Ctrl or Alt other than the key itself is held.</summary>
        private static bool ModifierHeld(KeyCode self)
        {
            foreach (KeyCode modifier in ModifierKeys)
            {
                if (modifier != self && Input.GetKey(modifier))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
