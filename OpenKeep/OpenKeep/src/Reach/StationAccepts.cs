using System;
using OpenKeep.Core;

namespace OpenKeep.Reach
{
    /// <summary>What each station accepts, expressed with the station's own allow checks so modded conversions work.</summary>
    internal static class StationAccepts
    {
        public static Func<ItemDrop.ItemData, bool> SmelterOre(Smelter smelter)
        {
            return item => item != null && smelter.IsItemAllowed(ItemNames.PrefabName(item));
        }

        public static Func<ItemDrop.ItemData, bool> CookingFood(CookingStation station)
        {
            return item => item != null && station.IsItemAllowed(ItemNames.PrefabName(item));
        }

        public static Func<ItemDrop.ItemData, bool> FermenterBase(Fermenter fermenter)
        {
            return item => item != null && fermenter.IsItemAllowed(ItemNames.PrefabName(item).GetStableHashCode());
        }

        /// <summary>The fuel of a smelter, cooking station or fireplace: the same shared name.</summary>
        public static Func<ItemDrop.ItemData, bool> Fuel(ItemDrop fuel)
        {
            string name = fuel != null ? fuel.m_itemData.m_shared.m_name : null;
            return item => name != null && item != null && item.m_shared.m_name == name;
        }
    }
}
