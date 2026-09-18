using BepInEx.Configuration;
using HaloMenu.API;
using UnityEngine;

namespace HaloMenu.Runtime
{
    /// <summary>Hotkey polling and the raw selection offset vector, mouse or gamepad. A shortcut with modifiers
    /// uses BepInEx's own KeyboardShortcut.IsDown/IsPressed; a bare key is read directly so it keeps working while
    /// the player is moving (WASD is held throughout, which BepInEx's own IsDown would otherwise see as "another
    /// key held" and refuse).</summary>
    public static class InputSource
    {
        public static bool Pressed(ConfigEntry<KeyboardShortcut> key)
        {
            KeyboardShortcut shortcut = key.Value;
            if (shortcut.MainKey == KeyCode.None)
                return false;
            return HasModifiers(shortcut) ? shortcut.IsDown() : Input.GetKeyDown(shortcut.MainKey);
        }

        public static bool Held(ConfigEntry<KeyboardShortcut> key)
        {
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

        public static Vector2 ScreenCenter() => new Vector2(Screen.width, Screen.height) / 2f;

        public static Vector2 MouseOffset() => (Vector2)Input.mousePosition - ScreenCenter();

        /// <summary>The offset to feed into selection this frame: the gamepad stick (scaled to outerRadius, so a
        /// full deflection reaches the ring's outer edge) while a gamepad is active and enabled, else the mouse.</summary>
        public static Vector2 SelectionOffset(bool gamepadEnabled, GamepadStick stick, float outerRadius)
        {
            if (gamepadEnabled && ZInput.IsGamepadActive())
                return StickVector(stick) * outerRadius;
            return MouseOffset();
        }

        private static Vector2 StickVector(GamepadStick stick) => stick == GamepadStick.Left
            ? new Vector2(ZInput.GetJoyLeftStickX(), ZInput.GetJoyLeftStickY())
            : new Vector2(ZInput.GetJoyRightStickX(), ZInput.GetJoyRightStickY());

        private static bool HasModifiers(KeyboardShortcut shortcut)
        {
            foreach (KeyCode _ in shortcut.Modifiers)
                return true;
            return false;
        }
    }
}
