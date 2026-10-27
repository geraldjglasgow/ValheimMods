using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The steering gear's train (<see cref="KwRudderGear"/>), its sizes and how every part of it turns, in the rudder's kit
    /// frame (x forward from the post's after face, y left, z up from the rudder point, metres). A toothed quadrant on the
    /// rudder's stock, facing aft over its head, outside the hull; a pinion on the line shaft's after end rolling on the
    /// quadrant's face <see cref="Working"/> aft of the stock and <see cref="Offset"/> to port of it, so the shaft runs
    /// forward past the stock and through a bore in the stern post into the hull, <see cref="Below"/> under the deck's top;
    /// at each bend a pair of bevel wheels as big as each other; under the captain's wheel a last pair turning the drive up a
    /// short shaft through the deck into the wheel's stand, and in there (hidden) to its axle. Every shaft turns the same
    /// amount about the way it runs from the wheel toward the rudder (<see cref="Spin"/>): the bevel pairs are one to one,
    /// and the pinion turns <see cref="Ratio"/> times for the quadrant's once, so the wheel turns <see cref="WheelTurn"/>
    /// each way at full helm. Plain arithmetic only, so offline checks run it.
    /// </summary>
    public static class KwGearTrain
    {
        /// <summary>The pinion's teeth; how many times it turns for the quadrant's once; the bevel wheels' teeth.</summary>
        public const int PinionTeeth = 8, Ratio = 8, BevelTeeth = 12;

        /// <summary>
        /// The pinion's pitch radius; where it rolls on the quadrant, from the stock (its pitch radius times the ratio, along
        /// the ship); the bevel wheels' pitch radius (their apex as far from each); the shaft's radius; its axis under a deck's top.
        /// </summary>
        public const float Pinion = 0.05f, Working = Pinion * Ratio, Bevel = 0.075f, Shaft = 0.032f, Below = 0.24f;

        /// <summary>The quadrant's plate thickness and its teeth's height over it; how far its teeth reach either side of the working radius.</summary>
        public const float Plate = 0.035f, Tooth = 0.018f, Band = 0.045f;

        /// <summary>The captain's wheel's turn at full helm: the rudder's, times the ratio.</summary>
        public const float WheelTurn = KwRudder.MaxAngle * Ratio;

        /// <summary>How far the quadrant's teeth reach round either side of the pinion, past the rudder's full turn (degrees).</summary>
        public const float Spare = 9f;

        /// <summary>
        /// How far to port of the stock the shaft passes: clear of the stock and its collars, inside the post's width where
        /// the post allows (its bore then inside the post).
        /// </summary>
        public static float Offset(KwRudderTier tier, KwRudderFit fit)
        {
            float stock = tier.Stock(fit);
            return Mathf.Max(stock + 0.055f, Mathf.Min(stock + 0.075f, fit.PostHalf - 0.045f));
        }

        /// <summary>The pinion's middle for a rudder steered by the gear: aft of the stock, to port of it, under the stern's deck (<see cref="KwRudderLay.Deck"/>).</summary>
        public static Vector3 PinionAt(KwRudderTier tier, KwRudderFit fit, KwRudderLay lay) =>
            new Vector3(tier.Pivot(fit).x - Working, Offset(tier, fit), lay.Deck - Below);

        /// <summary>The quadrant's plate top over the rudder point (its teeth stand on it): just under the pinion's pitch circle.</summary>
        public static float PlateTop(KwRudderLay lay) => lay.Deck - Below - Pinion - Tooth / 2f;

        /// <summary>The highest the rudder's blade may reach with the gear: a hand under the quadrant's plate.</summary>
        public static float Ceiling(KwRudderLay lay) => PlateTop(lay) - Plate - 0.06f;

        /// <summary>The top of the quadrant's hub round the stock.</summary>
        public static float HubTop(KwRudderLay lay) => PlateTop(lay) + 0.03f;

        /// <summary>The quadrant's radius to its teeth's middle: where the pinion rolls, seen from the stock.</summary>
        public static float Radius(float offset) => Mathf.Sqrt(Working * Working + offset * offset);

        /// <summary>Where on the quadrant the pinion rolls (degrees from forward toward port, in the blade's frame with the rudder amidships).</summary>
        public static float Contact(float offset) => 180f - Mathf.Asin(offset / Radius(offset)) * Mathf.Rad2Deg;

        /// <summary>The angle between two of the quadrant's teeth (degrees): one pinion tooth's roll, seen from the stock.</summary>
        public static float Pitch => 360f / (PinionTeeth * Ratio);

        /// <summary>
        /// How far every shaft has turned (degrees, right-handed about the way it runs from the wheel toward the rudder) with
        /// the rudder turned so far (degrees, its trailing edge to starboard for a positive helm, as <see cref="KwRudderBlade"/> turns it).
        /// </summary>
        public static float Spin(float rudder) => rudder * Ratio;

        /// <summary>A unit vector turned about a unit axis by an angle (degrees, right-handed): Rodrigues, without the engine.</summary>
        public static Vector3 Turn(Vector3 v, Vector3 axis, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return v * c + Vector3.Cross(axis, v) * s + axis * (Vector3.Dot(axis, v) * (1f - c));
        }

        /// <summary>A unit vector square to an axis, the "up" a shaft's teeth are counted from: the way up, or for an upright shaft forward.</summary>
        public static Vector3 Square(Vector3 axis)
        {
            Vector3 up = Mathf.Abs(axis.z) > 0.9f ? Vector3.right : new Vector3(0f, 0f, 1f);
            return (up - axis * Vector3.Dot(up, axis)).normalized;
        }
    }
}
