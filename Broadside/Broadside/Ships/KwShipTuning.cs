using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a ship floats and sails, from its hull and its sails, set on the game's own Ship (whose steering, sailing and
    /// rowing do the work, in <see cref="KwShipDrive"/>'s step). It floats on its own hull (<see cref="KwShipHydro"/>), its
    /// waterline 55% up its hull amidships or under its lowest deck, whichever is lower (<see cref="Draught"/>), so a hold
    /// is above the water; its weight is what its hull displaces there. The rudder's push acts at its stern, the sails' a
    /// little over its centre of weight (a fifth of its depth, 0.5 to 2 m, <see cref="KwShipDrive"/>); how fast, handy and tough it is comes from its build (<see cref="KwShipStats"/>).
    /// </summary>
    public static class KwShipTuning
    {
        private const float UnderDeck = 0.15f;

        /// <summary>How deep under the water the keel line floats: 55% of the hull's depth amidships (<see cref="KwBpHull.Midship"/>;
        /// the whole depth took in a raised bow and stern, and a ship with tall ends floated with its deck almost awash, the
        /// user 2026-10-26), or less, so the lowest deck stands a little over the water; at least 10 cm.</summary>
        public static float Draught(KwBpHull hull) => Mathf.Max(0.1f, Mathf.Min(0.55f * hull.Midship, hull.DeckLow - UnderDeck));

        /// <summary>The waterline a ship's figures are reckoned to (<see cref="KwShipWeight.Sized"/>): the draught as it stood
        /// before it went by the depth amidships, 55% up the whole depth, so its health and speed stay as they were tuned.</summary>
        public static float Sizing(KwBpHull hull) => Mathf.Max(0.1f, Mathf.Min(0.55f * hull.Depth, hull.DeckLow - UnderDeck));

        public static void Apply(Transform ship, KwBlueprint blueprint, KwBpHull hull)
        {
            ship.gameObject.AddComponent<KwShipHydro>().Set(blueprint, hull);
            Ship game = ship.GetComponent<Ship>();
            game.m_stearForceOffset = -hull.Length / 2f;
            game.m_sailForceOffset = Mathf.Clamp(0.2f * hull.Depth, 0.5f, 2f);
            float sized = KwShipWeight.Sized(ship.GetComponent<KwShipHydro>().Slices, hull);
            KwShipStats.Of(blueprint, hull, sized).Apply(game, ship.GetComponent<WearNTear>());
        }
    }
}
