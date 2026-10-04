using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Events
{
    /// <summary>How the game patches publish: a failure is swallowed so the game's own code goes on, and logged once per
    /// source so a patch that fails every frame cannot flood the log (and the log events with it).</summary>
    internal static class Publish
    {
        private static readonly HashSet<string> Failed = new HashSet<string>();

        internal static void Safely(string source, Action publish)
        {
            try
            {
                publish();
            }
            catch (Exception error)
            {
                Warn(source, error);
            }
        }

        private static void Warn(string source, Exception error)
        {
            lock (Failed)
            {
                if (!Failed.Add(source)) return;
            }
            Debug.LogWarning($"[DevBridge] events ({source}): {error.GetType().Name}: {error.Message} (later failures here are not logged)");
        }
    }
}
