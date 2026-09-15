namespace OpenKeep.Salvage
{
    /// <summary>One material a salvage returns: the item prefab's ItemDrop and the whole amount.</summary>
    public readonly struct SalvageReturn
    {
        public SalvageReturn(ItemDrop item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        public ItemDrop Item { get; }

        public int Amount { get; }

        /// <summary>The shared name token, the stacking identity of the material.</summary>
        public string Name => Item.m_itemData.m_shared.m_name;

        public int MaxStack => Item.m_itemData.m_shared.m_maxStackSize < 1 ? 1 : Item.m_itemData.m_shared.m_maxStackSize;
    }
}
