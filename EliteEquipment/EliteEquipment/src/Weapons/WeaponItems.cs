using System;
using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// Makes the new weapons once per process, on every peer (dedicated server included), the first time the scene or the
    /// item database wakes with the game's weapons in its list; then adds them (and the thrown projectiles, to the scene)
    /// to every scene's and database's list before the game indexes it. They exist whether the switch is on or not, so
    /// weapons already made always load. The models come from the embedded bundle <c>ee_weapons</c>; without it none is
    /// made. Nothing ever throws out of an Awake (a throw there stops the world from loading).
    /// </summary>
    public static class WeaponItems
    {
        private const string Bundle = "ee_weapons";
        private const string Probe = "THSwordKrom";
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
                foreach (WeaponSet set in WeaponSets.All)
                {
                    AddOnce(prefabs, set.Item);
                    if (scene)
                        AddOnce(prefabs, set.Projectile);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"EliteEquipment: the weapons could not be made: {e}");
            }
        }

        private static void Build(List<GameObject> prefabs)
        {
            // The main menu's database wakes with an empty list before it copies the real one in.
            if (built || prefabs == null || Find(prefabs, Probe) == null)
                return;
            built = true;
            AssetBundle bundle = EmbeddedBundle.Load(typeof(WeaponItems).Assembly, Bundle);
            WeaponIcons.Prepare(prefabs, bundle);
            foreach (WeaponSet set in WeaponSets.All)
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

        private static void BuildSet(WeaponSet set, List<GameObject> prefabs, AssetBundle bundle)
        {
            GameObject game = Find(prefabs, set.Base);
            if (game == null || game.GetComponent<ItemDrop>() == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the game's {set.Base} was not found: there is no {set.Name}");
                return;
            }
            set.Item = WeaponItem.Build(set, game, Find(prefabs, set.Look), bundle);
        }

        private static void AddOnce(List<GameObject> prefabs, GameObject prefab)
        {
            if (prefab != null && !prefabs.Contains(prefab))
                prefabs.Add(prefab);
        }

        private static GameObject Find(List<GameObject> prefabs, string name) => prefabs.Find(p => p != null && p.name == name);
    }
}
