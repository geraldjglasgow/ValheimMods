using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// The film's clock: one recorded frame per rendered frame while recording (Time.captureDeltaTime), real time
    /// otherwise. Camera moves and cues run on it, so slow motion slows the world but never the camera.
    /// </summary>
    internal static class FilmClock
    {
        private static int lastFrame = -1;

        /// <summary>Film seconds since the previous frame.</summary>
        internal static float Delta { get; private set; }

        /// <summary>Called once a frame, before anything reads Delta.</summary>
        internal static void Tick()
        {
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            Delta = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        }
    }
}
