using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// Makes the 20 boots items once per process, on every peer (dedicated server included), the first time the scene or
    /// the item database wakes, from the leggings found in its prefab list; then adds them to every scene's and database's
    /// list before the game indexes it (<c>ZNetScene.Awake</c>, <c>ObjectDB.Awake</c>, <c>ObjectDB.CopyOtherDB</c>, which
    /// shares the other database's list). They exist whether the switch is on or not, so boots already made always load.
    /// A set whose leggings are missing is left out; nothing ever throws out of an Awake (a throw there stops the world
    /// from loading).
    /// </summary>
    public static class BootsItems
    {
        private const string Probe = "ArmorIronLegs";
        private static bool built;

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ScenePatch
        {
            [HarmonyPrefix]
            private static void Prefix(ZNetScene __instance) => Prepare(__instance.m_prefabs, scene: true);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabasePatch
        {
            [HarmonyPrefix]
            private static void Prefix(ObjectDB __instance) => Prepare(__instance.m_items, scene: false);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static class CopyPatch
        {
            [HarmonyPrefix]
            private static void Prefix(ObjectDB other) => Prepare(other.m_items, scene: false);
        }

        private static void Prepare(List<GameObject> prefabs, bool scene)
        {
            try
            {
                Build(prefabs);
                foreach (BootSet set in BootSets.All)
                {
                    if (set.Item != null && !prefabs.Contains(set.Item))
                        prefabs.Add(set.Item);
                }
                if (scene)
                    BootsDrop.FitAll(prefabs);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"EliteEquipment: the boots could not be made: {e}");
            }
        }

        private static void Build(List<GameObject> prefabs)
        {
            if (built || prefabs == null)
                return;
            // The main menu's database wakes with an empty list before it copies the real one in: wait for a list that
            // holds the game's leggings (the iron greaves stand for them).
            if (Find(prefabs, Probe) == null)
                return;
            built = true;
            foreach (BootSet set in BootSets.All)
            {
                try
                {
                    BuildSet(set, prefabs);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"EliteEquipment: the {set.Key} boots could not be made and are not in the game: {e}");
                }
            }
        }

        private static void BuildSet(BootSet set, List<GameObject> prefabs)
        {
            GameObject legs = Find(prefabs, set.Legs);
            if (legs == null || legs.GetComponent<ItemDrop>() == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the game's {set.Legs} was not found: there are no {set.Key} boots");
                return;
            }
            set.LegsPrefab = legs;
            BootSets.FoundLegs(set, legs.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            BootsStats.Remember(legs);
            LegsIcons.Remember(legs);
            Dictionary<Mesh, Mesh[]> cuts = SkinSplit.Cut(set, legs);
            LegsLook.Add(set, cuts);
            set.Item = BootsItem.Build(set, legs, cuts);
        }

        private static GameObject Find(List<GameObject> prefabs, string name) => prefabs.Find(p => p != null && p.name == name);
    }
}
