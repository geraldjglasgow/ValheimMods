using System.Collections.Generic;
using UnityEngine;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// The copied requirement slots above the button: what the next press costs (a rune, an essence, a trophy), shown
    /// the way the crafting panel shows a recipe's materials. The station-level slot beside them is hidden.
    /// </summary>
    internal sealed class CostRow
    {
        private readonly List<Slot> _slots = new List<Slot>();

        public CostRow(RectTransform? requirements)
        {
            if (requirements == null)
            {
                return;
            }
            foreach (Transform child in requirements)
            {
                if (child.name.StartsWith("res_bkg"))
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

        public void Clear()
        {
            foreach (Slot slot in _slots)
            {
                slot.Clear();
            }
        }

        /// <summary>Fills the next free slot (the game fills from the left); nothing when all four are used.</summary>
        public void Add(Sprite? icon, string name, int amount, bool enough)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.Icon == null || slot.Icon.gameObject.activeSelf)
                {
                    continue;
                }
                slot.Show(icon, name, amount.ToString(), enough, dim: false);
                slot.Tooltip("", name);
                return;
            }
        }
    }
}
