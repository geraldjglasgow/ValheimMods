using BepInEx.Configuration;
using HaloMenu.API;
using Hotkeys;
using UnityEngine;

namespace HaloMenu.Runtime
{
    /// <summary>Hotkey polling and the raw selection offset vector, mouse or gamepad. The keys are read through the
    /// workspace's Hotkeys library: modifiers are honoured without allocating (BepInEx hands them out as a LINQ
    /// sequence), nothing fires while the player types, and Alt is read the way the game reads it. The main key going
    /// down is tested first, so a closed ring costs one key test a frame. Other keys held (W, Shift while running) never
    /// stop a ring from opening.</summary>
    public static class InputSource
    {
        public static bool Pressed(ConfigEntry<KeyboardShortcut> key)
        {
            KeyCode main = key.Value.MainKey;
            return main != KeyCode.None && Input.GetKeyDown(main) && Hotkey.Held(key);
        }

        public static bool Held(ConfigEntry<KeyboardShortcut> key) => Hotkey.Held(key);

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
    }
}
