using UnityEngine;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// One thing a salvage returns: the item prefab's ItemDrop and the whole amount, certain for materials, or at a
    /// chance below one for an EliteCrafting rune (<see cref="EliteCraftingSalvage"/>).
    /// </summary>
    public readonly struct SalvageReturn
    {
        public SalvageReturn(ItemDrop item, int amount, float chance = 1f)
        {
            Item = item;
            Amount = amount;
            Chance = chance;
        }

        public ItemDrop Item { get; }

        public int Amount { get; }

        /// <summary>0-1: how likely the salvage gives this return; 1 for materials.</summary>
        public float Chance { get; }

        public bool IsCertain => Chance >= 1f;

        /// <summary>The chance as whole percent, "25%".</summary>
        public string ChanceText => Mathf.RoundToInt(Chance * 100f) + "%";

        /// <summary>The shared name token, the stacking identity of the material.</summary>
        public string Name => Item.m_itemData.m_shared.m_name;

        public int MaxStack => Item.m_itemData.m_shared.m_maxStackSize < 1 ? 1 : Item.m_itemData.m_shared.m_maxStackSize;
    }
}
