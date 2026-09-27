using OpenKeep.Reach;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The refill rule, run by the fire's ZDO owner after the game's own two-second tick has burned fuel. The game's
    /// fuel is the <c>fuel</c> ZDO float, capped at <c>m_maxFuel</c>; as in <c>Fireplace.Interact</c> a unit is added
    /// only while the rounded-up fuel is below the cap, so nothing is ever wasted. When at least one whole unit fits,
    /// all the whole units that fit are taken from the containers near the fire and handed to the game in one
    /// <c>Fireplace.AddFuel</c> (the game's <c>RPC_AddFuelAmount</c>, handled at once on the owner: clamp, ZDO write,
    /// the fuel-added effect, <c>UpdateState</c>). A burning fire so stays between one unit below the cap and the cap,
    /// one container write per unit burned; a fire that burned out while nobody was near refills in one go.
    /// </summary>
    public static class FuelRefill
    {
        public static void Tick(Fireplace fire)
        {
            if (!Refillable(fire))
                return;
            ZDO zdo = fire.m_nview.GetZDO();
            int room = Room(fire, zdo.GetFloat(ZDOVars.s_fuel));
            if (room < 1 || !FuelFires.IsOn(zdo) || Player.m_localPlayer == null || !FuelRetry.Due(fire))
                return;
            if (!ReachRules.StationRuleFor(fire).Enabled)
            {
                FuelRetry.Later(fire);
                return;
            }
            int taken = FuelTake.Take(fire, room);
            if (taken > 0)
                Add(fire, zdo, taken);
            else
                FuelRetry.Later(fire);
        }

        /// <summary>The fire burns an item it may be refilled with (the game's own conditions for adding fuel).</summary>
        private static bool Refillable(Fireplace fire)
        {
            return fire.m_canRefill && !fire.m_infiniteFuel && fire.m_fuelItem != null && fire.m_maxFuel >= 1f;
        }

        /// <summary>Whole units that fit: the cap less the fuel rounded up, the game's own refusal rule.</summary>
        private static int Room(Fireplace fire, float fuel)
        {
            return Mathf.FloorToInt(fire.m_maxFuel) - Mathf.CeilToInt(Mathf.Max(0f, fuel));
        }

        private static void Add(Fireplace fire, ZDO zdo, int units)
        {
            float before = zdo.GetFloat(ZDOVars.s_fuel);
            fire.AddFuel(units);
            float after = zdo.GetFloat(ZDOVars.s_fuel);
            string name = Utils.GetPrefabName(fire.gameObject);
            string fuel = fire.m_fuelItem.name;
            if (after + 0.01f < before + units)
                Plugin.Log.LogWarning($"OpenKeep: {name} took {units} {fuel} from containers but its fuel only went from {before:0.##} to {after:0.##}");
            else
                Plugin.Log.LogInfo($"OpenKeep: {name} refilled itself with {units} {fuel} from containers near it ({after:0.#}/{fire.m_maxFuel:0})");
        }
    }
}
