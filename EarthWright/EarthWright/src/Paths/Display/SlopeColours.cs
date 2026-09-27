using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// The preview colour of each segment of a centre line, by how a cart would fare on it: green up to the easy slope
    /// (20° by default), yellow up to the hard slope (25°), red beyond; all of it in the refusal red when the plan
    /// cannot be built. The thresholds are the player's own (local settings).
    /// </summary>
    public static class SlopeColours
    {
        public const int Easy = 0;
        public const int Hard = 1;
        public const int Steep = 2;
        public const int Invalid = 3;

        private static readonly Color[] Colours =
        {
            new Color(0.3f, 0.95f, 0.35f),
            new Color(1f, 0.85f, 0.2f),
            new Color(1f, 0.4f, 0.2f),
            new Color(1f, 0.05f, 0.05f),
        };

        /// <summary>The class of every segment (point i to i + 1) of the plan's centre line.</summary>
        public static int[] Classes(PathPlan plan)
        {
            int segments = Mathf.Max(0, plan.Line.Count - 1);
            int[] classes = new int[segments];
            float easy = PathSettings.CartGreenSlope.Value;
            float hard = Mathf.Max(easy, PathSettings.CartYellowSlope.Value);
            for (int i = 0; i < segments; i++)
                classes[i] = plan.Valid ? Classify(plan.Line.SlopeDegrees(i), easy, hard) : Invalid;
            return classes;
        }

        public static int Classify(float degrees, float easy, float hard)
        {
            if (degrees <= easy)
                return Easy;
            return degrees <= hard ? Hard : Steep;
        }

        public static Color Colour(int slopeClass, float alpha)
        {
            Color colour = Colours[Mathf.Clamp(slopeClass, 0, Colours.Length - 1)];
            colour.a = alpha;
            return colour;
        }
    }
}
