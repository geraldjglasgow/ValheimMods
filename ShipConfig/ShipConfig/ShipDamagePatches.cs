using HarmonyLib;

namespace ShipConfig
{
    /// <summary>
    /// Damage taken and invulnerability. Every hit on a WearNTear (attacks, collisions, and the ship's own water
    /// impact, capsize and Ashlands ocean damage, which all call WearNTear.Damage) reaches the owner through
    /// RPC_Damage; weather, support and biome wear reach ApplyDamage directly from UpdateWear. Only objects with a
    /// Ship component are affected.
    /// </summary>
    public static class ShipDamage
    {
        /// <summary>The entries of the ship this WearNTear belongs to; null for anything that is not a known ship.</summary>
        public static ShipEntries ForShip(WearNTear wearNTear)
        {
            if (wearNTear == null || wearNTear.GetComponent<Ship>() == null)
                return null;
            return ShipConfiguration.TryGet(Utils.GetPrefabName(wearNTear.gameObject), out ShipEntries entries) ? entries : null;
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
