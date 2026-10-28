using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hotkeys
{
    /// <summary>
    /// Whether the player is typing, so no hotkey fires: the chat has focus, the console or the game's text input (signs,
    /// tame names) is open, a Unity input field is selected, or a window the mod registered (<see cref="AddWindow"/>, such as
    /// its YAML editor) is open. Every mod merges its own copy, so the registered windows are the mod's own.
    /// </summary>
    public static class Typing
    {
        private static readonly List<Func<bool>> Windows = new List<Func<bool>>();

        public static bool Active => ChatFocused || Console.IsVisible() || TextInput.IsVisible() || InputFieldSelected || WindowOpen;

        /// <summary>A text window of the mod's own (drawn with IMGUI, so no input field shows it) that silences its hotkeys while open.</summary>
        public static void AddWindow(Func<bool> open)
        {
            if (open != null)
            {
                Windows.Add(open);
            }
        }

        private static bool ChatFocused => Chat.instance != null && Chat.instance.HasFocus();

        /// <summary>
        /// An input field that takes the keys now. Selected is not enough: the game's console leaves its input field
        /// selected after it closes, hidden (2026-10-27: every hotkey dead until something else was clicked), and a field
        /// stays selected after its text is submitted.
        /// </summary>
        private static bool InputFieldSelected
        {
            get
            {
                EventSystem system = EventSystem.current;
                GameObject? selected = system != null ? system.currentSelectedGameObject : null;
                if (selected == null || !selected.activeInHierarchy)
                {
                    return false;
                }
                TMP_InputField? field = selected.GetComponent<TMP_InputField>();
                if (field != null)
                {
                    return field.isFocused;
                }
                InputField? legacy = selected.GetComponent<InputField>();
                return legacy != null && legacy.isFocused;
            }
        }

        private static bool WindowOpen
        {
            get
            {
                foreach (Func<bool> open in Windows)
                {
                    if (open())
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
