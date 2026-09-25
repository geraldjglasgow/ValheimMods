using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace FeastMaster
{
    /// <summary>
    /// Per-recipe cook times: one section per prefab with a CookingStation component in ZNetScene (the game's
    /// cooking stations and oven, and modded ones), one entry per recipe named after the raw item, defaulting to
    /// the game's time. Bound from <see cref="ScenePrefabs"/>, in the same scene load as the food sections, so the
    /// entries exist before the server's first push. In memory the entries are keyed by the raw item's prefab, which every live
    /// station's conversions reference as well.
    /// </summary>
    public static class CookTimes
    {
        private static readonly Dictionary<int, Dictionary<ItemDrop, ConfigEntry<float>>> byStation
            = new Dictionary<int, Dictionary<ItemDrop, ConfigEntry<float>>>();

        /// <summary>The entries of a station by its prefab hash (the ZDO's prefab), keyed by raw item.</summary>
        public static bool TryGet(int prefabHash, out Dictionary<ItemDrop, ConfigEntry<float>> entries)
        {
            return byStation.TryGetValue(prefabHash, out entries);
        }

        /// <summary>Binds one station's section unless it has one already. Returns whether a section was added.</summary>
        public static bool Bind(string prefabName, CookingStation station)
        {
            int hash = prefabName.GetStableHashCode();
            if (byStation.ContainsKey(hash))
                return false;
            try
            {
                byStation[hash] = BindRecipes(prefabName, station.m_conversion);
                return true;
            }
            catch (ArgumentException e)
            {
                FeastMaster.Log.LogWarning($"Cook times of {prefabName} cannot be configured: {e.Message}");
                return false;
            }
        }

        private static Dictionary<ItemDrop, ConfigEntry<float>> BindRecipes(string station, List<CookingStation.ItemConversion> recipes)
        {
            Dictionary<ItemDrop, ConfigEntry<float>> entries = new Dictionary<ItemDrop, ConfigEntry<float>>();
            foreach (CookingStation.ItemConversion recipe in recipes)
            {
                if (recipe == null || recipe.m_from == null || recipe.m_to == null || entries.ContainsKey(recipe.m_from))
                    continue;
                entries[recipe.m_from] = FeastMaster.Synced.Bind(station, recipe.m_from.name, recipe.m_cookTime,
                    $"Seconds {recipe.m_from.name} takes to cook into {recipe.m_to.name} on this station. Cook Time Multiplier in section 9 applies on top.",
                    acceptableValues: new AcceptableValueRange<float>(0.1f, 86400f));
            }
            return entries;
        }
    }
}
