using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Error logging for the brush's per-frame patches: one failure is logged, then the same context stays quiet for
    /// five seconds, so a broken frame does not flood the log.
    /// </summary>
    public static class BrushLog
    {
        private static readonly Dictionary<string, float> quietUntil = new Dictionary<string, float>();

        public static void Error(string context, Exception e)
        {
            float now = Time.realtimeSinceStartup;
            if (quietUntil.TryGetValue(context, out float until) && now < until)
                return;
            quietUntil[context] = now + 5f;
            Plugin.Log.LogError($"EarthWright {context}: {e}");
        }
    }
}
