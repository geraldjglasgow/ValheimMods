using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// The current binding of a key setting as short text for a description ("Ctrl+Z", "Alt", "[", "Middle mouse").
    /// The setting is read by section and key from the plugin's config file, whatever type it was bound with
    /// (a KeyboardShortcut or a plain KeyCode); a setting that does not exist shows its default.
    /// </summary>
    public static class KeyText
    {
        private static readonly Dictionary<KeyCode, string> names = new Dictionary<KeyCode, string>
        {
            { KeyCode.LeftControl, "Ctrl" }, { KeyCode.RightControl, "Ctrl" },
            { KeyCode.LeftShift, "Shift" }, { KeyCode.RightShift, "Shift" },
            { KeyCode.LeftAlt, "Alt" }, { KeyCode.RightAlt, "Alt" },
            { KeyCode.LeftBracket, "[" }, { KeyCode.RightBracket, "]" },
            { KeyCode.LeftArrow, "Left" }, { KeyCode.RightArrow, "Right" },
            { KeyCode.UpArrow, "Up" }, { KeyCode.DownArrow, "Down" },
            { KeyCode.PageUp, "PgUp" }, { KeyCode.PageDown, "PgDn" },
            { KeyCode.Mouse0, "Left mouse" }, { KeyCode.Mouse1, "Right mouse" }, { KeyCode.Mouse2, "Middle mouse" },
            { KeyCode.Backspace, "Backspace" }, { KeyCode.Return, "Enter" },
        };

        /// <summary>The key as text, or null when it is unbound (a hint for an unbound key is left out).</summary>
        public static string Of(KeyRef key)
        {
            KeyboardShortcut shortcut = Read(key);
            if (shortcut.MainKey == KeyCode.None)
                return null;
            IEnumerable<string> parts = shortcut.Modifiers.Select(Name).Append(Name(shortcut.MainKey));
            return string.Join("+", parts.Distinct());
        }

        /// <summary>The bound value of the setting, or its default when the setting is missing or of another type.</summary>
        public static KeyboardShortcut Read(KeyRef key)
        {
            ConfigFile config = Plugin.Synced?.Config;
            ConfigDefinition definition = new ConfigDefinition(key.Section, key.Key);
            if (config == null || !config.ContainsKey(definition))
                return key.Default;
            object value = config[definition].BoxedValue;
            if (value is KeyboardShortcut shortcut)
                return shortcut;
            if (value is KeyCode code)
                return new KeyboardShortcut(code);
            return key.Default;
        }

        private static string Name(KeyCode code)
        {
            if (names.TryGetValue(code, out string name))
                return name;
            if (code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9)
                return ((int)(code - KeyCode.Alpha0)).ToString();
            return code.ToString();
        }
    }
}
