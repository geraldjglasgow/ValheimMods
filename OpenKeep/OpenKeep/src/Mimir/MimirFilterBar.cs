using System.Collections.Generic;
using OpenKeep.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The quick filter buttons right of the search field (<see cref="MimirFilter"/>), ending at the toolbar row's right
    /// edge: one square button per category with the game's own item icon in full colour on the game's button frame, a
    /// small count of the chest's stacks in that category in its corner, its name as a tooltip; then a gold star that
    /// shows only starred stacks. A click picks a category, clicking it again shows everything; the star toggles on its
    /// own and combines with a category. The picked buttons get a gold frame. Counts are worked out again only when an
    /// inventory or the chest changed.
    /// </summary>
    public static class MimirFilterBar
    {
        public const float Size = 34f;
        public const float Gap = 3f;
        public static float Width => (MimirFilters.Buttons.Length + 1) * (Size + Gap) - Gap;

        private static readonly Color Picked = new Color(1f, 0.74f, 0.3f, 1f);
        private static readonly Color Idle = new Color(0.82f, 0.82f, 0.82f, 1f);
        private static readonly List<Button> buttons = new List<Button>();
        private static Button star;
        private static GameObject bar;
        private static (MimirFilter Filter, bool Starred) shown = ((MimirFilter)(-1), false);
        private static int countedChanges = -1;
        private static Inventory counted;

        public static void Build(InventoryGui gui, RectTransform panel, Vector2 topLeft)
        {
            buttons.Clear();
            shown = ((MimirFilter)(-1), false);
            countedChanges = -1;
            bar = new GameObject("OpenKeep_MimirFilters", typeof(RectTransform));
            RectTransform rect = (RectTransform)bar.transform;
            rect.SetParent(panel, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = new Vector2(Width, Size);
            for (int i = 0; i < MimirFilters.Buttons.Length; i++)
                buttons.Add(MimirFilterButton.Make(gui, rect, MimirFilters.Buttons[i], i * (Size + Gap)));
            star = MimirFilterButton.MakeStar(gui, rect, MimirFilters.Buttons.Length * (Size + Gap));
        }

        /// <summary>Every frame while the chest is open: the picked frames and, when anything changed, the counts.</summary>
        public static void Show(Inventory inventory, MimirFilter filter, bool starred)
        {
            if (bar == null)
                return;
            if ((filter, starred) != shown)
            {
                shown = (filter, starred);
                for (int i = 0; i < buttons.Count; i++)
                    Light(buttons[i], MimirFilters.Buttons[i] == filter);
                Light(star, starred);
            }
            if (inventory != counted || InventoryChanges.Count != countedChanges)
                Count(inventory);
        }

        /// <summary>A picked button: its frame tinted gold and a gold outline round it; the others plain.</summary>
        private static void Light(Button button, bool on)
        {
            if (button == null || button.image == null)
                return;
            button.image.color = on ? Picked : Idle;
            if (!button.TryGetComponent(out Outline ring))
            {
                ring = button.gameObject.AddComponent<Outline>();
                ring.effectColor = new Color(1f, 0.8f, 0.3f, 1f);
                ring.effectDistance = new Vector2(2f, -2f);
            }
            ring.enabled = on;
        }

        private static void Count(Inventory inventory)
        {
            counted = inventory;
            countedChanges = InventoryChanges.Count;
            int[] counts = new int[MimirFilters.Buttons.Length + 1];
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                int index = System.Array.IndexOf(MimirFilters.Buttons, MimirFilters.Of(item));
                if (index >= 0)
                    counts[index]++;
                if (MimirFilters.IsStarred(item))
                    counts[counts.Length - 1]++;
            }
            for (int i = 0; i < buttons.Count; i++)
                MimirFilterButton.SetCount(buttons[i], counts[i]);
            MimirFilterButton.SetCount(star, counts[counts.Length - 1]);
        }

        public static void SetActive(bool on)
        {
            if (bar != null && bar.activeSelf != on)
                bar.SetActive(on);
        }
    }
}
