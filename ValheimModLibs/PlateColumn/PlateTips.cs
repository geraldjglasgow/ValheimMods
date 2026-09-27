using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// A plate's tooltip is the game's own (<c>UITooltip</c>), using the tooltip prefab the inventory's craft button
    /// already uses, so it looks and behaves like every other tooltip on the screen: it shows after a short hover over
    /// the plate's wood and follows the pointer. Without a prefab to borrow no tooltip is added, since the game's would
    /// fail on hover.
    /// </summary>
    internal static class PlateTips
    {
        public static void Set(InventoryGui gui, RectTransform plate, string topic, string text)
        {
            UITooltip? tip = plate.GetComponent<UITooltip>();
            if (tip == null)
            {
                GameObject? prefab = Prefab(gui);
                if (prefab == null)
                {
                    return;
                }
                tip = plate.gameObject.AddComponent<UITooltip>();
                tip.m_tooltipPrefab = prefab;
            }
            tip.m_topic = topic;
            tip.m_text = text;
        }

        /// <summary>Tips a plate only when it has none: the game's plates, which every mod's copy of this reaches.</summary>
        public static void SetIfMissing(InventoryGui gui, RectTransform plate, string topic, string text)
        {
            if (plate.GetComponent<UITooltip>() == null)
            {
                Set(gui, plate, topic, text);
            }
        }

        private static GameObject? Prefab(InventoryGui gui)
        {
            UITooltip? craft = gui.m_craftButton != null ? gui.m_craftButton.GetComponent<UITooltip>() : null;
            if (craft != null && craft.m_tooltipPrefab != null)
            {
                return craft.m_tooltipPrefab;
            }
            foreach (UITooltip other in gui.GetComponentsInChildren<UITooltip>(true))
            {
                if (other.m_tooltipPrefab != null)
                {
                    return other.m_tooltipPrefab;
                }
            }
            return null;
        }
    }
}
