using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// A player's machine sharing blueprints: each file of the library is read (it must be a blueprint the tab can read),
    /// packed and sent to the server in parts, <see cref="BenchLimits.PartsPerFrame"/> a frame, to the path in the
    /// player's part of the pool the window chose for it; sharing to a path again replaces it. The server answers every
    /// file; once all are answered one message sums it up. A server that stops answering is given up after
    /// <see cref="BenchLimits.Timeout"/> seconds.
    /// </summary>
    public static class BenchShare
    {
        private static readonly Queue<(string Local, string Dest)> waiting = new Queue<(string, string)>();
        private static readonly Queue<ZPackage> parts = new Queue<ZPackage>();
        private static int sent;
        private static int answered;
        private static int shared;
        private static string firstRefusal;
        private static float lastHeard;

        public static bool Busy => waiting.Count > 0 || parts.Count > 0 || answered < sent;

        /// <summary>Blueprints not yet answered, for the window's status line.</summary>
        public static int Left => waiting.Count + sent - answered;

        /// <summary>Queues library blueprints (Local) for the pool paths they go to in the player's part (Dest).</summary>
        public static void Share(IEnumerable<(string Local, string Dest)> files)
        {
            foreach ((string Local, string Dest) file in files)
            {
                if (!waiting.Contains(file))
                    waiting.Enqueue(file);
            }
            lastHeard = Time.unscaledTime;
        }

        /// <summary>Every blueprint of the player's library, each to the same path in their part (Share all).</summary>
        public static List<(string, string)> AllOwn() => BlueprintLibrary.Everything(false).Select(p => (p, p)).ToList();

        public static void Tick()
        {
            if (!Busy)
                return;
            if (parts.Count == 0 && waiting.Count > 0)
                Prepare(waiting.Dequeue());
            for (int i = 0; i < BenchLimits.PartsPerFrame && parts.Count > 0; i++)
                ZRoutedRpc.instance?.InvokeRoutedRPC(BenchRpc.Put, parts.Dequeue());
            if (waiting.Count == 0 && parts.Count == 0 && answered < sent && Time.unscaledTime - lastHeard > BenchLimits.Timeout)
                GiveUp();
        }

        private static void Prepare((string Local, string Dest) share)
        {
            sent++;
            lastHeard = Time.unscaledTime;
            byte[] packed = Pack(share.Local, share.Dest, out string refused);
            if (packed == null)
            {
                Answer(share.Local, refused);
                return;
            }
            List<byte[]> pieces = BenchZip.Split(packed);
            for (int i = 0; i < pieces.Count; i++)
                parts.Enqueue(Part(share.Dest, i, pieces.Count, pieces[i]));
        }

        /// <summary>The file packed for sending, or null with the reason's word.</summary>
        private static byte[] Pack(string path, string dest, out string refused)
        {
            FileInfo file = new FileInfo(BlueprintLibrary.PathOf(path));
            refused = BenchPaths.Clean(dest) == null ? BlueprintWords.BadName
                : !file.Exists ? BenchWords.NotBlueprint
                : file.Length > BenchLimits.MaxFileBytes ? BenchWords.TooBig
                : BlueprintLibrary.Load(path) == null ? BenchWords.NotBlueprint : null;
            if (refused != null)
                return null;
            byte[] packed = BenchZip.Pack(File.ReadAllBytes(file.FullName));
            if (packed.Length <= BenchLimits.MaxParts * BenchLimits.PartBytes)
                return packed;
            refused = BenchWords.TooBig;
            return null;
        }

        private static ZPackage Part(string path, int part, int count, byte[] bytes)
        {
            ZPackage package = new ZPackage();
            package.Write(path);
            package.Write(part);
            package.Write(count);
            package.Write(bytes);
            return package;
        }

        /// <summary>The server's answer for one file: no word when it was stored.</summary>
        public static void OnDone(long sender, string path, string word)
        {
            if (BenchRpc.FromServer(sender) && answered < sent)
                Answer(path, string.IsNullOrEmpty(word) ? null : word);
        }

        private static void Answer(string path, string refused)
        {
            answered++;
            lastHeard = Time.unscaledTime;
            if (refused == null)
                shared++;
            else if (firstRefusal == null)
                firstRefusal = BlueprintWords.Format(refused, path);
            if (!Busy)
                Report();
        }

        private static void GiveUp()
        {
            waiting.Clear();
            parts.Clear();
            firstRefusal = Language.Localize(BenchWords.NoAnswer);
            answered = sent;
            Report();
        }

        /// <summary>One message for the whole batch: how many were shared, the first refusal and how many more there were.</summary>
        private static void Report()
        {
            int refusals = sent - shared;
            List<string> lines = new List<string>();
            if (shared > 0)
                lines.Add(BlueprintWords.Format(BenchWords.Shared, shared));
            if (firstRefusal != null)
                lines.Add(firstRefusal);
            if (refusals > 1)
                lines.Add(BlueprintWords.Format(BenchWords.MoreRefused, refusals - 1));
            Messages.TopLeft(string.Join("\n", lines));
            sent = answered = shared = 0;
            firstRefusal = null;
        }
    }
}
