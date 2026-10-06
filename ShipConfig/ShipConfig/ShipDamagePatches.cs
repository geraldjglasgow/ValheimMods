using System.Collections.Generic;
using HarmonyLib;

namespace ShipConfig
{
    /// <summary>
    /// Damage taken and invulnerability. Every hit on a WearNTear (attacks, collisions, and the ship's own water
    /// impact, capsize and Ashlands ocean damage, which all call WearNTear.Damage) reaches the owner through
    /// RPC_Damage; weather, support and biome wear reach ApplyDamage directly from UpdateWear. Only the ship prefabs
    /// ShipConfig knows (each has a Ship component) are affected.
    /// </summary>
    public static class ShipDamage
    {
        /// <summary>The known ships by the hash of their prefab name, which is what each one's ZDO carries.</summary>
        private static readonly Dictionary<int, ShipEntries> byPrefab = new Dictionary<int, ShipEntries>();

        /// <summary>
        /// The entries of the ship this WearNTear belongs to; null for anything that is not a known ship. Every hit on
        /// every piece and every weather-wear tick asks, so it is one dictionary lookup by the ZDO's prefab hash: no
        /// component lookup and no name string.
        /// </summary>
        public static ShipEntries ForShip(WearNTear wearNTear)
        {
            ZDO zdo = wearNTear != null && wearNTear.m_nview != null ? wearNTear.m_nview.GetZDO() : null;
            if (zdo == null)
                return null;
            if (byPrefab.Count != ShipConfiguration.Ships.Count)
                Index();
            return byPrefab.TryGetValue(zdo.GetPrefab(), out ShipEntries entries) ? entries : null;
        }

        /// <summary>Ships are only ever added, so a count that differs means new ones to index.</summary>
        private static void Index()
        {
            byPrefab.Clear();
            foreach (KeyValuePair<string, ShipEntries> ship in ShipConfiguration.Ships)
                byPrefab[ship.Key.GetStableHashCode()] = ship.Value;
        }
    }

    /// <summary>Scales every damage component of a hit on a ship, or drops the hit for a ship that takes no damage.</summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_Damage))]
    public static class WearNTearRpcDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(WearNTear __instance, HitData hit)
        {
            ShipEntries entries = ShipDamage.ForShip(__instance);
            if (entries == null)
                return true;
            if (entries.TakesNoDamage)
                return false;
            hit.m_damage.Modify(entries.EffectiveDamageTaken);
            return true;
        }
    }

    /// <summary>Blocks weather wear, and any other direct damage, on a ship that takes no damage. Hits were scaled already.</summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ApplyDamage))]
    public static class WearNTearApplyDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(WearNTear __instance, ref bool __result)
        {
            ShipEntries entries = ShipDamage.ForShip(__instance);
            if (entries == null || !entries.TakesNoDamage)
                return true;
            __result = false;
            return false;
        }
    }
}
