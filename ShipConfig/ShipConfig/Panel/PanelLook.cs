using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShipConfig
{
    /// <summary>
    /// The ship panel's look, taken from the game's own HUD: the minimap's dark frame and font, the inventory's item
    /// tooltip, the game's key yellow and its highlight orange.
    /// </summary>
    public static class PanelLook
    {
        public const float FontSize = 12f;

        public static readonly Color Label = new Color(0.72f, 0.72f, 0.72f, 1f);
        public static readonly Color Line = new Color(1f, 1f, 1f, 0.14f);
        public static readonly Color Bar = new Color(1f, 0.713f, 0.361f, 0.8f);
        public const string Orange = "#FFB65C";
        public const string Red = "#FF8A80";

        public static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>A flat colour block that never catches the pointer (divider, bar).</summary>
        public static Image Block(string name, Transform parent, Color colour)
        {
            Image image = Node(name, parent).gameObject.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A text filling its parent, created asleep so it never looks for TextMeshPro's missing default font.</summary>
        public static TMP_Text Text(RectTransform parent, string name, TMP_FontAsset font, TextAlignmentOptions align, Color colour)
        {
            RectTransform rect = Node(name, parent);
            rect.gameObject.SetActive(false);
            Stretch(rect);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            rect.gameObject.SetActive(true);
            text.fontSize = FontSize;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.color = colour;
            return text;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>The minimap's own dark frame (a sliced sprite at 40% black), else plain dark.</summary>
        public static void Frame(Image image)
        {
            image.raycastTarget = false;
            image.color = new Color(0f, 0f, 0f, 0.4f);
            Image map = Minimap.instance != null && Minimap.instance.m_smallRoot != null
                ? Minimap.instance.m_smallRoot.GetComponent<Image>()
                : null;
            if (map == null)
                return;
            image.sprite = map.sprite;
            image.type = map.type;
            image.color = map.color;
        }

        /// <summary>The minimap biome name's font (the game's Averia Sans), else TextMeshPro's default.</summary>
        public static TMP_FontAsset Font()
        {
            TMP_Text biome = Minimap.instance != null ? Minimap.instance.m_biomeNameSmall : null;
            return biome != null && biome.font != null ? biome.font : TMP_Settings.defaultFontAsset;
        }

        /// <summary>The tooltip every inventory item slot shows (the game's dark box with a topic and a text), or null.</summary>
        public static GameObject TipPrefab()
        {
            InventoryGui gui = InventoryGui.instance;
            GameObject element = gui != null && gui.m_playerGrid != null ? gui.m_playerGrid.m_elementPrefab : null;
            UITooltip tip = element != null ? element.GetComponent<UITooltip>() : null;
            return tip != null ? tip.m_tooltipPrefab : null;
        }
    }
}
