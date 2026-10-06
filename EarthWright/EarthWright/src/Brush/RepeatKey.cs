using System.Collections.Generic;
using BepInEx.Configuration;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Hold-to-repeat for the size, rotation and height keys: true on the frame the key goes down, then again after the
    /// start delay and at the repeat rate while it stays held ("Hold To Repeat", "Repeat Start Delay", "Repeat Rate").
    /// The shortcut's own modifiers must be held; an extra Shift or Ctrl is allowed, since those pick bigger steps.
    /// Nothing fires while text is being typed.
    /// </summary>
    public static class RepeatKey
    {
        private static readonly Dictionary<ConfigEntry<KeyboardShortcut>, float> nextAt = new Dictionary<ConfigEntry<KeyboardShortcut>, float>();

        public static bool Fire(ConfigEntry<KeyboardShortcut> key)
        {
            if (key == null)
                return false;
            KeyboardShortcut shortcut = key.Value;
            // The cheap test first: the typing test and the modifiers only for a key that is held.
            if (shortcut.MainKey == KeyCode.None || !Input.GetKey(shortcut.MainKey) || Keys.TextInputActive || !ModifiersHeld(shortcut))
                return false;
            float now = Time.unscaledTime;
            if (Input.GetKeyDown(shortcut.MainKey))
            {
                nextAt[key] = now + ControlSettings.RepeatDelay.Value;
                return true;
            }
            if (!Input.GetKey(shortcut.MainKey) || !ControlSettings.HoldToRepeat.Value)
                return false;
            if (!nextAt.TryGetValue(key, out float at) || now < at)
                return false;
            nextAt[key] = now + ControlSettings.RepeatRate.Value;
            return true;
        }

        private static bool ModifiersHeld(KeyboardShortcut shortcut)
        {
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!Hotkeys.Hotkey.KeyHeld(modifier))
                    return false;
            }
            return true;
        }
    }
}
