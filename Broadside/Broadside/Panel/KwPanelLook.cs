using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// The ship panel's look (<see cref="KwShipPanel"/>), taken from the game's own HUD: the minimap's dark frame and font,
    /// the inventory's item tooltip box, the game's highlight orange. <see cref="Orange"/> and <see cref="Red"/> are the
    /// colours the panel's own values use, for other code's lines in rich text.
    /// </summary>
    public static class KwPanelLook
    {
        public const float FontSize = 12f;
        public const string Orange = "#FFB65C", Red = "#FF8A80", Grey = "#B8B8B8";

        public static readonly Color Label = new Color(0.72f, 0.72f, 0.72f, 1f);
        public static readonly Color Line = new Color(1f, 1f, 1f, 0.14f);
        public static readonly Color Bar = new Color(1f, 0.713f, 0.361f, 0.8f);

        internal static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>A flat colour block that never catches the pointer (divider, bar).</summary>
        internal static Image Block(string name, Transform parent, Color colour)
        {
            Image image = Node(name, parent).gameObject.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A text filling its parent, created asleep so it never looks for TextMeshPro's missing default font.</summary>
        internal static TMP_Text Text(RectTransform parent, string name, TMP_FontAsset? font, TextAlignmentOptions align, Color colour)
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

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>The minimap's own dark frame (a sliced sprite at 40% black), else plain dark.</summary>
        internal static void Frame(Image image)
        {
            image.raycastTarget = false;
            image.color = new Color(0f, 0f, 0f, 0.4f);
            Image? map = Minimap.instance != null && Minimap.instance.m_smallRoot != null
                ? Minimap.instance.m_smallRoot.GetComponent<Image>()
                : null;
            if (map == null)
                return;
            image.sprite = map.sprite;
            image.type = map.type;
            image.color = map.color;
        }

        /// <summary>The minimap biome name's font (the game's Averia Sans), else TextMeshPro's default.</summary>
        internal static TMP_FontAsset? Font()
        {
            TMP_Text? biome = Minimap.instance != null ? Minimap.instance.m_biomeNameSmall : null;
            return biome != null && biome.font != null ? biome.font : TMP_Settings.defaultFontAsset;
        }

        /// <summary>The tooltip every inventory item slot shows (the game's dark box with a topic and a text), or null.</summary>
        internal static GameObject? TipPrefab()
        {
            InventoryGui? gui = InventoryGui.instance;
            GameObject? element = gui != null && gui.m_playerGrid != null ? gui.m_playerGrid.m_elementPrefab : null;
            UITooltip? tip = element != null ? element.GetComponent<UITooltip>() : null;
            return tip != null ? tip.m_tooltipPrefab : null;
        }

        /// <summary>A number with that many decimals, the same on every machine ("1.25").</summary>
        internal static string N(float value, int decimals) => value.ToString(decimals == 0 ? "0" : decimals == 1 ? "0.0" : "0.00", CultureInfo.InvariantCulture);

        /// <summary>The text in the panel's orange.</summary>
        internal static string InOrange(string text) => "<color=" + Orange + ">" + text + "</color>";

        /// <summary>The text in the panel's red.</summary>
        internal static string InRed(string text) => "<color=" + Red + ">" + text + "</color>";
    }
}
