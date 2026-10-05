using UnityEngine;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The game's own hover tooltips (<see cref="UITooltip"/>, the inventory slots' tooltip prefab) on OpenKeep's crafting
    /// panel parts and on grid tiles. Topic and text are $words or plain text; the game localizes them when shown. The
    /// prefab is a copy made once (<see cref="Template"/>), so the tips stay short and beside the pointer.
    /// </summary>
    public static class RecipeTips
    {
        /// <summary>Epic Loot passes over a tooltip that already has a child of this name (its own scroll box).</summary>
        private const string ScrollMarker = "Scroll View";

        private static GameObject template;

        /// <summary>Adds or updates the tooltip; nothing when the game's tooltip prefab cannot be found.</summary>
        public static UITooltip Set(GameObject go, string topic, string text)
        {
            GameObject prefab = Template();
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

        /// <summary>
        /// The game's prefab copied once under an inactive holder kept across scene loads, with an empty, inactive
        /// "Scroll View" child. Epic Loot turns every tooltip with a Topic and no Scroll View into a scroll box of its
        /// item tooltip size (350 tall at least) set half its width beside the hovered element, so a tile's one-word
        /// name showed far from the tile (2026-10-05). With the marker the game shows it as it shows an item's tooltip,
        /// just below and right of the pointer.
        /// </summary>
        private static GameObject Template()
        {
            if (template != null)
                return template;
            GameObject prefab = GamePrefab();
            if (prefab == null)
                return null;
            GameObject holder = new GameObject("OpenKeep_recipetips");
            holder.SetActive(false);
            Object.DontDestroyOnLoad(holder);
            template = Object.Instantiate(prefab, holder.transform, false);
            template.name = "OpenKeep_recipetip";
            GameObject marker = new GameObject(ScrollMarker);
            marker.SetActive(false);
            marker.transform.SetParent(template.transform, false);
            return template;
        }

        /// <summary>The item slots' tooltip prefab, else any tooltip prefab the panel uses.</summary>
        private static GameObject GamePrefab()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
                return null;
            GameObject element = gui.m_playerGrid != null ? gui.m_playerGrid.m_elementPrefab : null;
            UITooltip tip = element != null ? element.GetComponent<UITooltip>() : null;
            if (tip != null && tip.m_tooltipPrefab != null)
                return tip.m_tooltipPrefab;
            foreach (UITooltip other in gui.GetComponentsInChildren<UITooltip>(true))
            {
                if (other.m_tooltipPrefab != null)
                    return other.m_tooltipPrefab;
            }
            return null;
        }
    }
}
