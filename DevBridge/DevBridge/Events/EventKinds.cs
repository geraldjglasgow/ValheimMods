using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Events
{
    /// <summary>What each event kind means and carries, for /events?kinds=list. A feature that publishes its own kind
    /// adds a line here (or calls Describe), so the list stays the one place a caller learns them.</summary>
    internal static class EventKinds
    {
        private static readonly object Gate = new object();

        private static readonly Dictionary<string, string> Meaning = new Dictionary<string, string>
        {
            ["hit"] = "damage a character took, on the machine that owns it: target, prefab, id, level, damage (final),\n" +
                      "types, raw (as it arrived), health, max, how (EnemyHit, Burning, Fall...), attacker, attacker_prefab, distance",
            ["death"] = "a character died, on its owner: name, prefab, id, level, how, killer, killer_prefab, position, boss, tamed",
            ["spawn"] = "a character appeared here: name, prefab, id, level, origin (created here, or loaded from the world\n" +
                        "or another machine), owner (me or other), position, boss, tamed",
            ["player"] = "the local player: what = spawned or respawned (first, valkyrie), died (how, killer), teleport\n" +
                         "(from, to, distance, distant), arrived (position, blocked, seconds), skill (skill, level)",
            ["boss"] = "a boss: what = appeared (loaded or summoned here), alerted (its AI noticed someone, on its owner),\n" +
                       "died (on its owner); name, prefab, id, level, position",
            ["log"] = "a BepInEx warning or error (Unity's included): level, source, text; at most 20 a second, then dropped=N",
            ["console"] = "a line the console or chat printed, without rich-text tags: source (console or chat), text",
            ["hitbox"] = "a /hitbox record while it is on: kind (swing or hit) and its shape and distances",
        };

        /// <summary>Adds or replaces the line for a kind; any thread.</summary>
        internal static void Describe(string kind, string meaning)
        {
            lock (Gate) Meaning[kind] = meaning;
        }

        /// <summary>Every described kind with how many were seen, then kinds seen that nobody described.</summary>
        internal static string List()
        {
            Dictionary<string, long> seen = EventLog.Tally();
            var lines = new List<string>();
            lock (Gate)
            {
                lines.AddRange(Meaning.Select(k => Line(k.Key, k.Value, seen)));
                IEnumerable<string> others = seen.Keys.Where(k => !Meaning.ContainsKey(k)).OrderBy(k => k);
                lines.AddRange(others.Select(k => Line(k, "(published by another feature, not described yet)", seen)));
            }
            return string.Join("\n", lines) + $"\n\nnext={EventLog.Next}";
        }

        private static string Line(string kind, string meaning, Dictionary<string, long> seen)
        {
            string count = seen.TryGetValue(kind, out long n) ? $"{n} seen" : "none yet";
            return $"{kind,-10} {meaning.Replace("\n", "\n           ")}  [{count}]";
        }
    }
}
