using System.Collections.Generic;
using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// The right side of the crafting panel while the Salvage tab is selected, filled every frame after the game's
    /// UpdateRecipe has cleared it: the selected stack's icon, name and tooltip, the returns in the requirement
    /// rows (amounts always white, more than four rows cycle like the game's), and the craft button as "Salvage".
    /// </summary>
    public static class SalvagePanel
    {
        public static void Fill(InventoryGui gui, Player player)
        {
            if (gui == null || player == null || gui.m_craftTimer >= 0f)
                return;
            TMP_Text buttonText = gui.m_craftButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = Language.Localize(SalvageWords.Salvage);
            ItemDrop.ItemData item = SalvageList.Selected;
            if (item == null)
            {
                ShowEmpty(gui);
                return;
            }
            ShowItem(gui, item);
            List<SalvageReturn> returns = SalvageActions.Returns(item);
            ShowReturns(gui, returns);
            ShowButton(gui, player, item, returns);
        }

        private static void ShowEmpty(InventoryGui gui)
        {
            gui.m_recipeDecription.enabled = true;
            gui.m_recipeDecription.text = Language.Localize(SalvageWords.Empty);
            gui.m_craftButton.interactable = false;
            UITooltip tip = gui.m_craftButton.GetComponent<UITooltip>();
            if (tip != null)
                tip.m_text = "";
        }

        private static void ShowItem(InventoryGui gui, ItemDrop.ItemData item)
        {
            gui.m_recipeIcon.enabled = true;
            gui.m_recipeIcon.sprite = item.GetIcon();
            gui.m_recipeName.enabled = true;
            gui.m_recipeName.text = ItemNames.DisplayName(item) + (item.m_stack > 1 ? $" x{item.m_stack}" : "");
            gui.m_recipeDecription.enabled = true;
            gui.m_recipeDecription.text = Language.Localize(ItemDrop.ItemData.GetTooltip(item, item.m_quality, false, Game.m_worldLevel));
        }

        private static void ShowReturns(InventoryGui gui, List<SalvageReturn> returns)
        {
            GameObject[] slots = gui.m_recipeRequirementList;
            int start = 0;
            if (slots.Length > 0 && returns.Count > slots.Length)
            {
                int pages = Mathf.CeilToInt(returns.Count / (float)slots.Length);
                start = (int)Time.fixedTime % pages * slots.Length;
            }
            int shown = 0;
            for (int k = start; k < returns.Count && shown < slots.Length; k++, shown++)
                ShowReturn(slots[shown].transform, returns[k]);
            for (; shown < slots.Length; shown++)
                InventoryGui.HideRequirement(slots[shown].transform);
        }

        private static void ShowReturn(Transform root, SalvageReturn entry)
        {
            Image icon = root.Find("res_icon").GetComponent<Image>();
            TMP_Text name = root.Find("res_name").GetComponent<TMP_Text>();
            TMP_Text amount = root.Find("res_amount").GetComponent<TMP_Text>();
            icon.gameObject.SetActive(true);
            name.gameObject.SetActive(true);
            amount.gameObject.SetActive(true);
            icon.sprite = entry.Item.m_itemData.GetIcon();
            icon.color = Color.white;
            string label = Language.Localize(entry.Name);
            name.text = label;
            amount.text = entry.Amount.ToString();
            amount.color = Color.white;
            UITooltip tip = root.GetComponent<UITooltip>();
            if (tip != null)
                tip.m_text = label;
        }

        private static void ShowButton(InventoryGui gui, Player player, ItemDrop.ItemData item, List<SalvageReturn> returns)
        {
            string blocker = SalvageRules.Blocker(player, item);
            if (blocker == null && returns.Count == 0)
                blocker = SalvageWords.Nothing;
            bool fits = blocker == null && SalvageInventory.Fits(player.GetInventory(), item, returns);
            gui.m_craftButton.interactable = blocker == null && fits;
            UITooltip tip = gui.m_craftButton.GetComponent<UITooltip>();
            if (tip == null)
                return;
            if (blocker != null)
                tip.m_text = Language.Localize(blocker);
            else
                tip.m_text = fits ? "" : Language.Localize("$inventory_full");
        }
    }
}
