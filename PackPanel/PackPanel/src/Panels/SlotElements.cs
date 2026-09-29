using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Look;
using PackPanel.Ring;
using PackPanel.Slots;
using PackPanel.Tackle;
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
    /// date. The container grid only gets the skin.
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
            grid.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, InventoryState.Layout.MainRows * grid.m_elementSpace);
            SlotTabs.FollowGamepad(grid);
            GearStats.Refresh();
            ShowCoins(ElementPlacer.PurseElement);
            SlotLabels.Refresh();
            KeyRingPopup.Refresh(gui);
            KeyRingButton.Refresh(gui);
            TacklePopup.Refresh(gui);
            TackleboxSlot.Refresh(ElementPlacer.TackleboxElement);
        }

        /// <summary>The purse shows its count alone ("51"), not the game's "51/999".</summary>
        private static void ShowCoins(InventoryElement purse)
        {
            ItemDrop.ItemData coins = purse != null ? InventoryState.ItemIn(SlotKind.Purse, 1) : null;
            if (coins != null)
                purse.m_amount.text = coins.m_stack.ToString();
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
