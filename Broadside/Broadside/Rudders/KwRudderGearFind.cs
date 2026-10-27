using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Lays out a rudder's steering gear to its ship's captain's wheel (<see cref="KwRudderGear"/>) from the ship's pieces
    /// alone (<see cref="KwGearShip"/>): on the rudder's owner on the dock (<see cref="KwRudderGearDock"/>), and on every
    /// peer for a launched ship whose blueprint has a wheel but no gear saved (<see cref="KwShipGear"/>). The wheel nearest
    /// the rudder within <see cref="Search"/> is the one. It steers the rudder when it stands within <see cref="Reach"/> of
    /// it, on a deck, inside the hull, that deck (or one as high) runs back over the line shaft's way to within
    /// <see cref="StemGap"/> of the stern post, and the quadrant under it leaves the rudder's blade most of its height
    /// (<see cref="KwGearTrain.Ceiling"/>); the line shaft's way from the pinion to under the wheel is laid by
    /// <see cref="KwRudderGearRoute"/>. A wheel on a cabin's roof (<see cref="KwGearRooms"/>) counts the deck under the
    /// lowest cabin as its deck, and its shaft climbs the cabins' back walls to it (<see cref="KwGearClimb"/>). The rudder's
    /// lay is then the gear's (<see cref="KwRudderHelm.Gear"/>): its deck
    /// tiller gone, its head over the post's top, the quadrant on its stock under the stern's deck. Kit axes of the rudder.
    /// </summary>
    public static class KwRudderGearFind
    {
        /// <summary>How far from its rudder a wheel may stand and still steer it.</summary>
        public const float Reach = 25f;

        /// <summary>How far forward of the stern post the deck must reach, over the shaft coming through the post.</summary>
        public const float StemGap = 0.6f;

        // How far a wheel is looked for; how near its deck's level a deck counts as that one; the least share of its own
        // height the quadrant may leave the blade.
        private const float Search = 40f, Level = 0.35f, Least = 0.6f;

        /// <summary>
        /// The gear to the ship's wheel and, when it is linked, the rudder's lay for it; a refusal saying why when a wheel is
        /// near but cannot steer it (the rudder then keeps the lay its stern gives it); null with no wheel near.
        /// </summary>
        public static KwRudderGear? Find(KwGearShip ship, KwRudderTier tier, KwRudderFit fit, out KwRudderLay lay)
        {
            lay = KwRudderLay.Plain;
            (Vector3 At, Quaternion Turn)? wheel = Nearest(ship);
            if (wheel == null)
                return null;
            Vector3 at = wheel.Value.At;
            float far = new Vector2(at.x, at.y).magnitude;
            if (far > Reach)
                return KwRudderGear.Refused(at, KwGearWhy.Far, far);
            KwGearRooms? rooms = ship.TopNear(at.x, at.y, at.z, Level) == null ? KwGearRooms.Under(ship, at) : null;
            KwGearWhy why = Site(ship, tier, fit, at, rooms, out KwRudderLay found, out float level);
            if (why != KwGearWhy.None)
                return KwRudderGear.Refused(at, why);
            Vector3 pinion = KwGearTrain.PinionAt(tier, fit, found);
            KwRudderGear gear = rooms != null ? KwGearClimb.Find(ship, rooms, pinion, fit.PostDepth, at, level)
                : KwRudderGearRoute.Find(ship, pinion, fit.PostDepth, at, level) ?? KwRudderGear.Refused(at, KwGearWhy.Route);
            if (gear.Linked)
                lay = found;
            return gear;
        }

        // The wheel nearest the rudder (seen from above) within the search.
        private static (Vector3 At, Quaternion Turn)? Nearest(KwGearShip ship)
        {
            (Vector3 At, Quaternion Turn)? best = null;
            float bestSq = Search * Search;
            foreach ((Vector3 At, Quaternion Turn) wheel in ship.Wheels)
            {
                float sq = new Vector2(wheel.At.x, wheel.At.y).sqrMagnitude;
                if (sq < bestSq)
                    (best, bestSq) = (wheel, sq);
            }
            return best;
        }

        // The gear's lay for a wheel standing at a point: on a deck (or a cabin's roof: the deck under its cabins), in the
        // hull, its deck reaching back over the shaft to the stern post, the quadrant under it leaving the blade most of its height.
        private static KwGearWhy Site(KwGearShip ship, KwRudderTier tier, KwRudderFit fit, Vector3 at, KwGearRooms? rooms, out KwRudderLay lay, out float level)
        {
            lay = KwRudderLay.Plain;
            float? deck = rooms != null ? rooms.Floor : ship.TopNear(at.x, at.y, at.z, Level);
            level = deck ?? at.z;
            if (deck == null)
                return KwGearWhy.Deck;
            if (rooms == null && Mathf.Abs(at.y) + 0.3f > ship.Inside(new Vector3(at.x, at.y, level - 0.4f)))
                return KwGearWhy.Hull;
            float? stern = ship.TopNear(fit.PostDepth + StemGap, KwGearTrain.Offset(tier, fit), level, Level);
            if (stern == null)
                return KwGearWhy.Stern;
            lay = new KwRudderLay(KwRudderHelm.Gear, 0.02f, 0f, stern.Value, 0f);
            KwRudderSpan blade = tier.Span(fit);
            return KwGearTrain.Ceiling(lay) - blade.Foot < Least * (blade.Tip - blade.Foot) ? KwGearWhy.Low : KwGearWhy.None;
        }
    }
}
