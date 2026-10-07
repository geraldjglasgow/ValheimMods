using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The sort choices of Mímir's Chest in the title row where Take all was (user, 2026-10-07): three small buttons,
    /// Name, Type and Stars, copies of the game's Take all button so they look like the panel's own. The chest's order
    /// (<see cref="MimirSortMode"/>) is lit with the gold outline the quick filters use; a click picks it and sorts.
    /// </summary>
    public static class MimirSortButtons
    {
        private const float Width = 58f;
        private const float Height = 28f;
        private const float Gap = 4f;
        private const float Top = -10f;
        private static readonly Color Picked = new Color(1f, 0.74f, 0.3f, 1f);

        private static readonly Button[] buttons = new Button[3];
        private static GameObject group;
        private static MimirSortMode.Mode shown = (MimirSortMode.Mode)(-1);

        public static void Build(InventoryGui gui, RectTransform panel)
        {
            if (gui.m_takeAllButton == null)
                return;
            shown = (MimirSortMode.Mode)(-1);
            group = new GameObject("OpenKeep_MimirSort", typeof(RectTransform));
            RectTransform rect = (RectTransform)group.transform;
            rect.SetParent(panel, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(MimirLayout.Margin, Top);
            rect.sizeDelta = new Vector2(3 * Width + 2 * Gap, Height);
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = Make(gui, rect, (MimirSortMode.Mode)i, i * (Width + Gap));
        }

        private static Button Make(InventoryGui gui, RectTransform parent, MimirSortMode.Mode mode, float x)
        {
            GameObject go = Object.Instantiate(gui.m_takeAllButton.gameObject, parent, false);
            go.name = "sort_" + mode;
            go.SetActive(true);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(Width, Height);
            foreach (UIGamePad pad in go.GetComponentsInChildren<UIGamePad>(true))
                Object.DestroyImmediate(pad);
            Label(go, mode);
            Button button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => MimirSortMode.Pick(gui.m_currentContainer, mode));
            return button;
        }

        private static void Label(GameObject go, MimirSortMode.Mode mode)
        {
            TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;
            label.text = Language.Localize("$ok_mimir_sort_" + mode.ToString().ToLowerInvariant());
            label.enableAutoSizing = false;
            label.fontSize = 15f;
        }

        /// <summary>Every frame while a Mímir's Chest is open: lights its order.</summary>
        public static void Show(Container open)
        {
            MimirSortMode.Mode mode = MimirSortMode.Of(open);
            if (group == null || mode == shown)
                return;
            shown = mode;
            for (int i = 0; i < buttons.Length; i++)
                Light(buttons[i], i == (int)mode);
        }

        private static void Light(Button button, bool on)
        {
            if (button == null)
                return;
            if (button.image != null)
                button.image.color = on ? Picked : Color.white;
            if (!button.TryGetComponent(out Outline ring))
            {
                ring = button.gameObject.AddComponent<Outline>();
                ring.effectColor = new Color(1f, 0.8f, 0.3f, 1f);
                ring.effectDistance = new Vector2(2f, -2f);
            }
            ring.enabled = on;
        }

        public static void SetActive(bool on)
        {
            if (group != null && group.activeSelf != on)
                group.SetActive(on);
        }
    }
}
