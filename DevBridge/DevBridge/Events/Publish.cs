using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Events
{
    /// <summary>How the game patches publish: a failure is swallowed so the game's own code goes on, and logged once per
    /// source so a patch that fails every frame cannot flood the log (and the log events with it). The patches pass
    /// their arguments in and call with a static lambda, which captures nothing and so allocates nothing: a lambda that
    /// captures a patch parameter is allocated at the patch's entry, on every call, before any early return in it.
    /// Other per-call features (/hitbox) log their failures through WarnOnce, on the same once-per-source set.</summary>
    internal static class Publish
    {
        private static readonly HashSet<string> Failed = new HashSet<string>();

        internal static void Safely<T>(string source, Action<T> publish, T arg)
        {
            try
            {
                publish(arg);
            }
            catch (Exception error)
            {
                Warn(source, error);
            }
        }

        internal static void Safely<T1, T2>(string source, Action<T1, T2> publish, T1 first, T2 second)
        {
            try
            {
                publish(first, second);
            }
            catch (Exception error)
            {
                Warn(source, error);
            }
        }

        internal static void Safely<T1, T2, T3>(string source, Action<T1, T2, T3> publish, T1 first, T2 second, T3 third)
        {
            try
            {
                publish(first, second, third);
            }
            catch (Exception error)
            {
                Warn(source, error);
            }
        }

        private static void Warn(string source, Exception error) => WarnOnce("events (" + source + ")", error);

        /// <summary>Logs a failure the first time that source has one; later failures there are not logged.</summary>
        internal static void WarnOnce(string source, Exception error)
        {
            lock (Failed)
            {
                if (!Failed.Add(source)) return;
            }
            Debug.LogWarning($"[DevBridge] {source}: {error.GetType().Name}: {error.Message} (later failures here are not logged)");
        }
    }
}
