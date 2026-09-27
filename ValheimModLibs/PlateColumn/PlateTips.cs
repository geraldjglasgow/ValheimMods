using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// A plate's tooltip is a <see cref="PlateTip"/> showing the game's inventory item tooltip box (the one every item
    /// slot uses), pinned beside the plate. Without a tooltip prefab to borrow no tooltip is added. Every mod merges its
    /// own copy of this library, so another copy's tip is recognised by its type's name, not its type.
    /// </summary>
    internal static class PlateTips
    {
        public static void Set(InventoryGui gui, RectTransform plate, string topic, string text)
        {
            PlateTip? tip = plate.GetComponent<PlateTip>();
            if (tip == null)
            {
                GameObject? prefab = Prefab(gui);
                if (prefab == null)
                {
                    return;
                }
                tip = plate.gameObject.AddComponent<PlateTip>();
                tip.Prefab = prefab;
            }
            tip.Topic = topic;
            tip.Text = text;
        }

        /// <summary>Tips a plate only when no copy of this library has: the game's plates, which every copy reaches.</summary>
        public static void SetIfMissing(InventoryGui gui, RectTransform plate, string topic, string text)
        {
            foreach (MonoBehaviour behaviour in plate.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().FullName == typeof(PlateTip).FullName)
                {
                    return;
                }
            }
            Set(gui, plate, topic, text);
        }

        /// <summary>The item slots' tooltip prefab (a bordered box with topic and text), else any tooltip prefab the panel uses.</summary>
        private static GameObject? Prefab(InventoryGui gui)
        {
            GameObject? element = gui.m_playerGrid != null ? gui.m_playerGrid.m_elementPrefab : null;
            UITooltip? item = element != null ? element.GetComponent<UITooltip>() : null;
            if (item != null && item.m_tooltipPrefab != null)
            {
                return item.m_tooltipPrefab;
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
