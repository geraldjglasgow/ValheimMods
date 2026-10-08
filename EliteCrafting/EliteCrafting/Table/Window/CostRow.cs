using System.Collections.Generic;
using UnityEngine;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// The copied requirement slots: what the next press costs (a rune, an essence, a trophy), shown the way the crafting
    /// panel shows a recipe's materials, on the button's row (<see cref="PanelLayout"/>). Only <see cref="Most"/> are kept
    /// and only the ones a press needs show (user 2026-10-07: "only have the max number of boxes needed"); the
    /// station-level slot beside them is hidden.
    /// </summary>
    internal sealed class CostRow
    {
        /// <summary>The most a press costs: a rune and an essence.</summary>
        public const int Most = 2;

        private readonly List<Slot> _slots = new List<Slot>();

        public CostRow(RectTransform? requirements)
        {
            if (requirements == null)
            {
                return;
            }
            foreach (Transform child in requirements)
            {
                if (child.name.StartsWith("res_bkg") && _slots.Count < Most)
                {
                    _slots.Add(new Slot(child.gameObject));
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>The first requirement slot, which the window copies for its own rows; null when a UI mod removed them.</summary>
        public GameObject? Template => _slots.Count > 0 ? _slots[0].Root : null;

        public IReadOnlyList<Slot> Slots => _slots;

        public void Clear()
        {
            foreach (Slot slot in _slots)
            {
                slot.Clear();
                slot.Root.SetActive(false);
            }
        }

        /// <summary>Shows the next unused slot (from the left) with the item's icon and amount; its name is the hover.</summary>
        public void Add(Sprite? icon, string name, int amount, bool enough)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.Root.activeSelf)
                {
                    continue;
                }
                slot.Root.SetActive(true);
                slot.Show(icon, null, amount.ToString(), enough, dim: false);
                slot.Tooltip("", name);
                return;
            }
        }
    }
}
