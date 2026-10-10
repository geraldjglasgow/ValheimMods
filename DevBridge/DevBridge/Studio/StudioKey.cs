using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevBridge.Studio
{
    /// <summary>
    /// The key that opens the studio page in the player's browser (F3 by default; user request 2026-10-08: "maybe F3 key
    /// opens it"). Nothing happens while the player types: chat, the console, the game's text window or any text box.
    /// </summary>
    internal static class StudioKey
    {
        private static ConfigEntry<KeyCode> key;

        internal static void Bind(ConfigFile config) =>
            key = config.Bind("Studio", "Open key", KeyCode.F3,
                "Opens the studio page (http://127.0.0.1:<port>/studio) in your browser: pick an object near you, move its " +
                "parts, attach effects and play them, or find items and turn them in a viewer. None turns the key off.");

        internal static void Check(int port)
        {
            if (key == null || key.Value == KeyCode.None || port == 0 || !UnityEngine.Input.GetKeyDown(key.Value) || Typing()) return;
            Application.OpenURL($"http://127.0.0.1:{port}/studio");
        }

        private static bool Typing() =>
            (Chat.instance && Chat.instance.HasFocus()) || global::Console.IsVisible() || TextInput.IsVisible() || FieldSelected();

        private static bool FieldSelected()
        {
            GameObject selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            return selected && (selected.GetComponent<TMP_InputField>() || selected.GetComponent<InputField>());
        }
    }
}
