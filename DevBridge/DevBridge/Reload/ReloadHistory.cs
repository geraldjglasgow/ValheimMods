using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace DevBridge.Reload
{
    /// <summary>
    /// What reloads did in this game session: the assemblies they replaced (Mono cannot unload them, so they stay loaded
    /// and /eval must look past them), and per plugin how often it was reloaded and from which build of its DLL.
    /// </summary>
    internal static class ReloadHistory
    {
        private sealed class Entry
        {
            internal int Count;
            internal DateTime FileTime;
            internal DateTime LoadedAt;
        }

        private static readonly HashSet<Assembly> Superseded = new HashSet<Assembly>();
        private static readonly Dictionary<string, Entry> Plugins = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Bumped whenever an assembly is superseded, so type caches know to rebuild.</summary>
        internal static int Version { get; private set; }

        internal static int Reloads { get; private set; }

        internal static long BytesLoaded { get; private set; }

        private static DateTime? gameStarted;

        /// <summary>When this game process started, for telling a DLL built before the start from one built since (main thread).</summary>
        internal static DateTime GameStarted =>
            gameStarted ?? (gameStarted = DateTime.Now - TimeSpan.FromSeconds(Time.realtimeSinceStartup)).Value;

        internal static bool IsSuperseded(Assembly assembly) => Superseded.Contains(assembly);

        internal static void Supersede(Assembly old, long newBytes)
        {
            Superseded.Add(old);
            Version++;
            Reloads++;
            BytesLoaded += newBytes;
        }

        /// <summary>A copy loaded and then refused: never started, but loaded for good, so it is superseded at once.</summary>
        internal static void Discard(Assembly refused, long bytes)
        {
            Superseded.Add(refused);
            Version++;
            BytesLoaded += bytes;
        }

        internal static void Record(string guid, DateTime fileTime)
        {
            if (!Plugins.TryGetValue(guid, out Entry entry)) Plugins[guid] = entry = new Entry();
            entry.Count++;
            entry.FileTime = fileTime;
            entry.LoadedAt = DateTime.Now;
        }

        internal static int Count(string guid) => Plugins.TryGetValue(guid, out Entry entry) ? entry.Count : 0;

        internal static DateTime? LoadedAt(string guid) => Plugins.TryGetValue(guid, out Entry entry) ? entry.LoadedAt : (DateTime?)null;

        /// <summary>
        /// The write time of the DLL the running copy came from: the reloaded build's, or for the copy BepInEx loaded at
        /// the start the file's own time when it predates the start (a later time means it was rebuilt since: the game
        /// start then stands in, so the file counts as newer).
        /// </summary>
        internal static DateTime RunningFileTime(string guid, string file)
        {
            if (Plugins.TryGetValue(guid, out Entry entry)) return entry.FileTime;
            DateTime time = File.Exists(file) ? File.GetLastWriteTime(file) : DateTime.MinValue;
            return time <= GameStarted ? time : GameStarted;
        }

        internal static bool IsStale(string guid, string file) =>
            File.Exists(file) && File.GetLastWriteTime(file) != RunningFileTime(guid, file);
    }
}
