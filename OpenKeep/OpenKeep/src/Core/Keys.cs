using BepInEx.Configuration;
using Hotkeys;
using UnityEngine;

namespace OpenKeep.Core
{
    /// <summary>
    /// OpenKeep's hotkeys, read through the workspace's Hotkeys library (<see cref="Hotkey"/>, <see cref="Typing"/>).
    /// Nothing counts as pressed while text is being typed (chat, console, the sign text input, a selected Unity input
    /// field, the YAML editor). A shortcut with modifiers is its main key going down this frame with all its modifiers
    /// held and no other Shift, Ctrl or Alt (other keys such as W may be held, so it works while walking); a single key
    /// is the key going down this frame with no Shift, Ctrl or Alt held, so it does not fire together with a modified
    /// shortcut on the same key (F next to LeftShift + F). Modifiers are read the way the game reads them (a modifier let
    /// go in another window is not stuck). The main key is looked at first: OpenKeep asks for some twenty keys a frame
    /// while the inventory is open, and the typing check is the slow part.
    /// </summary>
    public static class Keys
    {
        /// <summary>Registers the YAML editor as a window that silences the keys while it is open. Called once at load.</summary>
        public static void Initialize() => Typing.AddWindow(() => Plugin.Synced != null && Plugin.Synced.YamlEditor.IsOpen);

        /// <summary>This frame, modifiers honoured, no text input active.</summary>
        public static bool Pressed(ConfigEntry<KeyboardShortcut> key)
        {
            return key != null && Input.GetKeyDown(key.Value.MainKey) && Hotkey.Pressed(key);
        }

        /// <summary>The main key held with its modifiers (for modifier settings such as LeftShift).</summary>
        public static bool Held(ConfigEntry<KeyboardShortcut> key)
        {
            return key != null && Hotkey.KeyHeld(key.Value.MainKey) && Hotkey.Held(key);
        }

        public static bool InventoryOpen => InventoryGui.IsVisible();

        public static bool TextInputActive => Typing.Active;
    }
}
