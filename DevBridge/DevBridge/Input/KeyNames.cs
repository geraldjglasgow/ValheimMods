using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DevBridge.Input
{
    /// <summary>Key and mouse button names: Input System Key names, most KeyCode names, and a few short aliases.</summary>
    internal static class KeyNames
    {
        private static readonly Dictionary<string, Key> Aliases = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase)
        {
            ["esc"] = Key.Escape, ["return"] = Key.Enter, ["shift"] = Key.LeftShift, ["ctrl"] = Key.LeftCtrl,
            ["control"] = Key.LeftCtrl, ["leftcontrol"] = Key.LeftCtrl, ["rightcontrol"] = Key.RightCtrl,
            ["alt"] = Key.LeftAlt, ["del"] = Key.Delete, ["ins"] = Key.Insert, ["pgup"] = Key.PageUp,
            ["pgdn"] = Key.PageDown, ["up"] = Key.UpArrow, ["down"] = Key.DownArrow, ["left"] = Key.LeftArrow,
            ["right"] = Key.RightArrow, ["tilde"] = Key.Backquote, ["grave"] = Key.Backquote,
            ["keypadenter"] = Key.NumpadEnter, ["leftcommand"] = Key.LeftMeta,
        };

        /// <summary>A list separated by commas or plus signs: "W", "LeftShift+W", "1,2,3".</summary>
        internal static Key[] Parse(string list) =>
            list.Split(',', '+').Select(s => s.Trim()).Where(s => s.Length > 0).Select(One).ToArray();

        private static Key One(string name)
        {
            if (Aliases.TryGetValue(name, out Key alias)) return alias;
            string lower = name.ToLowerInvariant();
            if (name.Length == 1 && char.IsDigit(name[0])) name = "Digit" + name;
            else if (lower.StartsWith("alpha")) name = "Digit" + name.Substring(5);
            else if (lower.StartsWith("keypad")) name = "Numpad" + name.Substring(6);
            if (Enum.TryParse(name, true, out Key key) && key != Key.None) return key;
            throw new BridgeException($"unknown key '{name}': use Input System names (W, Space, LeftShift, Tab, Escape, Enter, Digit1, F5, UpArrow)");
        }

        internal static MouseButton Button(string name)
        {
            switch ((name ?? "left").Trim().ToLowerInvariant())
            {
                case "left": case "0": return MouseButton.Left;
                case "right": case "1": return MouseButton.Right;
                case "middle": case "2": return MouseButton.Middle;
                default: throw new BridgeException($"unknown mouse button '{name}': left, right or middle");
            }
        }
    }
}
