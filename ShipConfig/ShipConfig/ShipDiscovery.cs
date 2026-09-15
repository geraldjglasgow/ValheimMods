using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// Finds ships and binds their entries: every prefab of the scene with a Ship and a WearNTear component at scene
    /// start, and any ship another mod registers later, the first time one of its instances awakes.
    /// </summary>
    public static class ShipDiscovery
    {
        /// <summary>Binds entries for every ship prefab of the scene (once) and applies the values everywhere.</summary>
        public static void LoadShips()
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return;

            int before = ShipConfiguration.Ships.Count;
            ShipConfiguration.RegisterBatch(() =>
            {
                foreach (GameObject prefab in scene.m_prefabs)
                    RegisterPrefab(prefab);
            });
            LogCount(before);
            ShipValues.ApplyAll();
        }

        private static void RegisterPrefab(GameObject prefab)
        {
            if (prefab == null || prefab.GetComponent<Ship>() == null || prefab.GetComponent<WearNTear>() == null)
                return;
            ShipConfiguration.Register(prefab.name, ShipDefaults.From(prefab, isInstance: false));
        }

        /// <summary>
        /// A WearNTear awoke. For a ship whose prefab has no entries yet, binds them (defaults from the scene prefab
        /// of that name, or from the instance when the prefab is not in the scene) and applies them to prefab and
        /// loaded ships; then applies the values to this instance. Not a ship: nothing.
        /// </summary>
        public static void OnWearNTearAwake(WearNTear wearNTear)
        {
            Ship ship = wearNTear.GetComponent<Ship>();
            if (ship == null)
                return;

            string name = Utils.GetPrefabName(wearNTear.gameObject);
            if (!ShipConfiguration.TryGet(name, out ShipEntries entries))
            {
                entries = RegisterLate(name, wearNTear.gameObject);
                if (entries == null)
                    return;
                ShipValues.Apply(name);
            }
            ShipValues.ApplyToInstance(ship, wearNTear, entries);
        }

        private static ShipEntries RegisterLate(string name, GameObject instance)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            ShipDefaults defaults = prefab != null
                ? ShipDefaults.From(prefab, isInstance: false)
                : ShipDefaults.From(instance, isInstance: true);
            int before = ShipConfiguration.Ships.Count;
            ShipConfiguration.RegisterBatch(() => ShipConfiguration.Register(name, defaults));
            LogCount(before);
            return ShipConfiguration.TryGet(name, out ShipEntries entries) ? entries : null;
        }

        private static void LogCount(int before)
        {
            if (ShipConfiguration.Ships.Count != before)
                ShipConfig.Log.LogInfo($"Loaded settings for {ShipConfiguration.Ships.Count} ships.");
        }
    }

    /// <summary>Scene start, after other mods' postfixes so prefabs they register at normal priority are seen.</summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class ZNetSceneAwakePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix() => Guard.Run("ship discovery", ShipDiscovery.LoadShips);
    }

    /// <summary>
    /// A ship instance awoke. WearNTear.Awake is the hook, not Ship.Awake: Ship's fields need no Awake, while
    /// WearNTear.Awake reads the stored health and adds the world level bonus, and both must be done first.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
    public static class WearNTearAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(WearNTear __instance) => Guard.Run("ship awake", () => ShipDiscovery.OnWearNTearAwake(__instance));
    }
}
