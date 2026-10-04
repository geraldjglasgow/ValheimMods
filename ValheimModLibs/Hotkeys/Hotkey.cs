using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Each setting's modifiers as an array, read once: BepInEx hands them out as a LINQ sequence, which makes garbage
        /// on every read, and the keys are read every frame. Dropped when the setting changes.
        /// </summary>
        private static readonly Dictionary<ConfigEntry<KeyboardShortcut>, KeyCode[]> Modifiers =
            new Dictionary<ConfigEntry<KeyboardShortcut>, KeyCode[]>();

        private static readonly HashSet<ConfigEntry<KeyboardShortcut>> Watched = new HashSet<ConfigEntry<KeyboardShortcut>>();

        /// <summary>Down this frame, modifiers honoured, nothing being typed.</summary>
        public static bool Pressed(ConfigEntry<KeyboardShortcut>? key)
        {
            if (key == null || Typing.Active)
            {
                return false;
            }
            KeyCode main = key.Value.MainKey;
            if (main == KeyCode.None)
            {
                return false;
            }
            KeyCode[] modifiers = ModifiersOf(key);
            if (modifiers.Length > 0)
            {
                return Input.GetKeyDown(main) && AllHeld(modifiers) && !OtherModifierHeld(main, modifiers);
            }
            return Input.GetKeyDown(main) && !ModifierHeld(main);
        }

        /// <summary>The main key held with its modifiers (for modifier settings such as LeftShift), nothing being typed.</summary>
        public static bool Held(ConfigEntry<KeyboardShortcut>? key)
        {
            if (key == null || Typing.Active)
            {
                return false;
            }
            KeyCode main = key.Value.MainKey;
            return main != KeyCode.None && Input.GetKey(main) && AllHeld(ModifiersOf(key));
        }

        private static KeyCode[] ModifiersOf(ConfigEntry<KeyboardShortcut> key)
        {
            if (Modifiers.TryGetValue(key, out KeyCode[] modifiers))
            {
                return modifiers;
            }
            if (Watched.Add(key))
            {
                key.SettingChanged += (sender, args) => Modifiers.Remove(key);
            }
            modifiers = key.Value.Modifiers.ToArray();
            Modifiers[key] = modifiers;
            return modifiers;
        }

        private static bool AllHeld(KeyCode[] keys)
        {
            foreach (KeyCode held in keys)
            {
                if (!Input.GetKey(held))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>A Shift, Ctrl or Alt key that is not part of the shortcut is held.</summary>
        private static bool OtherModifierHeld(KeyCode main, KeyCode[] modifiers)
        {
            foreach (KeyCode modifier in ModifierKeys)
            {
                if (modifier != main && Array.IndexOf(modifiers, modifier) < 0 && Input.GetKey(modifier))
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
