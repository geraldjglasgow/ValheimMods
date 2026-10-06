using System.Collections.Generic;

namespace GrindstoneSkills
{
    /// <summary>
    /// What animal yield knows about items, discovered from the scene's prefabs rather than listed, so modded
    /// stations and animals count the same way. Keyed by the item's shared name (m_shared.m_name), which is also what
    /// <see cref="Kitchen"/> and stacks compare. Filled by <see cref="YieldDiscovery"/> on every machine; adding is
    /// idempotent and never removes anything while the game runs.
    /// <list type="bullet">
    /// <item>Cooking inputs: the m_from of every CookingStation conversion (any station). Produce never gives them.</item>
    /// <item>Meat: cooking inputs of a kitchen station (<see cref="Kitchen.IsKitchen(CookingStation)"/>) that some
    /// prefab with a Tameable drops (its CharacterDrop): raw meat, wolf meat, lox meat, chicken meat. Prime Cuts stars
    /// these.</item>
    /// </list>
    /// </summary>
    public static class YieldCatalog
    {
        private static readonly HashSet<string> cookingInputs = new HashSet<string>();
        private static readonly Dictionary<string, ItemDrop> meat = new Dictionary<string, ItemDrop>();

        /// <summary>The meat item prefabs found so far.</summary>
        public static IEnumerable<ItemDrop> Meat => meat.Values;

        public static bool IsMeat(ItemDrop.ItemData item) => item?.m_shared != null && meat.ContainsKey(item.m_shared.m_name);

        public static bool IsCookingInput(ItemDrop.ItemData.SharedData shared) => shared != null && cookingInputs.Contains(shared.m_name);

        /// <summary>Reads the scene's cooking stations and tameable creatures; does nothing before ZNetScene exists.</summary>
        public static void Discover()
        {
            if (ZNetScene.instance == null)
                return;
            int before = meat.Count;
            HashSet<string> kitchenInputs = new HashSet<string>();
            PrefabIndex.ScenePrefabs prefabs = PrefabIndex.Scene();
            foreach (PrefabIndex.Converter converter in prefabs.Converters)
                AddStation(converter.Station, kitchenInputs);
            foreach (CharacterDrop drops in prefabs.TameableDrops)
                AddMeat(drops, kitchenInputs);
            if (meat.Count != before)
                GrindstoneSkills.Log.LogInfo($"Husbandry: animal meat for Prime Cuts: {string.Join(", ", meat.Keys)}.");
        }

        private static void AddStation(CookingStation station, HashSet<string> kitchenInputs)
        {
            if (station == null || station.m_conversion == null)
                return;
            bool kitchen = Kitchen.IsKitchen(station);
            foreach (CookingStation.ItemConversion conversion in station.m_conversion)
            {
                string name = SharedName(conversion?.m_from);
                if (name == null)
                    continue;
                cookingInputs.Add(name);
                if (kitchen)
                    kitchenInputs.Add(name);
            }
        }

        /// <summary>The kitchen inputs a tameable creature drops (its CharacterDrop).</summary>
        private static void AddMeat(CharacterDrop drops, HashSet<string> kitchenInputs)
        {
            if (drops.m_drops == null)
                return;
            foreach (CharacterDrop.Drop drop in drops.m_drops)
            {
                ItemDrop item = drop?.m_prefab != null ? drop.m_prefab.GetComponent<ItemDrop>() : null;
                string name = SharedName(item);
                if (name != null && kitchenInputs.Contains(name))
                    meat[name] = item;
            }
        }

        private static string SharedName(ItemDrop item) =>
            item != null && item.m_itemData?.m_shared != null && !string.IsNullOrEmpty(item.m_itemData.m_shared.m_name)
                ? item.m_itemData.m_shared.m_name : null;
    }
}
