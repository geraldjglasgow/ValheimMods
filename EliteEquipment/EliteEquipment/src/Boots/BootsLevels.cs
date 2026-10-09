using EliteCraftingLink;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// With EliteCrafting, every pair of boots has its leggings' item level (EliteCrafting's 1 Meadows to 8 Deep North,
    /// which sets how strong an inscription it may roll), set through its API (<c>SetItemLevel</c>) when the item
    /// database wakes. Its own derivation would read the boots' recipe instead, a fifth of the leggings' (a rare material
    /// could round away) or only its station, and would miss an override the leggings have. Silent without EliteCrafting.
    /// </summary>
    public static class BootsLevels
    {
        public static void Apply()
        {
            if (!CraftingLink.Present)
                return;
            foreach (BootSet set in BootSets.All)
            {
                ItemDrop legs = set.LegsPrefab != null ? set.LegsPrefab.GetComponent<ItemDrop>() : null;
                int level = legs != null && set.Item != null ? CraftingClasses.GetItemLevel(legs.m_itemData) : 0;
                if (level > 0)
                    CraftingClasses.SetItemLevel(set.Prefab, level);
            }
        }
    }
}
