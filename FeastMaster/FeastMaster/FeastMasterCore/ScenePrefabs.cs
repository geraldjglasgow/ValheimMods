using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>
    /// Binds the sections that come from ZNetScene rather than the item database: one per cooking station, and the
    /// food of every feast. Last, so prefabs another mod registers in its own ZNetScene.Awake postfix are found
    /// too. Like the item database pass, the .cfg is written once and the item values applied once at the end.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class ScenePrefabs
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ZNetScene __instance)
        {
            ConfigFile file = FeastMaster.Synced.Config;
            if (!BindAll(file, __instance.m_prefabs))
                return;
            if (file.SaveOnConfigSet)
                file.Save();
            ItemValues.ApplyAll();
        }

        /// <summary>Binds with file writes and item value updates held back; returns whether sections were added.</summary>
        private static bool BindAll(ConfigFile file, List<GameObject> prefabs)
        {
            bool saveOnSet = file.SaveOnConfigSet;
            bool added = false;
            file.SaveOnConfigSet = false;
            ItemValues.SuspendWhileBinding(true);
            try
            {
                foreach (GameObject prefab in prefabs)
                    added |= Bind(prefab);
            }
            finally
            {
                file.SaveOnConfigSet = saveOnSet;
                ItemValues.SuspendWhileBinding(false);
            }
            return added;
        }

        private static bool Bind(GameObject prefab)
        {
            if (prefab == null)
                return false;
            CookingStation station = prefab.GetComponent<CookingStation>();
            bool added = station != null && CookTimes.Bind(prefab.name, station);
            Feast feast = prefab.GetComponent<Feast>();
            ItemDrop food = feast == null ? null : feast.m_foodItem != null ? feast.m_foodItem : prefab.GetComponent<ItemDrop>();
            return (food != null && FeastMasterData.BindFeastFood(food)) | added;
        }
    }
}
