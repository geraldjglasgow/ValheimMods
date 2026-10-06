using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// A time budget for burst work spread over frames (preview and ghost copies, building at once): work runs until
    /// <see cref="BlueprintRules.FrameSeconds"/> of the frame are spent, so a slow machine does fewer jobs a frame rather
    /// than hitch. At least one job runs each frame, so the work always moves on.
    /// </summary>
    public static class FrameBudget
    {
        /// <summary>The moment this frame's budget runs out.</summary>
        public static double Until() => Time.realtimeSinceStartupAsDouble + BlueprintRules.FrameSeconds;

        /// <summary>Time is left before <paramref name="until"/>.</summary>
        public static bool Left(double until) => Time.realtimeSinceStartupAsDouble < until;
    }
}
