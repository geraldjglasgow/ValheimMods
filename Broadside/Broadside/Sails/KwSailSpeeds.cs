using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How fast a ship sails with the true wind at each angle off its bow, steady: where its sails' push (each kind's by
    /// the apparent wind at that speed, <see cref="KwSailMix"/>, <see cref="KwApparentWind"/>) meets the drag along the
    /// ship, as the launched ship's step has them (<see cref="KwShipDrive"/>: both a speed change each physics step, the
    /// push the sail force times the wind's, the drag a share of the speed squared). Full sail. For the blueprint table's
    /// figures (<see cref="KwBpHandling"/>) and the offline checks; pure arithmetic.
    /// </summary>
    public static class KwSailSpeeds
    {
        /// <summary>The speed in m/s with the true wind that many degrees off the bow, in a wind of that intensity (0 to 1).</summary>
        public static float At(KwSailMix mix, float sailFactor, float drag, float angle, float intensity = 1f)
        {
            Vector3 windTo = WindTo(angle);
            float lo = 0f, hi = Mathf.Sqrt(sailFactor * KwApparentWind.MostGain / Mathf.Max(drag, 1e-9f)) + 1f;
            if (Excess(mix, sailFactor, drag, windTo, intensity, 0.01f) <= 0f)
                return 0f;
            for (int i = 0; i < 40; i++)
            {
                float mid = (lo + hi) / 2f;
                if (Excess(mix, sailFactor, drag, windTo, intensity, mid) > 0f)
                    lo = mid;
                else
                    hi = mid;
            }
            return lo;
        }

        /// <summary>The fastest of every whole degree off the wind, and that angle.</summary>
        public static (float Speed, float Angle) Best(KwSailMix mix, float sailFactor, float drag)
        {
            (float speed, float angle) best = (0f, 180f);
            for (int degrees = 0; degrees <= 180; degrees++)
            {
                float v = At(mix, sailFactor, drag, degrees);
                if (v > best.speed + 1e-4f)
                    best = (v, degrees);
            }
            return best;
        }

        /// <summary>The closest the ship sails to the true wind at a third of its best speed or more, degrees; 180 when it never does.</summary>
        public static float Closest(KwSailMix mix, float sailFactor, float drag, float best)
        {
            for (int degrees = 0; degrees <= 180; degrees++)
            {
                if (best > 0f && At(mix, sailFactor, drag, degrees) >= best / 3f)
                    return degrees;
            }
            return 180f;
        }

        /// <summary>The push less the drag at a speed: the speed change a physics step.</summary>
        public static float Excess(KwSailMix mix, float sailFactor, float drag, Vector3 windTo, float intensity, float speed)
        {
            KwApparentWind air = new KwApparentWind(windTo, intensity, Vector3.forward * speed, Vector3.forward);
            return sailFactor * air.Strength * mix.Drive(air.Angle, false) - drag * speed * speed;
        }

        /// <summary>Where a wind that many degrees off the starboard bow of a ship heading +z blows to.</summary>
        public static Vector3 WindTo(float angle)
        {
            float a = angle * Mathf.Deg2Rad;
            return -new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }
    }
}
