using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// A player's machine taking copies from the pool: the files are asked for <see cref="BenchLimits.TakesInFlight"/> at
    /// a time, arrive packed in parts and are written into the library at the path the window chose (the folder the
    /// player's own list shows, a taken folder keeping what is inside it); a name already taken there gets " (2)", " (3)"
    /// and so on, so nothing of the player's is ever overwritten. One message sums up the batch. A file the server stops
    /// sending is given up after <see cref="BenchLimits.Timeout"/> seconds.
    /// </summary>
    public static class BenchTake
    {
        private static readonly Queue<(long Owner, string Path, string Dest)> waiting = new Queue<(long, string, string)>();
        private static readonly Dictionary<string, float> asked = new Dictionary<string, float>();
        private static readonly Dictionary<string, string> destinations = new Dictionary<string, string>();
        private static readonly BenchInbox inbox = new BenchInbox();
        private static int took;
        private static int failed;
        private static string into;

        public static bool Busy => waiting.Count > 0 || asked.Count > 0;

        /// <summary>Blueprints still to come, for the window's status line.</summary>
        public static int Left => waiting.Count + asked.Count;

        /// <summary>Queues pool blueprints of one player (Path) for library paths (Dest); <paramref name="folder"/> names the batch.</summary>
        public static void Take(long owner, IEnumerable<(string Path, string Dest)> files, string folder)
        {
            foreach ((string path, string dest) in files)
            {
                if (!asked.ContainsKey(Key(owner, path)) && !waiting.Any(w => w.Owner == owner && w.Path == path))
                    waiting.Enqueue((owner, path, dest));
            }
            into = folder;
        }

        public static void Tick()
        {
            if (!Busy)
                return;
            while (asked.Count < BenchLimits.TakesInFlight && waiting.Count > 0)
            {
                (long owner, string path, string dest) = waiting.Dequeue();
                asked[Key(owner, path)] = Time.unscaledTime;
                destinations[Key(owner, path)] = dest;
                ZRoutedRpc.instance?.InvokeRoutedRPC(BenchRpc.Get, owner, path);
            }
            GiveUpLate();
        }

        private static string Key(long owner, string path) => owner + "|" + path;

        private static void GiveUpLate()
        {
            float now = Time.unscaledTime;
            List<string> late = asked.Where(a => now - a.Value > BenchLimits.Timeout).Select(a => a.Key).ToList();
            foreach (string key in late)
                asked.Remove(key);
            failed += late.Count;
            if (late.Count > 0 && !Busy)
                Report();
        }

        /// <summary>One part from the server: owner, owner's name, path, part, parts (none: the file is gone), bytes.</summary>
        public static void OnFile(long sender, ZPackage package)
        {
            if (!BenchRpc.FromServer(sender))
                return;
            long owner = package.ReadLong();
            package.ReadString();
            string path = package.ReadString();
            int part = package.ReadInt();
            int parts = package.ReadInt();
            byte[] bytes = package.ReadByteArray();
            string key = Key(owner, path);
            if (!asked.ContainsKey(key))
                return;
            asked[key] = Time.unscaledTime;
            byte[] packed = parts > 0 ? inbox.Add(key, part, parts, bytes) : null;
            if (parts > 0 && packed == null)
                return;
            asked.Remove(key);
            if (packed != null && destinations.TryGetValue(key, out string dest) && Save(dest, packed))
                took++;
            else
                failed++;
            if (!Busy)
                Report();
        }

        /// <summary>Writes a taken copy into the library at a free name; false when it cannot be read or written.</summary>
        private static bool Save(string dest, byte[] packed)
        {
            byte[] json = BenchZip.Unpack(packed, BenchLimits.MaxFileBytes);
            if (json == null || BenchPaths.Clean(dest) == null)
                return false;
            try
            {
                string file = BlueprintLibrary.PathOf(Free(dest));
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllBytes(file, json);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Plugin.Log.LogWarning($"OpenKeep: cannot write the blueprint {dest} taken from the bench: {e.Message}");
                return false;
            }
            BlueprintLibrary.Rescan();
            return true;
        }

        /// <summary>The path itself when no blueprint has it, else "name (2)", "name (3)" ... in the same folder.</summary>
        public static string Free(string path)
        {
            if (!File.Exists(BlueprintLibrary.PathOf(path)))
                return path;
            for (int n = 2; ; n++)
            {
                string candidate = path + " (" + n + ")";
                if (!File.Exists(BlueprintLibrary.PathOf(candidate)))
                    return candidate;
            }
        }

        private static void Report()
        {
            List<string> lines = new List<string>();
            if (took > 0)
                lines.Add(BlueprintWords.Format(BenchWords.Took, took, BlueprintEntries.Shown(into)));
            if (failed > 0)
                lines.Add(BlueprintWords.Format(BenchWords.NotTaken, failed));
            Messages.TopLeft(string.Join("\n", lines));
            took = failed = 0;
            destinations.Clear();
        }
    }
}
