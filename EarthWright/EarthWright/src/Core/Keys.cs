using System;
using BepInEx.Configuration;
using Hotkeys;
using UnityEngine;

namespace EarthWright.Core
{
    /// <summary>
    /// EarthWright's hotkeys, read through the shared Hotkeys library: a shortcut with modifiers is its main key going
    /// down this frame with all its modifiers held and no other Shift, Ctrl or Alt (other keys, such as W while walking,
    /// do not block it); a single key is the key going down with no Shift, Ctrl or Alt held, so it does not fire together
    /// with a modified shortcut on the same key (F next to LeftShift + F). Nothing counts as pressed while text is being
    /// typed (chat, console, the sign text input, a selected Unity input field, the YAML editor, the EarthWright panel).
    /// The cheap test of the main key comes first; the typing test runs only for a key that is down.
    /// </summary>
    public static class Keys
    {
        /// <summary>Registers EarthWright's own text windows with the library's typing test (once, at start).</summary>
        internal static void Register()
        {
            Typing.AddWindow(() => Plugin.Synced != null && Plugin.Synced.YamlEditor.IsOpen);
            Typing.AddWindow(() => PanelTyping());
        }

        /// <summary>This frame, modifiers honoured, no text input active.</summary>
        public static bool Pressed(ConfigEntry<KeyboardShortcut> key)
        {
            return key != null && Input.GetKeyDown(key.Value.MainKey) && Hotkey.Pressed(key);
        }

        /// <summary>The main key held with its modifiers (for modifier settings such as LeftShift).</summary>
        public static bool Held(ConfigEntry<KeyboardShortcut> key)
        {
            if (key == null)
                return false;
            KeyCode main = key.Value.MainKey;
            return main != KeyCode.None && Hotkey.KeyHeld(main) && Hotkey.Held(key);
        }

        public static bool InventoryOpen => InventoryGui.IsVisible();

        /// <summary>Set by the Preview module: a text field of the EarthWright panel has the keyboard.</summary>
        public static Func<bool> PanelTyping = () => false;

        public static bool TextInputActive => Typing.Active;
    }
}
