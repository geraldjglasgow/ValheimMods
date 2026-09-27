using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Crops are Farming's, never forage: a pickable whose prefab name is in any Plant's m_grownPrefabs, planted or
    /// wild (Jotun puffs, magecap and seed carrots grow wild from the same prefabs). Filled once from the scene's
    /// prefabs when ZNetScene wakes. The split agreed with the Farming module; this private copy goes once Farming's
    /// shared Core/Crops.cs is in.
    /// </summary>
    public static class ForageCrops
    {
        private static readonly HashSet<string> names = new HashSet<string>();

        public static bool Is(Pickable pickable) => pickable != null && names.Contains(Utils.GetPrefabName(pickable.gameObject));

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(ZNetScene __instance)
            {
                foreach (GameObject prefab in __instance.m_prefabs)
                {
                    Plant plant = prefab != null ? prefab.GetComponent<Plant>() : null;
                    if (plant?.m_grownPrefabs == null)
                        continue;
                    foreach (GameObject grown in plant.m_grownPrefabs)
                    {
                        if (grown != null)
                            names.Add(grown.name);
                    }
                }
            }
        }
    }
}
