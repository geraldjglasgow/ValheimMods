using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Look;
using PackPanel.Ring;
using PackPanel.Slots;
using PackPanel.Tackle;
using TMPro;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// After the game draws a grid (<c>InventoryGrid.UpdateGui</c>). The game makes one element per cell of the whole
    /// inventory, slot rows included, and remakes them all when the size changes; every time it does, the elements of
    /// the slot cells move to the slot panel with their captions (the ring cells to the key ring's pop-up), the cells unused by the
    /// last slot row are hidden, the hotbar numbers stop at 8 and every cell gets the skin. Each frame the grid's root is
    /// kept to the main rows (the game sizes it for every row), the gamepad's selection turns to its slot tab, the stat
    /// sheet is brought up to date, the purse shows its count and the key ring's pop-up and button are brought up to
    /// date; the root's size and the purse's count are written only when they change, the game's own writes going to
    /// stand-ins meanwhile (<see cref="GridHold"/>). The container grid only gets the skin.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class SlotElements
    {
        private static InventoryElement placedFirst;
        private static int placedCount;
        private static InventoryElement containerFirst;
        private static int containerVersion;
        private static int placedVersion;
        private static int version;
        private static int purseStack = -1;
        private static string purseText;

        /// <summary>A setting the elements show changed (labels, keys, skin, layout): place them again next frame.</summary>
        public static void Invalidate() => version++;

        [HarmonyPostfix]
        public static void Postfix(InventoryGrid __instance)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null)
                return;
            if (__instance == gui.m_containerGrid)
                SkinContainer(__instance);
            else if (__instance == gui.m_playerGrid)
                KeepPlayer(gui, __instance);
        }

        private static void KeepPlayer(InventoryGui gui, InventoryGrid grid)
        {
            List<InventoryElement> elements = grid.m_elements;
            if (elements.Count == 0)
                return;
            if (elements[0] != placedFirst || elements.Count != placedCount || version != placedVersion)
            {
                placedFirst = elements[0];
                placedCount = elements.Count;
                placedVersion = version;
                ElementPlacer.PlaceAll(gui, grid);
            }
            PanelDress.Update(gui);
            if (!InventoryState.Active)
                return;
            KeepRoot(grid);
            SlotTabs.FollowGamepad(grid);
            GearStats.Refresh();
            ShowCoins(ElementPlacer.PurseElement);
            SlotLabels.Refresh();
            KeyRingPopup.Refresh(gui);
            KeyRingButton.Refresh(gui);
            TacklePopup.Refresh(gui);
            TackleboxSlot.Refresh(ElementPlacer.TackleboxElement);
        }

        /// <summary>The grid's root as tall as the main rows, set only when it is not (the game's own size went to a stand-in).</summary>
        private static void KeepRoot(InventoryGrid grid)
        {
            float rows = InventoryState.Layout.MainRows * grid.m_elementSpace;
            if (!Mathf.Approximately(grid.m_gridRoot.rect.height, rows))
                grid.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rows);
        }

        /// <summary>
        /// The purse shows its count alone ("51"), not the game's "51/999", and shows it when the game would (a stacking
        /// item there): the game wrote to a stand-in (<see cref="GridHold"/>), so both are written here, only when they change.
        /// </summary>
        private static void ShowCoins(InventoryElement purse)
        {
            if (purse == null)
                return;
            ItemDrop.ItemData coins = InventoryState.ItemIn(SlotKind.Purse, 1);
            TMP_Text amount = purse.m_amount;
            bool show = coins != null && coins.m_shared.m_maxStackSize > 1;
            if (amount.enabled != show)
                amount.enabled = show;
            if (coins == null)
                return;
            if (coins.m_stack != purseStack || purseText == null)
            {
                purseStack = coins.m_stack;
                purseText = NumberText.Of(purseStack);
            }
            if (amount.text != purseText)
                amount.text = purseText;
        }

        private static void SkinContainer(InventoryGrid grid)
        {
            List<InventoryElement> elements = grid.m_elements;
            if (elements.Count == 0 || (elements[0] == containerFirst && version == containerVersion))
                return;
            containerFirst = elements[0];
            containerVersion = version;
            foreach (InventoryElement element in elements)
                GridSkin.Cell(element);
        }
    }
}
