using System;
using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// Makes the new shield items once per process, on every peer (dedicated server included), the first time the scene
    /// or the item database wakes with the game's shields in its list; then adds them to every scene's and database's list
    /// before the game indexes it (<c>ZNetScene.Awake</c>, <c>ObjectDB.Awake</c>, <c>ObjectDB.CopyOtherDB</c>). They exist
    /// whether the switch is on or not, so shields already made always load. The models come from the embedded bundle
    /// <c>ee_shields</c> (one per platform); without it none is made. Nothing ever throws out of an Awake (a throw
    /// there stops the world from loading).
    /// </summary>
    public static class ShieldItems
    {
        private const string Bundle = "ee_shields";
        private const string Probe = "ShieldIronBuckler";
        private static bool built;

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ScenePatch
        {
            [HarmonyPrefix]
            private static void Prefix(ZNetScene __instance) => Prepare(__instance.m_prefabs);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabasePatch
        {
            [HarmonyPrefix]
            private static void Prefix(ObjectDB __instance) => Prepare(__instance.m_items);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static class CopyPatch
        {
            [HarmonyPrefix]
            private static void Prefix(ObjectDB other) => Prepare(other.m_items);
        }

        private static void Prepare(List<GameObject> prefabs)
        {
            try
            {
                Build(prefabs);
                foreach (ShieldSet set in ShieldSets.All)
                {
                    if (set.Item != null && !prefabs.Contains(set.Item))
                        prefabs.Add(set.Item);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"EliteEquipment: the shields could not be made: {e}");
            }
        }

        private static void Build(List<GameObject> prefabs)
        {
            // The main menu's database wakes with an empty list before it copies the real one in: wait for a list that
            // holds the game's shields (the iron buckler stands for them).
            if (built || prefabs == null || Find(prefabs, Probe) == null)
                return;
            built = true;
            AssetBundle bundle = EmbeddedBundle.Load(typeof(ShieldItems).Assembly, Bundle);
            ShieldIcons.Prepare(prefabs, bundle);
            foreach (ShieldSet set in ShieldSets.All)
            {
                try
                {
                    BuildSet(set, prefabs, bundle);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"EliteEquipment: the {set.Name} could not be made and is not in the game: {e}");
                }
            }
        }

        private static void BuildSet(ShieldSet set, List<GameObject> prefabs, AssetBundle bundle)
        {
            GameObject game = Find(prefabs, set.Base);
            if (game == null || game.GetComponent<ItemDrop>() == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the game's {set.Base} was not found: there is no {set.Name}");
                return;
            }
            GameObject look = set.Look != null ? Find(prefabs, set.Look) : null;
            if (set.Look != null && look == null)
                Plugin.Log.LogWarning($"EliteEquipment: the game's {set.Look} was not found: the {set.Name} wears its own look");
            set.Item = ShieldItem.Build(set, game, look, bundle);
        }

        private static GameObject Find(List<GameObject> prefabs, string name) => prefabs.Find(p => p != null && p.name == name);
    }
}
