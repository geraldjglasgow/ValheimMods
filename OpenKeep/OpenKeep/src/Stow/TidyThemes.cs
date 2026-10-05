using System;
using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Stow
{
    /// <summary>
    /// Which items Auto Tidy treats as alike: the same prefab, a shared label (<see cref="TidyLabels"/>), or a pair the
    /// base taught it. A pair is learned when players keep the two items together in at least <see cref="MinChests"/>
    /// chests: both put in by hand, both held there for at least a day, in a chest of at most
    /// <see cref="MaxKinds"/> such items (a junk chest teaches nothing). Learning reads the memories of every loaded
    /// chest, whoever owns it, at most once every <see cref="RelearnSeconds"/>; <see cref="Version"/> changes when
    /// the learned pairs do, so kept profiles are scored again.
    /// </summary>
    internal static class TidyThemes
    {
        private const int MinChests = 2;
        private const int MaxKinds = 8;
        private const float RelearnSeconds = 30f;

        private static HashSet<string> learned = new HashSet<string>(StringComparer.Ordinal);
        private static float learnedAt = float.MinValue;

        public static int Version { get; private set; }

        public static int Learned => learned.Count;

        public static bool Alike(string a, string b) =>
            a == b || TidyLabels.Shared(a, b) || (learned.Count > 0 && learned.Contains(Pair(a, b)));

        /// <summary>Learns again from the loaded chests when the last time is long enough ago.</summary>
        public static void Refresh()
        {
            if (Time.time - learnedAt < RelearnSeconds)
                return;
            learnedAt = Time.time;
            HashSet<string> fresh = Learn();
            if (fresh.SetEquals(learned))
                return;
            learned = fresh;
            Version++;
        }

        private static HashSet<string> Learn()
        {
            Dictionary<string, int> together = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Container chest in ContainerScan.All())
            {
                if (!TidyChests.TakesPart(chest) || !TidyChests.IsLoaded(chest))
                    continue;
                List<string> kept = KeptByHand(TidyProfiles.MemoryOf(chest));
                if (kept.Count >= 2 && kept.Count <= MaxKinds)
                    Tally(kept, together);
            }
            HashSet<string> pairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in together)
            {
                if (pair.Value >= MinChests)
                    pairs.Add(pair.Key);
            }
            return pairs;
        }

        /// <summary>The prefabs a chest held at its last look that a player put there and that have been there a day.</summary>
        private static List<string> KeptByHand(TidyMemory memory)
        {
            double now = TidyProfiles.Now;
            double settle = TidyProfiles.SettleSeconds;
            List<string> kept = new List<string>();
            foreach (KeyValuePair<string, TidyMemory.Entry> pair in memory.Entries)
            {
                TidyMemory.Entry entry = pair.Value;
                if (entry.Hand && entry.Last > 0f && now - entry.FirstSeen >= settle)
                    kept.Add(pair.Key);
            }
            return kept;
        }

        private static void Tally(List<string> kept, Dictionary<string, int> together)
        {
            for (int i = 0; i < kept.Count; i++)
            {
                for (int j = i + 1; j < kept.Count; j++)
                {
                    string key = Pair(kept[i], kept[j]);
                    together.TryGetValue(key, out int count);
                    together[key] = count + 1;
                }
            }
        }

        private static string Pair(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
    }
}
