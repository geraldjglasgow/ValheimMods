using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>How a ramp climbs from its start height to its end height along its length.</summary>
    public enum RampProfile
    {
        /// <summary>One even slope from end to end.</summary>
        Straight = 0,
        /// <summary>An even slope with short rounded joins where it meets the ground at both ends.</summary>
        SoftJoins = 1,
        /// <summary>The slope builds up over the first part and eases off over the last: gentle landings.</summary>
        SoftEnds = 2,
        /// <summary>Flat at both ends and steepest in the middle: an S seen from the side.</summary>
        SCurve = 3,
    }

    /// <summary>
    /// The ramp profiles as pure functions: the share of the rise reached at a share of the length. Every profile
    /// starts at 0, ends at 1 and never goes back down, so a ramp never overshoots the heights of its ends.
    /// </summary>
    public static class ProfileCurve
    {
        /// <summary>Share of the length at each end over which the soft-ends profile changes its slope.</summary>
        public const float SoftEndShare = 0.3f;

        /// <summary>The largest share of the length a soft join may take at each end, so a straight middle remains.</summary>
        public const float MaxJoinShare = 0.4f;

        /// <summary>Share of the rise (0..1) at share <paramref name="t"/> of the length; <paramref name="joinShare"/> is the soft join length over the ramp length.</summary>
        public static float Fraction(RampProfile profile, float t, float joinShare)
        {
            t = Mathf.Clamp01(t);
            switch (profile)
            {
                case RampProfile.SoftJoins:
                    return Trapezoid(t, Mathf.Clamp(joinShare, 0f, MaxJoinShare));
                case RampProfile.SoftEnds:
                    return Trapezoid(t, SoftEndShare);
                case RampProfile.SCurve:
                    return t * t * t * (t * (6f * t - 15f) + 10f);
                default:
                    return t;
            }
        }

        /// <summary>
        /// The height of a slope that grows evenly from flat over the first <paramref name="share"/> of the length,
        /// stays constant, and flattens again over the last share; scaled so the full rise is reached at the end.
        /// </summary>
        public static float Trapezoid(float t, float share)
        {
            if (share <= 0.0001f)
                return t;
            float peak = 1f / (1f - share);
            if (t < share)
                return peak * t * t / (2f * share);
            if (t > 1f - share)
            {
                float rest = 1f - t;
                return 1f - peak * rest * rest / (2f * share);
            }
            return peak * (t - share / 2f);
        }

        /// <summary>The profile after this one, wrapping round.</summary>
        public static RampProfile Next(RampProfile profile) => (RampProfile)(((int)profile + 1) % 4);
    }
}
