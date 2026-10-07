using System.Collections.Generic;

namespace GrindstoneSkills
{
    /// <summary>
    /// What animal yield knows about items, discovered from the scene's prefabs rather than listed, so modded stations
    /// count the same way: the cooking inputs, the m_from of every CookingStation conversion (any station), which
    /// produce never gives. Keyed by the item's shared name (m_shared.m_name), which is also what stacks compare.
    /// Filled by <see cref="YieldDiscovery"/> on every machine; adding is idempotent and never removes anything while
    /// the game runs.
    /// </summary>
    public static class YieldCatalog
    {
        private static readonly HashSet<string> cookingInputs = new HashSet<string>();

        public static bool IsCookingInput(ItemDrop.ItemData.SharedData shared) => shared != null && cookingInputs.Contains(shared.m_name);

        /// <summary>Reads the scene's cooking stations; does nothing before ZNetScene exists.</summary>
        public static void Discover()
        {
            if (ZNetScene.instance == null)
                return;
            foreach (PrefabIndex.Converter converter in PrefabIndex.Scene().Converters)
                AddStation(converter.Station);
        }

        private static void AddStation(CookingStation station)
        {
            if (station == null || station.m_conversion == null)
                return;
            foreach (CookingStation.ItemConversion conversion in station.m_conversion)
            {
                string name = SharedName(conversion?.m_from);
                if (name != null)
                    cookingInputs.Add(name);
            }
        }

        private static string SharedName(ItemDrop item) =>
            item != null && item.m_itemData?.m_shared != null && !string.IsNullOrEmpty(item.m_itemData.m_shared.m_name)
                ? item.m_itemData.m_shared.m_name : null;
    }
}
