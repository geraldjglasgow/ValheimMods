using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The wind a moving ship's sails feel: the game's wind (where it blows to and how hard) less the ship's own speed, both
    /// level, against the ship's heading. The game's wind has no speed of its own, so it is taken as blowing at
    /// <see cref="Strongest"/> in the game's strongest wind and as much slower in a lighter one as a ship sails slower in
    /// it (its push's square root, the game's push being a quarter of its strongest in no wind at all): the apparent wind
    /// comes round toward the bow by as much in any wind. Its <see cref="Angle"/> off the bow and the side it comes from
    /// decide every sail's drive (<see cref="KwSailPolar"/>) and how the fore-and-aft sails swing to it (<see cref="KwSailTrim"/>);
    /// its <see cref="Strength"/> is the game's sail push in that wind, a little more when the apparent wind blows harder
    /// than the true wind (reaching or beating), never less (so a run is as fast as before). Pure arithmetic.
    /// </summary>
    public readonly struct KwApparentWind
    {
        /// <summary>The wind's speed in the game's strongest wind, metres a second (a little over twice a karve's best).</summary>
        public const float Strongest = 22f;

        /// <summary>The most the apparent wind's own speed adds to the push.</summary>
        public const float MostGain = 1.25f;

        public KwApparentWind(Vector3 windTo, float intensity, Vector3 velocity, Vector3 forward)
        {
            float push = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(intensity)), speed = Strongest * Mathf.Sqrt(push);
            Vector3 wind = Level(windTo) * speed, apparent = wind - new Vector3(velocity.x, 0f, velocity.z);
            Vector3 ahead = Level(forward), right = new Vector3(ahead.z, 0f, -ahead.x);
            float from = -Vector3.Dot(apparent, ahead), aside = -Vector3.Dot(apparent, right);
            Angle = wind.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(Mathf.Abs(aside), from) * Mathf.Rad2Deg;
            FromStarboard = aside > 0f;
            Across = speed > 0f ? aside / Mathf.Max(apparent.magnitude, 1e-3f) : 0f;
            Strength = push * (speed > 0f ? Mathf.Clamp(apparent.magnitude / speed, 1f, MostGain) : 1f);
            Leeward = FromStarboard ? -right : right;
            Ahead = ahead;
        }

        /// <summary>How far off the bow the apparent wind comes from, degrees: 0 right ahead, 180 dead astern.</summary>
        public float Angle { get; }

        /// <summary>Whether it comes from the starboard side (the sails then lie to port).</summary>
        public bool FromStarboard { get; }

        /// <summary>The sine of its angle off the bow, signed: plus from starboard, minus from port.</summary>
        public float Across { get; }

        /// <summary>The game's sail push in this wind, the apparent wind's gain with it.</summary>
        public float Strength { get; }

        /// <summary>The way to leeward, level, in the frame the heading was given in.</summary>
        public Vector3 Leeward { get; }

        /// <summary>The heading, level.</summary>
        public Vector3 Ahead { get; }

        private static Vector3 Level(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;
    }
}
