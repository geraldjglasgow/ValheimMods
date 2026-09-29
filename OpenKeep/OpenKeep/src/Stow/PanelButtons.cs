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
    /// The panel buttons: <c>Quick stack</c>, <c>Store all</c>, <c>Top up</c> and <c>Sort</c> in a row hanging
    /// below the bottom edge of the player panel, centred, and <c>Sort</c> centred below the bottom edge of the
    /// container panel, under the game's take-all line. The trash can gets its own plate in the column of stat plates
    /// on the panel's right, between the armour and the weight readouts (<see cref="TrashPlate"/>), and only when that
    /// fails, or while PackPanel lays the inventory out (its stat panel has no room for it), does it join the row.
    /// Nothing inside either panel is free: the player panel grows exactly one grid row per inventory row
    /// (<c>InventoryGui.SetInventorySize</c>), so its last item row sits on its bottom edge, and the container panel ends
    /// with the take-all buttons. PackPanel keeps a strip free at the player panel's bottom instead and marks it with an
    /// object named <see cref="PackPanelStrip"/>; while that is shown the row sits inside it (<see cref="Follow"/>). Every
    /// button is a clone of the game's take-all button, so style, font and gamepad selection are the game's; the container
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

        /// <summary>PackPanel's mark on the player panel: active while the strip at its bottom is kept for this row.</summary>
        private const string PackPanelStrip = "PackPanel_buttonstrip";

        /// <summary>Inside PackPanel's strip (30 high): 2 above the panel's bottom edge.</summary>
        private const float InsideStrip = 2f;
        private const string Prefix = "OpenKeep_";
        private static readonly List<RectTransform> Placed = new List<RectTransform>();
        private static bool rowInside;

        /// <summary>The setting's shift, positive up.</summary>
        private static float Offset => StowSettings.ButtonRowOffset != null ? StowSettings.ButtonRowOffset.Value : 0;

        /// <summary>
        /// The bottom of a button relative to its panel's bottom edge, shifted by the setting: below the edge (one row
        /// height plus the gap), or on the player panel inside the strip PackPanel keeps.
        /// </summary>
        private static float BottomOf(RectTransform rect)
        {
            bool inside = rowInside && InventoryGui.instance != null && rect.parent == InventoryGui.instance.m_player;
            return (inside ? InsideStrip : BelowEdge) + Offset;
        }

        /// <summary>
        /// Every frame of the inventory's update (<see cref="StowHotkeys"/>): the row moves into PackPanel's strip when
        /// the strip appears and back below the panel when it goes, as PackPanel starts or stops laying the panel out.
        /// </summary>
        public static void Follow(InventoryGui gui)
        {
            bool inside = StripShown(gui);
            if (inside == rowInside)
                return;
            rowInside = inside;
            Reposition();
        }

        private static bool StripShown(InventoryGui gui)
        {
            if (!PackPanelLink.Present || gui == null || gui.m_player == null)
                return false;
            Transform strip = gui.m_player.Find(PackPanelStrip);
            return strip != null && strip.gameObject.activeSelf;
        }

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
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, BottomOf(rect));
        }

        public static void Create(InventoryGui gui)
        {
            if (gui == null || gui.m_takeAllButton == null || gui.m_player == null || gui.m_container == null)
                return;
            // While PackPanel lays the inventory out, the stat boxes sit in its stats panel and the trash can joins this row.
            bool onPlate = !PackPanelLink.LaysOutInventory && TrashPlate.TryCreate(gui, () => TrashMode.CanClicked(gui));
            float x = -(1.5f * Width + 1.5f * Gap) - (onPlate ? 0f : (Height + Gap) / 2f);
            Add(gui, gui.m_player, StowWords.QuickStack, x, StowActions.QuickStack);
            x += Width + Gap;
            Add(gui, gui.m_player, StowWords.StoreAll, x, StowActions.StoreAll);
            x += Width + Gap;
            Add(gui, gui.m_player, StowWords.TopUp, x, TopUp.Run);
            x += Width + Gap;
            Add(gui, gui.m_player, StowWords.Sort, x, () => Sorting.SortPlayer(Player.m_localPlayer, false));
            if (!onPlate)
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
            rect.anchoredPosition = new Vector2(centreX, BottomOf(rect));
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
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = new Vector4(6f, 1f, 6f, 1f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void AddTrash(InventoryGui gui, RectTransform panel, float centreX)
        {
            // Not "OpenKeep_trash": the PlateColumn library adopts an object of that name (the pre-library trash plate)
            // into its column of boxes.
            GameObject go = UnityEngine.Object.Instantiate(gui.m_takeAllButton.gameObject, panel);
            go.name = Prefix + "trashcan";
            go.SetActive(true);
            Place((RectTransform)go.transform, centreX, new Vector2(Height, Height));
            foreach (TMP_Text text in go.GetComponentsInChildren<TMP_Text>(true))
                text.gameObject.SetActive(false);
            BinIcon(go.transform);
            Button button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Guard.Run("trash can", () => TrashMode.CanClicked(gui)));
        }

        /// <summary>The bin inside the vanilla button frame; the frame keeps the game's hover and press states.</summary>
        private static Image BinIcon(Transform button)
        {
            GameObject go = new GameObject("icon", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(button, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            Image image = go.GetComponent<Image>();
            image.sprite = StowSprites.Bin;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
