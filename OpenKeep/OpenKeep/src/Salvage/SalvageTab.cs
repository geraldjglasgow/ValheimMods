using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// The third tab button of the crafting panel, cloned from the Upgrade tab and placed after the last visible
    /// game tab. <see cref="Active"/> is the selected state: while it is on, the crafting panel shows the
    /// salvage list instead of recipes (see SalvageGuiPatches). The game's tab buttons keep their own handlers;
    /// pressing one of them deselects this tab.
    /// </summary>
    public static class SalvageTab
    {
        private static TMP_Text label;

        public static bool Active { get; private set; }

        public static Button Button { get; private set; }

        /// <summary>Called from InventoryGui.Awake: clones the Upgrade tab next to itself with a fresh click handler.</summary>
        public static void Create(InventoryGui gui)
        {
            Active = false;
            if (gui == null || gui.m_tabUpgrade == null)
                return;
            Button template = gui.m_tabUpgrade;
            GameObject clone = Object.Instantiate(template.gameObject, template.transform.parent);
            clone.name = "OpenKeep_SalvageTab";
            clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            Button = clone.GetComponent<Button>();
            if (Button == null)
                return;
            Button.onClick = new Button.ButtonClickedEvent();
            Button.onClick.AddListener(() => Select(gui));
            Button.interactable = true;
            label = clone.GetComponentInChildren<TMP_Text>(true);
            Refresh(gui);
        }

        /// <summary>Visibility (the Enabled setting), the label, the selected state and the position; after every panel update.</summary>
        public static void Refresh(InventoryGui gui)
        {
            if (Button == null || gui == null)
                return;
            bool enabled = SalvageSettings.Enabled.Value;
            if (Button.gameObject.activeSelf != enabled)
                Button.gameObject.SetActive(enabled);
            if (!enabled)
                return;
            if (label != null)
                label.text = Core.Language.Localize(SalvageWords.Salvage);
            Button.interactable = !Active;
            Place(gui);
        }

        /// <summary>After the last visible game tab, one tab step to the right (the step is the craft to upgrade distance).</summary>
        private static void Place(InventoryGui gui)
        {
            RectTransform craft = gui.m_tabCraft != null ? gui.m_tabCraft.transform as RectTransform : null;
            RectTransform upgrade = gui.m_tabUpgrade != null ? gui.m_tabUpgrade.transform as RectTransform : null;
            RectTransform mine = Button.transform as RectTransform;
            if (craft == null || upgrade == null || mine == null)
                return;
            Vector2 step = upgrade.anchoredPosition - craft.anchoredPosition;
            if (step.sqrMagnitude < 1f)
                step = new Vector2(upgrade.rect.width, 0f);
            Vector2 last = upgrade.gameObject.activeSelf ? upgrade.anchoredPosition : craft.anchoredPosition;
            mine.anchoredPosition = last + step;
        }

        /// <summary>The click handler: the game tabs become selectable, ours selected, and the panel is rebuilt.</summary>
        public static void Select(InventoryGui gui)
        {
            if (Active || gui == null || !SalvageSettings.Enabled.Value)
                return;
            if (gui.m_uiGroups != null && gui.m_uiGroups.Length > 3)
                gui.SetActiveGroup(gui.m_uiGroups[3]);
            gui.m_tabCraft.interactable = true;
            gui.m_tabUpgrade.interactable = true;
            Active = true;
            gui.UpdateCraftingPanel();
        }

        /// <summary>Called before a game tab handler runs: it sets its own selected state and rebuilds the panel.</summary>
        public static void Deselect()
        {
            if (!Active)
                return;
            Active = false;
            SalvageList.Clear();
        }

        /// <summary>Leaves the tab for the Craft tab, for when the module is switched off while it is selected.</summary>
        public static void ForceCraftTab(InventoryGui gui)
        {
            Deselect();
            gui.m_tabCraft.interactable = false;
            gui.m_tabUpgrade.interactable = true;
        }

        /// <summary>The game's own tab visibility rules (station or not, station without a craft tab), interactable for both.</summary>
        public static void ShowGameTabs(InventoryGui gui, Player player)
        {
            CraftingStation station = player.GetCurrentCraftingStation();
            bool free = player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost);
            bool craft = station == null ? true : station.m_hasCraftTab;
            bool upgrade = station != null || free;
            gui.m_tabCraft.gameObject.SetActive(craft);
            gui.m_tabUpgrade.gameObject.SetActive(upgrade);
            gui.m_tabCraft.interactable = true;
            gui.m_tabUpgrade.interactable = true;
        }
    }
}
