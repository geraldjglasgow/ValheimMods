using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// One quick filter button (<see cref="MimirFilterBar"/>): the game's button frame (the Take all button's sliced
    /// sprite), the category's item icon or a gold star, a count in the bottom-right corner in the slots' font with an
    /// outline, and the game's tooltip (its prefab borrowed from an inventory slot's).
    /// </summary>
    public static class MimirFilterButton
    {
        private const float Inset = 3f;
        private const float CountSize = 11f;
        private const string Gold = "#FFD157";

        public static Button Make(InventoryGui gui, RectTransform parent, MimirFilter filter, float x)
        {
            Button button = Frame(gui, parent, "filter_" + filter, x);
            Icon((RectTransform)button.transform, filter);
            button.onClick.AddListener(() => MimirSearch.Pick(filter));
            Tooltip(button.gameObject, gui, Language.Localize(MimirFilters.Word(filter)));
            Label(gui, (RectTransform)button.transform, "count", "", CountSize, TextAlignmentOptions.BottomRight);
            return button;
        }

        public static Button MakeStar(InventoryGui gui, RectTransform parent, float x)
        {
            Button button = Frame(gui, parent, "filter_starred", x);
            Label(gui, (RectTransform)button.transform, "star", "<color=" + Gold + ">★</color>", 20f, TextAlignmentOptions.Center);
            button.onClick.AddListener(MimirSearch.ToggleStarred);
            Tooltip(button.gameObject, gui, Language.Localize("$ok_mimir_filter_starred"));
            Label(gui, (RectTransform)button.transform, "count", "", CountSize, TextAlignmentOptions.BottomRight);
            return button;
        }

        public static void SetCount(Button button, int count)
        {
            Transform label = button != null ? button.transform.Find("count") : null;
            if (label != null && label.TryGetComponent(out TMP_Text text))
                text.text = count > 0 ? count.ToString() : "";
        }

        private static Button Frame(InventoryGui gui, RectTransform parent, string name, float x)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(MimirFilterBar.Size, MimirFilterBar.Size);
            Image frame = go.GetComponent<Image>();
            Image template = gui.m_takeAllButton != null ? gui.m_takeAllButton.image : null;
            if (template != null)
            {
                frame.sprite = template.sprite;
                frame.type = template.type;
                frame.material = template.material;
            }
            Button button = go.GetComponent<Button>();
            button.targetGraphic = frame;
            return button;
        }

        private static void Icon(RectTransform button, MimirFilter filter)
        {
            GameObject go = new GameObject("icon", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(button, false);
            Stretch(rect, Inset);
            Image icon = go.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            GameObject item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(MimirFilters.IconItem(filter)) : null;
            Sprite sprite = item != null ? item.GetComponent<ItemDrop>()?.m_itemData.GetIcon() : null;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        private static void Label(InventoryGui gui, RectTransform button, string name, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);   // wakes with its font set (no missing LiberationSans warning)
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(button, false);
            Stretch(rect, 2f);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = SlotFont(gui);
            if (font != null)
                label.font = font;
            label.fontSize = size;
            label.alignment = align;
            label.raycastTarget = false;
            label.text = text;
            go.SetActive(true);
            // The outline needs the text's material, which it only has once awake.
            label.outlineWidth = 0.25f;
            label.outlineColor = Color.black;
        }

        /// <summary>The font of an inventory slot's amount: the game's text font, which falls back to fonts that have ★.</summary>
        private static TMP_FontAsset SlotFont(InventoryGui gui)
        {
            InventoryElement slot = gui.m_playerGrid != null && gui.m_playerGrid.m_elementPrefab != null
                ? gui.m_playerGrid.m_elementPrefab.GetComponent<InventoryElement>()
                : null;
            return slot != null && slot.m_amount != null ? slot.m_amount.font : null;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Tooltip(GameObject go, InventoryGui gui, string text)
        {
            UITooltip slotTip = gui.m_playerGrid != null && gui.m_playerGrid.m_elementPrefab != null
                ? gui.m_playerGrid.m_elementPrefab.GetComponent<UITooltip>()
                : null;
            if (slotTip == null)
                return;
            UITooltip tip = go.AddComponent<UITooltip>();
            tip.m_tooltipPrefab = slotTip.m_tooltipPrefab;
            tip.m_text = text;
            tip.m_topic = "";
        }
    }
}
