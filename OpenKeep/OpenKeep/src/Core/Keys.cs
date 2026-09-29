using System.Linq;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenKeep.Core
{
    /// <summary>
    /// Hotkey helpers. Nothing counts as pressed while text is being typed (chat, console, the sign text input, a
    /// selected Unity input field, the YAML editor). A shortcut with modifiers is its main key going down this frame
    /// with all its modifiers held and no other Shift, Ctrl or Alt (other keys such as W may be held, so it works while
    /// walking); a single key is the key going down this frame with no Shift, Ctrl or Alt held, so it does not fire
    /// together with a modified shortcut on the same key (F next to LeftShift + F).
    /// </summary>
    public static class Keys
    {
        /// <summary>This frame, modifiers honoured, no text input active.</summary>
        public static bool Pressed(ConfigEntry<KeyboardShortcut> key)
        {
            if (key == null || TextInputActive)
                return false;
            KeyboardShortcut shortcut = key.Value;
            if (shortcut.MainKey == KeyCode.None)
                return false;
            if (HasModifiers(shortcut))
                return Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey) && !OtherModifierHeld(shortcut);
            return Input.GetKeyDown(shortcut.MainKey) && !ModifierHeld(shortcut.MainKey);
        }

        /// <summary>
        /// A Shift, Ctrl or Alt key that is not part of the shortcut is held. BepInEx's own IsDown refuses a shortcut
        /// while any other key at all is held, so LeftAlt + D would never fire while walking with W; only modifiers block here.
        /// </summary>
        private static bool OtherModifierHeld(KeyboardShortcut shortcut)
        {
            foreach (KeyCode modifier in ModifierKeys)
            {
                if (modifier != shortcut.MainKey && !shortcut.Modifiers.Contains(modifier) && Input.GetKey(modifier))
                    return true;
            }
            return false;
        }

        /// <summary>The main key held with its modifiers (for modifier settings such as LeftShift).</summary>
        public static bool Held(ConfigEntry<KeyboardShortcut> key)
        {
            if (key == null || TextInputActive)
                return false;
            KeyboardShortcut shortcut = key.Value;
            if (shortcut.MainKey == KeyCode.None || !Input.GetKey(shortcut.MainKey))
                return false;
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!Input.GetKey(modifier))
                    return false;
            }
            return true;
        }

        public static bool InventoryOpen => InventoryGui.IsVisible();

        public static bool TextInputActive => ChatFocused || Console.IsVisible() || TextInput.IsVisible() || InputFieldSelected || YamlEditorOpen;

        private static bool ChatFocused => Chat.instance != null && Chat.instance.HasFocus();

        private static bool YamlEditorOpen => Plugin.Synced != null && Plugin.Synced.YamlEditor.IsOpen;

        private static bool InputFieldSelected
        {
            get
            {
                EventSystem system = EventSystem.current;
                GameObject selected = system != null ? system.currentSelectedGameObject : null;
                if (selected == null)
                    return false;
                return selected.GetComponent<TMP_InputField>() != null || selected.GetComponent<InputField>() != null;
            }
        }

        private static bool HasModifiers(KeyboardShortcut shortcut) => shortcut.Modifiers.Any();

        private static readonly KeyCode[] ModifierKeys =
        {
            KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt,
        };

        /// <summary>A Shift, Ctrl or Alt key other than the shortcut's own main key is held.</summary>
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
