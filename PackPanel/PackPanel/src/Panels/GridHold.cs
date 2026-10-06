using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Ring;
using PackPanel.Tackle;
using TMPro;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// Keeps the game's <c>InventoryGrid.UpdateGui</c> from undoing, every frame, what PackPanel shows on the player grid
    /// (<see cref="SlotElements"/>): the grid's root sized to the main rows (the game sizes it for every row, slot rows
    /// included), the purse's count alone ("51" where the game writes "51/999"), the Tacklebox slot's "3/6" (the game
    /// hides the count of an item that stacks to one) and the ring's and the tacklebox's cells' counts alone ("3" for
    /// "3/10", none for a single key). Put back after the game every frame, those made the canvas lay out and redraw them
    /// twice a frame. So for the length of the game's method only (a prefix after every other mod's, a postfix before
    /// every other's, and a finalizer should it throw) the root and those cells' count texts are swapped for stand-ins
    /// nobody sees, and PackPanel writes the real ones only when what they show changes. Not while the grid makes its
    /// cells again (a size change: the game parents the new cells to the root), nor with the inventory section off; the
    /// Tacklebox slot's and cells' counts only while the tacklebox is on (<see cref="TackleboxSlot"/>,
    /// <see cref="TackleCells"/>), the ring cells' only while the key ring is (<see cref="KeyRingCells"/>).
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class GridHold
    {
        private static readonly List<InventoryElement> swapped = new List<InventoryElement>();
        private static readonly List<TMP_Text> realAmounts = new List<TMP_Text>();
        private static InventoryGrid held;
        private static RectTransform root;
        private static RectTransform standInRoot;
        private static TMP_Text standInText;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static void Prefix(InventoryGrid __instance)
        {
            Release();
            if (!Holds(__instance) || !StandIns())
                return;
            held = __instance;
            root = __instance.m_gridRoot;
            __instance.m_gridRoot = standInRoot;
            Swap(ElementPlacer.PurseElement);
            if (Tacklebox.Active)
            {
                Swap(ElementPlacer.TackleboxElement);
                for (int number = 1; number <= TackleCells.Count; number++)
                    Swap(TackleCells.At(number));
            }
            if (KeyRing.Active)
            {
                for (int number = 1; number <= KeyRingCells.Count; number++)
                    Swap(KeyRingCells.At(number));
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        public static void Postfix() => Release();

        [HarmonyFinalizer]
        public static void Finalizer() => Release();

        /// <summary>The player grid laid out by PackPanel, keeping its cells this frame (the inventory's size unchanged).</summary>
        private static bool Holds(InventoryGrid grid)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || grid != gui.m_playerGrid || !InventoryState.Active || grid.m_inventory == null)
                return false;
            return grid.m_elements.Count > 0 && grid.m_inventory.GetWidth() == grid.m_width
                && grid.m_inventory.GetHeight() == grid.m_height;
        }

        private static void Swap(InventoryElement element)
        {
            TMP_Text real = element != null ? element.m_amount : null;
            if (real == null)
                return;
            swapped.Add(element);
            realAmounts.Add(real);
            element.m_amount = standInText;
        }

        /// <summary>Everything as it was: the real root and count texts back in their fields.</summary>
        private static void Release()
        {
            if (ReferenceEquals(held, null))
                return;
            held.m_gridRoot = root;
            for (int i = 0; i < swapped.Count; i++)
                swapped[i].m_amount = realAmounts[i];
            swapped.Clear();
            realAmounts.Clear();
            held = null;
            root = null;
        }

        /// <summary>
        /// An inactive object with a rect and a text: writes to them dirty no canvas. Made on first use, again after a
        /// new scene took the old one.
        /// </summary>
        private static bool StandIns()
        {
            if (standInRoot != null && standInText != null)
                return true;
            if (standInRoot != null)
                Object.Destroy(standInRoot.gameObject);
            GameObject go = new GameObject("PackPanel_gridstandin", typeof(RectTransform));
            go.SetActive(false);
            standInRoot = (RectTransform)go.transform;
            standInText = go.AddComponent<TextMeshProUGUI>();
            return standInText != null;
        }
    }
}
