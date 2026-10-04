using UnityEngine;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The game's own hover tooltips (<see cref="UITooltip"/>, the inventory slots' tooltip prefab) on OpenKeep's crafting
    /// panel parts and on grid tiles. Topic and text are $words or plain text; the game localizes them when shown.
    /// </summary>
    public static class RecipeTips
    {
        /// <summary>Adds or updates the tooltip; nothing when the game's tooltip prefab cannot be found.</summary>
        public static UITooltip Set(GameObject go, string topic, string text)
        {
            GameObject prefab = Prefab();
            if (go == null || prefab == null)
                return null;
            UITooltip tip = go.GetComponent<UITooltip>();
            if (tip == null)
                tip = go.AddComponent<UITooltip>();
            tip.m_tooltipPrefab = prefab;
            tip.m_topic = topic ?? "";
            tip.m_text = text ?? "";
            return tip;
        }

        /// <summary>The item slots' tooltip prefab, else any tooltip prefab the panel uses; found once.</summary>
        private static GameObject Prefab()
        {
            if (prefab != null)
                return prefab;
            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
                return null;
            GameObject element = gui.m_playerGrid != null ? gui.m_playerGrid.m_elementPrefab : null;
            UITooltip tip = element != null ? element.GetComponent<UITooltip>() : null;
            if (tip != null && tip.m_tooltipPrefab != null)
                return prefab = tip.m_tooltipPrefab;
            foreach (UITooltip other in gui.GetComponentsInChildren<UITooltip>(true))
            {
                if (other.m_tooltipPrefab != null)
                    return prefab = other.m_tooltipPrefab;
            }
            return null;
        }

        private static GameObject prefab;
    }
}
