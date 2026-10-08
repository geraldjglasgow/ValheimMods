using System;
using System.Collections.Generic;
using System.Linq;
using PatchGuard;
using Charter;
using SyncedConfig;

namespace Lockstep
{
    /// <summary>
    /// The per-stage summary the server publishes, as every client sees it: one line per stage,
    /// <c>bossPrefab TAB stageName TAB previousStageName TAB waitingNames</c>.
    /// </summary>
    public static class ProgressState
    {
        public sealed class StageStatus
        {
            public string BossPrefab;
            public string Name;
            public string PreviousName;
            public string Waiting;
            public bool Open => string.IsNullOrEmpty(Waiting);
        }

        private static Article<string> article;
        private static readonly Dictionary<string, StageStatus> byBoss = new Dictionary<string, StageStatus>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<StageStatus> Stages => byBoss.Values;

        /// <summary>Counts the summaries received, so text built from one knows when it is stale.</summary>
        public static int Revision { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            article = new Article<string>(config.Sync, "lockstep_state", "", standing: true);
            article.Changed += () => Guard.Run("progress state changed", Parse);
        }

        /// <summary>Server only: publishes a new summary.</summary>
        public static void Assign(string text)
        {
            if (article.Value != text)
                article.Assign(text);
        }

        private static void Parse()
        {
            Revision++;
            byBoss.Clear();
            foreach (string line in (article.Value ?? "").Split('\n'))
            {
                string[] parts = line.Split('\t');
                if (parts.Length < 4 || parts[0].Length == 0)
                    continue;
                byBoss[parts[0]] = new StageStatus { BossPrefab = parts[0], Name = parts[1], PreviousName = parts[2], Waiting = parts[3] };
            }
        }

        /// <summary>Status of the stage a boss prefab belongs to, or null when the boss is not in the chain.</summary>
        public static StageStatus ForBoss(string bossPrefab) =>
            bossPrefab != null && byBoss.TryGetValue(bossPrefab, out StageStatus status) ? status : null;

        /// <summary>The message shown at a closed altar.</summary>
        public static string ClosedMessage(StageStatus status)
        {
            return $"{status.Name} will not answer. Waiting for {Who(status)} to defeat {status.PreviousName}.";
        }

        /// <summary>The players a closed stage waits for, or "the group" when names are hidden.</summary>
        public static string Who(StageStatus status) =>
            LockstepConfiguration.NameMissingPlayers.Value ? status.Waiting : "the group";

        public static string Summary()
        {
            if (byBoss.Count == 0)
                return "Lockstep: no progression state received from the server yet.";
            return "Lockstep status\n" + string.Join("\n", byBoss.Values.Select(s =>
                $"  {s.Name}: {(s.Open ? "open" : $"waiting for {s.Waiting}")}"));
        }
    }
}
