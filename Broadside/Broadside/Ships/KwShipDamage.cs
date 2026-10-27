using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Damage and flooding on a launched ship, made on every peer as the ship is built (<see cref="KwShipBody"/>), after
    /// its float (<see cref="KwShipHydro"/>, whose slices hold the water): its holes (<see cref="KwHullHoles"/>, made by the
    /// game's wear, <see cref="KwHullDamagePatch"/>; patched with a repair kit, <see cref="KwHullPatches"/>; mended with the
    /// hammer, <see cref="KwShipRepair"/>), its boxes opened through them (<see cref="KwHoleBoxes"/>), the water
    /// in its hold (<see cref="KwShipFlood"/>, run on the owner in its buoyancy step) and bailing (<see cref="KwShipBail"/>);
    /// where there is a screen, the holes cut through the hull's look (<see cref="KwHoleLooks"/>) and the water drawn
    /// (<see cref="KwFloodSurface"/>). Heavy shots are routed through the ship (<see cref="KwShipHitRouter"/>), and its
    /// railings break where they strike (<see cref="KwShipRails"/>, drawn by <see cref="KwShipRailLooks"/>).
    /// </summary>
    public static class KwShipDamage
    {
        public static void Add(Transform ship, KwBlueprint blueprint, KwBpHull hull)
        {
            KwShipHydro? hydro = ship.GetComponent<KwShipHydro>();
            if (hydro == null)
                return;
            KwHullHoles holes = ship.gameObject.AddComponent<KwHullHoles>();
            holes.Set(blueprint, hydro.Waterline);
            KwShipBody.Step("Broadside ship hull patches", () => KwHullPatches.Add(ship, holes));
            KwShipBody.Step("Broadside ship holes' boxes", () => KwHoleBoxes.Add(ship, holes));
            KwShipBody.Step("Broadside ship shots", () => KwShipHitRouter.Add(ship, blueprint, holes, hull));
            KwShipRails? rails = null;
            KwShipBody.Step("Broadside ship railings", () => rails = KwShipRails.Add(ship, blueprint));
            KwShipFlood flood = ship.gameObject.AddComponent<KwShipFlood>();
            flood.Set(hydro, holes, hull);
            ship.gameObject.AddComponent<KwShipBail>().Set(flood, hull);
            if (!KwShipBody.Drawn)
                return;
            KwShipBody.Step("Broadside ship holes look", () => KwHoleLooks.Watch(ship, holes));
            if (rails != null)
                KwShipBody.Step("Broadside ship railings look", () => KwShipRailLooks.Watch(ship, rails));
            KwShipBody.Step("Broadside ship flood look", () => KwFloodSurface.Add(ship, flood, KwHullSlices.Rings(blueprint, hull)));
        }
    }
}
