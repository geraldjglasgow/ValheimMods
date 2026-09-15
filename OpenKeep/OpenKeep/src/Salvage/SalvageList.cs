using System.Collections.Generic;
using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// The left list of the crafting panel while the Salvage tab is selected: one row per salvageable inventory
    /// stack, built from the game's recipe element prefab in the game's list root, with the game's selection
    /// marker, centring and gamepad stepping. The game's own recipe rows are removed while the tab is active.
    /// </summary>
    public static class SalvageList
    {
        private static readonly List<SalvageRow> rows = new List<SalvageRow>();
        private static ItemDrop.ItemData selected;
        private static int selectedIndex;

        public static ItemDrop.ItemData Selected => selected;

        public static int Count => rows.Count;

        /// <summary>Replaces the game's recipe rows with the salvageable stacks and restores the selection.</summary>
        public static void Rebuild(InventoryGui gui, bool center)
        {
            ClearGameRows(gui);
            Clear();
            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItemsInGridOrder())
            {
                if (SalvageActions.CanSalvage(item))
                    rows.Add(CreateRow(gui, item, rows.Count));
            }
            float height = Mathf.Max(gui.m_recipeListBaseSize, rows.Count * gui.m_recipeListSpace);
            gui.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            int index = IndexOf(selected);
            if (index < 0)
                index = Mathf.Clamp(selectedIndex, 0, rows.Count - 1);
            Select(gui, index, center);
        }

        private static void ClearGameRows(InventoryGui gui)
        {
            foreach (InventoryGui.RecipeDataPair pair in gui.m_availableRecipes)
            {
                if (pair.InterfaceElement != null)
                    Object.Destroy(pair.InterfaceElement);
            }
            gui.m_availableRecipes.Clear();
            gui.m_selectedRecipe = default(InventoryGui.RecipeDataPair);
        }

        /// <summary>Destroys our rows. The selection is remembered so a rebuild can restore it.</summary>
        public static void Clear()
        {
            foreach (SalvageRow row in rows)
            {
                if (row.Element != null)
                    Object.Destroy(row.Element);
            }
            rows.Clear();
        }

        private static SalvageRow CreateRow(InventoryGui gui, ItemDrop.ItemData item, int index)
        {
            GameObject element = Object.Instantiate(gui.m_recipeElementPrefab, gui.m_recipeListRoot);
            element.SetActive(true);
            (element.transform as RectTransform).anchoredPosition = new Vector2(0f, -index * gui.m_recipeListSpace);
            Image icon = element.transform.Find("icon").GetComponent<Image>();
            icon.sprite = item.GetIcon();
            icon.color = Color.white;
            TMP_Text name = element.transform.Find("name").GetComponent<TMP_Text>();
            name.text = ItemNames.DisplayName(item) + (item.m_stack > 1 ? $" x{item.m_stack}" : "");
            name.color = Color.white;
            ShowDurability(element, item);
            ShowQuality(element, item);
            element.GetComponent<Button>().onClick.AddListener(() => OnRowClicked(gui, element));
            return new SalvageRow(item, element);
        }

        private static void ShowDurability(GameObject element, ItemDrop.ItemData item)
        {
            GuiBar bar = element.transform.Find("Durability").GetComponent<GuiBar>();
            bool worn = item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability();
            bar.gameObject.SetActive(worn);
            if (worn)
                bar.SetValue(item.GetDurabilityPercentage());
        }

        private static void ShowQuality(GameObject element, ItemDrop.ItemData item)
        {
            TMP_Text quality = element.transform.Find("QualityLevel").GetComponent<TMP_Text>();
            bool show = item.m_shared.m_maxQuality > 1;
            quality.gameObject.SetActive(show);
            if (show)
                quality.text = item.m_quality.ToString();
        }

        private static void OnRowClicked(InventoryGui gui, GameObject element)
        {
            if (gui.m_uiGroups != null && gui.m_uiGroups.Length > 3)
                gui.SetActiveGroup(gui.m_uiGroups[3]);
            if (GamepadRumble.instance != null)
                GamepadRumble.instance.PlayGlobalSelectVibration();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Element == element)
                    Select(gui, i, false);
            }
        }

        public static void Select(InventoryGui gui, int index, bool center)
        {
            selected = null;
            for (int i = 0; i < rows.Count; i++)
            {
                bool on = i == index;
                rows[i].SetSelected(on);
                if (on)
                    selected = rows[i].Item;
            }
            if (selected != null)
                selectedIndex = index;
            if (center && selected != null && gui.m_recipeEnsureVisible != null)
                gui.m_recipeEnsureVisible.CenterOnItem(rows[index].Element.transform as RectTransform);
        }

        /// <summary>Gamepad stepping through the list, like the game's recipe list.</summary>
        public static void Step(InventoryGui gui, int delta)
        {
            if (rows.Count == 0)
                return;
            int index = Mathf.Clamp(IndexOf(selected) + delta, 0, rows.Count - 1);
            Select(gui, index, true);
        }

        private static int IndexOf(ItemDrop.ItemData item)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Item == item)
                    return i;
            }
            return -1;
        }

        /// <summary>The craft button while the tab is active: salvages the selected stack and rebuilds the list.</summary>
        public static void SalvageSelected(InventoryGui gui)
        {
            ItemDrop.ItemData item = selected;
            Player player = Player.m_localPlayer;
            if (item == null || player == null)
                return;
            if (gui.m_uiGroups != null && gui.m_uiGroups.Length > 3)
                gui.SetActiveGroup(gui.m_uiGroups[3]);
            if (!SalvageActions.Salvage(player, item))
                return;
            gui.m_craftItemEffects.Create(player.transform.position, Quaternion.identity);
            gui.UpdateCraftingPanel();
        }
    }
}
