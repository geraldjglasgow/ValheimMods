using System;
using System.Collections.Generic;
using HarmonyLib;
using OpenKeep.Core;
using PatchGuard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Stow
{
    /// <summary>
    /// The panel buttons: <c>Quick stack</c>, <c>Stow all</c>, <c>Top up</c>, <c>Sort</c> and the trash can in a
    /// row hanging below the bottom edge of the player panel, centred, and <c>Sort</c> centred below the bottom
    /// edge of the container panel, under the game's take-all line. Nothing inside either panel is free: the player
    /// panel grows exactly one grid row per inventory row (<c>InventoryGui.SetInventorySize</c>), so its last item
    /// row sits on its bottom edge, and the container panel ends with the take-all buttons. Every button is a
    /// clone of the game's take-all button, so style, font and gamepad selection are the game's; the container
    /// panel's button shares the panel's visibility, the player panel's row is part of the panel. The per player
    /// <c>Button Row Offset</c> moves both and is applied at once when it changes.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    public static class PanelButtons
    {
        private const float Width = 78f;
        private const float Height = 26f;
        private const float Gap = 4f;
        private const float BelowEdge = -(Height + Gap);
        private const string Prefix = "OpenKeep_";
        private static readonly List<RectTransform> Placed = new List<RectTransform>();

        /// <summary>The bottom of every button relative to its panel's bottom edge: one row height plus the gap below it, shifted by the setting.</summary>
        private static float Bottom => BelowEdge + (StowSettings.ButtonRowOffset != null ? StowSettings.ButtonRowOffset.Value : 0);

        [HarmonyPostfix]
        public static void Postfix(InventoryGui __instance)
        {
            Guard.Run("stow panel buttons", () => Create(__instance));
        }

        /// <summary>Moves every button that still exists to the current offset; called when the setting changes.</summary>
        public static void Reposition()
        {
            Placed.RemoveAll(rect => rect == null);
            foreach (RectTransform rect in Placed)
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Bottom);
        }

        public static void Create(InventoryGui gui)
        {
            if (gui == null || gui.m_takeAllButton == null || gui.m_player == null || gui.m_container == null)
                return;
            float x = -(2f * Width + 1.5f * Gap) - (Height + Gap) / 2f;
            Add(gui, gui.m_player, StowWords.QuickStack, x, StowActions.QuickStack);
            x += Width + Gap;
            Add(gui, gui.m_player, StowWords.StowAll, x, StowActions.StowAll);
            x += Width + Gap;
            Add(gui, gui.m_player, StowWords.TopUp, x, TopUp.Run);
            x += Width + Gap;
            Add(gui, gui.m_player, StowWords.Sort, x, () => Sorting.SortPlayer(Player.m_localPlayer, false));
            AddTrash(gui, gui.m_player, x + Width / 2f + Gap + Height / 2f);
            Add(gui, gui.m_container, StowWords.Sort, 0f, Sorting.SortOpenContainer);
        }

        private static void Add(InventoryGui gui, RectTransform panel, string word, float centreX, Action action)
        {
            GameObject go = UnityEngine.Object.Instantiate(gui.m_takeAllButton.gameObject, panel);
            go.name = Prefix + word.TrimStart('$');
            go.SetActive(true);
            Place((RectTransform)go.transform, centreX, new Vector2(Width, Height));
            Button button = go.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => Guard.Run(word, action));
            }
            SetLabel(go, word);
        }

        private static void Place(RectTransform rect, float centreX, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(centreX, Bottom);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            Placed.Add(rect);
        }

        private static void SetLabel(GameObject go, string word)
        {
            TMP_Text text = go.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
                return;
            text.text = Language.Localize(word);
            text.enableAutoSizing = true;
            text.fontSizeMax = Mathf.Max(12f, Mathf.Min(text.fontSize, 16f));
            text.fontSizeMin = 9f;
            text.overflowMode = TextOverflowModes.Overflow;
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void AddTrash(InventoryGui gui, RectTransform panel, float centreX)
        {
            GameObject go = new GameObject(Prefix + "trash", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);
            Place((RectTransform)go.transform, centreX, new Vector2(Height, Height));
            Image image = go.GetComponent<Image>();
            image.sprite = StowSprites.Bin;
            image.color = new Color(0.85f, 0.75f, 0.6f, 0.95f);
            image.preserveAspect = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colours = button.colors;
            colours.highlightedColor = new Color(1f, 0.55f, 0.45f, 1f);
            colours.selectedColor = colours.highlightedColor;
            colours.pressedColor = new Color(1f, 0.3f, 0.2f, 1f);
            button.colors = colours;
            button.onClick.AddListener(() => Guard.Run("trash can", () => Trash.TrashDragged(gui)));
        }
    }
}
