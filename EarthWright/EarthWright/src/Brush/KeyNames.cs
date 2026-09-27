using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>Short, readable names of the configured keys for the controls hint ("Alt", "]", "PgUp", "MMB").</summary>
    public static class KeyNames
    {
        public static string Of(ConfigEntry<KeyboardShortcut> entry)
        {
            if (entry == null || entry.Value.MainKey == KeyCode.None)
                return "-";
            StringBuilder text = new StringBuilder();
            foreach (KeyCode modifier in entry.Value.Modifiers)
                text.Append(Name(modifier)).Append('+');
            return text.Append(Name(entry.Value.MainKey)).ToString();
        }

        /// <summary>The wheel with the adjust modifier, or the plain wheel when no modifier is needed.</summary>
        public static string Wheel()
        {
            bool plain = ControlSettings.PlainWheel.Value || ControlSettings.AdjustModifier.Value.MainKey == KeyCode.None;
            return plain ? BrushWords.Wheel : Of(ControlSettings.AdjustModifier) + "+" + BrushWords.Wheel;
        }

        public static string Name(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftBracket: return "[";
                case KeyCode.RightBracket: return "]";
                case KeyCode.Mouse2: return "MMB";
                case KeyCode.PageUp: return "PgUp";
                case KeyCode.PageDown: return "PgDn";
                case KeyCode.LeftAlt: case KeyCode.RightAlt: return "Alt";
                case KeyCode.LeftControl: case KeyCode.RightControl: return "Ctrl";
                case KeyCode.LeftShift: case KeyCode.RightShift: return "Shift";
                case KeyCode.RightArrow: return "Right";
                case KeyCode.LeftArrow: return "Left";
                default:
                    if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                        return ((int)(key - KeyCode.Alpha0)).ToString();
                    return key.ToString();
            }
        }
    }
}
