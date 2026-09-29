using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DevBridge.Input
{
    /// <summary>
    /// Feeds keyboard and mouse state into the game's own devices through the Input System, which is what ZInput and
    /// the UI read. Devices normally switch off while the window is in the background; the first injection keeps them on.
    /// </summary>
    internal static class VirtualInput
    {
        private static readonly HashSet<Key> Held = new HashSet<Key>();
        private static Vector2 pointer;
        private static ushort buttons;
        private static bool pointerPlaced;

        internal static IEnumerable<string> HeldKeys => Held.Select(k => k.ToString()).Concat(HeldButtons());

        internal static void Press(IEnumerable<Key> keys)
        {
            Held.UnionWith(keys);
            SendKeyboard();
        }

        internal static void Release(IEnumerable<Key> keys)
        {
            Held.ExceptWith(keys);
            SendKeyboard();
        }

        internal static void ReleaseAll()
        {
            Held.Clear();
            buttons = 0;
            SendKeyboard();
            SendMouse(Vector2.zero, 0f);
        }

        private static void SendKeyboard() =>
            InputSystem.QueueStateEvent(Ready(Keyboard.current, "keyboard"), new KeyboardState(Held.ToArray()));

        /// <summary>Moves the pointer to a Unity screen point (bottom-left origin).</summary>
        internal static void MoveTo(Vector2 point)
        {
            pointer = point;
            pointerPlaced = true;
            SendMouse(Vector2.zero, 0f);
        }

        internal static void Nudge(Vector2 delta) => SendMouse(delta, 0f);

        internal static void Scroll(float amount) => SendMouse(Vector2.zero, amount);

        internal static void SetButton(MouseButton button, bool down)
        {
            ushort bit = (ushort)(1 << (int)button);
            buttons = down ? (ushort)(buttons | bit) : (ushort)(buttons & ~bit);
            SendMouse(Vector2.zero, 0f);
        }

        private static void SendMouse(Vector2 delta, float scroll)
        {
            Mouse mouse = Ready(Mouse.current, "mouse");
            if (!pointerPlaced) pointer = mouse.position.ReadValue();
            pointerPlaced = true;
            var state = new MouseState { position = pointer, delta = delta, scroll = new Vector2(0f, scroll), buttons = buttons };
            InputSystem.QueueStateEvent(mouse, state);
        }

        internal static Vector2 Pointer => pointer;

        private static T Ready<T>(T device, string what) where T : InputDevice
        {
            if (device == null) throw new BridgeException($"the game has no {what} device");
            if (InputSystem.settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus)
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (!device.enabled) InputSystem.EnableDevice(device);
            return device;
        }

        private static IEnumerable<string> HeldButtons()
        {
            for (int bit = 0; bit < 5; bit++)
                if ((buttons & (1 << bit)) != 0) yield return "Mouse" + (MouseButton)bit;
        }
    }
}
