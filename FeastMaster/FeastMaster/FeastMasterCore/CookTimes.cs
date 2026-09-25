using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>
    /// Per-recipe cook times: one section per prefab with a CookingStation component in ZNetScene (the game's
    /// cooking stations and oven, and modded ones), one entry per recipe named after the raw item, defaulting to
    /// the game's time. Bound on ZNetScene.Awake, in the same scene load as the food sections, so the entries exist
    /// before the server's first push. In memory the entries are keyed by the raw item's prefab, which every live
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

        /// <summary>Binds every station not bound yet; the file is written once, and only when entries were added.</summary>
        public static void BindAll(ZNetScene scene)
        {
            ConfigFile file = FeastMaster.Synced.Config;
            bool saveOnSet = file.SaveOnConfigSet;
            int before = byStation.Count;
            file.SaveOnConfigSet = false;
            try
            {
                foreach (GameObject prefab in scene.m_prefabs)
                    BindStation(prefab);
            }
            finally
            {
                file.SaveOnConfigSet = saveOnSet;
            }
            if (byStation.Count != before && saveOnSet)
                file.Save();
        }

        private static void BindStation(GameObject prefab)
        {
            CookingStation station = prefab != null ? prefab.GetComponent<CookingStation>() : null;
            if (station == null || byStation.ContainsKey(prefab.name.GetStableHashCode()))
                return;
            try
            {
                byStation[prefab.name.GetStableHashCode()] = BindRecipes(prefab.name, station.m_conversion);
            }
            catch (ArgumentException e)
            {
                FeastMaster.Log.LogWarning($"Cook times of {prefab.name} cannot be configured: {e.Message}");
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
