using EarthWright.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EarthWright.Preview
{
    /// <summary>
    /// An "EarthWright" entry in the game's Esc menu, right below Settings, that closes the menu and opens the panel.
    /// It is a copy of the game's own Settings button (same look, sound and layout, read from the menu's
    /// <c>m_settingsButton</c>) with its click replaced; the menu lays its entries out itself. Mouse only: the game's
    /// gamepad navigation list is fixed and not extended.
    /// </summary>
    internal static class EscMenuButton
    {
        private const string Name = "EarthWright";

        public static void Add(global::Menu menu)
        {
            Button settings = menu != null ? menu.m_settingsButton : null;
            Transform parent = settings != null ? settings.transform.parent : null;
            if (parent == null || parent.Find(Name) != null)
                return;
            GameObject copy = Object.Instantiate(settings.gameObject, parent);
            copy.name = Name;
            copy.SetActive(true);
            copy.transform.SetSiblingIndex(settings.transform.GetSiblingIndex() + 1);
            Button button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(OnClick);
            TMP_Text label = copy.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = Language.Localize("$ew_preview_menu_button");
        }

        private static void OnClick()
        {
            Safe.Run("EarthWright menu button", () =>
            {
                global::Menu.instance?.OnClose();
                PanelWindow.Open();
            });
        }
    }

    /// <summary>Adds the button when the in-game menu starts (once per world session; the menu lives in the game scene).</summary>
    [HarmonyPatch(typeof(global::Menu), nameof(global::Menu.Start))]
    public static class EscMenuButtonPatch
    {
        [HarmonyPostfix]
        public static void Postfix(global::Menu __instance) => Safe.Run("EarthWright menu button", () => EscMenuButton.Add(__instance));
    }
}
