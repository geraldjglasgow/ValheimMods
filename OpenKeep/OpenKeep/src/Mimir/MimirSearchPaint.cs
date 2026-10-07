using System.Collections.Generic;
using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// After the container grid has drawn its slots: while a Mímir's Chest is open with a search or a quick filter, only
    /// the stacks that match are shown (user, 2026-10-07: "the filter should just show the options"). They were packed
    /// to the top in rows (<see cref="MimirPack"/>), so the grid shows rows of matches; every other slot, empty or not,
    /// is faded out and lets clicks through (a CanvasGroup per slot, alpha 0). Which stacks match is worked out again
    /// only when the search or any inventory changed (<see cref="InventoryChanges"/>), not per frame. Leaving the search
    /// or the chest shows every slot again. Local player only.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class MimirSearchPaint
    {
        private static readonly HashSet<int> shown = new HashSet<int>();
        private static int seenVersion = -1;
        private static int seenChanges = -1;
        private static bool painted;

        [HarmonyPostfix]
        private static void Postfix(InventoryGrid __instance)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || __instance != gui.m_containerGrid)
                return;
            bool searching = MimirSearch.Sync(gui) && MimirSearch.Narrowed;
            if (searching)
                Paint(__instance);
            else if (painted)
                Restore(__instance);
        }

        private static void Paint(InventoryGrid grid)
        {
            if (Refresh(grid.m_inventory) && grid.m_scrollbar != null)
                grid.m_scrollbar.value = 1f;
            for (int i = 0; i < grid.m_elements.Count; i++)
                Fade(grid.m_elements[i], shown.Contains(i));
            painted = true;
        }

        // The slots holding a match, again only when the search or an inventory changed. True for a new search.
        private static bool Refresh(Inventory inventory)
        {
            if (seenVersion == MimirSearch.Version && seenChanges == InventoryChanges.Count)
                return false;
            bool newSearch = seenVersion != MimirSearch.Version;
            seenVersion = MimirSearch.Version;
            seenChanges = InventoryChanges.Count;
            shown.Clear();
            int width = inventory.GetWidth();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (MimirSearch.Matches(item))
                    shown.Add(item.m_gridPos.y * width + item.m_gridPos.x);
            }
            return newSearch;
        }

        private static void Restore(InventoryGrid grid)
        {
            foreach (InventoryElement element in grid.m_elements)
                Fade(element, true);
            painted = false;
            seenVersion = -1;
        }

        // The grid's slots are made row by row, so a slot's place in the list is its row times the width plus its column.
        private static void Fade(InventoryElement element, bool visible)
        {
            if (element == null)
                return;
            if (!element.TryGetComponent(out CanvasGroup group))
            {
                if (visible)
                    return;
                group = element.gameObject.AddComponent<CanvasGroup>();
            }
            float alpha = visible ? 1f : 0f;
            if (!Mathf.Approximately(group.alpha, alpha))
            {
                group.alpha = alpha;
                group.blocksRaycasts = visible;
                group.interactable = visible;
            }
        }
    }
}
