using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PatchGuard;
using UnityEngine;

namespace Lockstep
{
    /// <summary>
    /// Server side: the roster, who gets credit when a boss dies, the gating rule, and the summary pushed to clients.
    /// Every method is a no-op unless this instance is the server (dedicated, or the hosting player).
    /// The admin console verbs live in <see cref="ServerCommands"/>.
    /// </summary>
    public static class ProgressServer
    {
        // Renamed from Lockstep_BossDefeated when the report gained the boss's ZDOID: an old client and a new server (or
        // the other way round) ignore each other's report instead of misreading it.
        public const string RpcBossDefeated = "Lockstep_BossKilled";
        public const string RpcCommand = "Lockstep_Command";
        public const string RpcReply = "Lockstep_Reply";

        private static Roster roster;

        public static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

        /// <summary>Registers the routed RPCs. Called on every instance; the server handlers check <see cref="IsServer"/>.</summary>
        public static void RegisterRpcs()
        {
            ZRoutedRpc.instance.Register<ZDOID, string, string>(RpcBossDefeated,
                (sender, boss, key, attackers) => Guard.Run("boss defeated", () => OnBossDefeated(sender, boss, key, attackers)));
            ZRoutedRpc.instance.Register<string>(RpcCommand,
                (sender, line) => Guard.Run("command", () => ServerCommands.OnCommand(sender, line)));
            ZRoutedRpc.instance.Register<string>(RpcReply,
                (sender, text) => Guard.Run("reply", () => Commands.Print(text)));
        }

        /// <summary>Forgets the loaded roster when the world is left, so the next world loads its own.</summary>
        public static void Shutdown()
        {
            roster?.Dispose();
            roster = null;
            KillWitness.Clear();
        }

        internal static Roster EnsureRoster()
        {
            if (roster != null)
                return roster;
            roster = new Roster(ZNet.instance.GetWorldName());
            if (!roster.Load())
            {
                // First run in this world: bosses the world has already defeated count as cleared by everyone.
                roster.Data.InstalledKeys = Chain.Stages.Where(s => IsWorldKeySet(s.Key)).Select(s => s.Key).ToList();
                roster.Save();
                Lockstep.Log.LogInfo($"Created roster {roster.FilePath}" +
                    (roster.Data.InstalledKeys.Count > 0 ? $", already defeated: {string.Join(", ", roster.Data.InstalledKeys)}" : ""));
            }
            roster.Changed += Publish;
            roster.Watch();
            return roster;
        }

        private static bool IsWorldKeySet(string key) => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key);

        // ------------------------------------------------------------------ roster events

        /// <summary>A player connected (or the host spawned). Adds them to the roster and applies late-joiner catch-up.</summary>
        public static void PlayerSeen(long id, string name)
        {
            if (!IsServer || id == 0)
                return;
            Roster r = EnsureRoster();
            RosterEntry entry = r.Touch(id, name, out bool isNew);
            if (isNew && LockstepConfiguration.LateJoiners.Value == LateJoinerMode.Catchup)
            {
                foreach (Stage stage in Chain.Stages)
                {
                    if (IsWorldKeySet(stage.Key) && !entry.Cleared.Contains(stage.Key))
                        entry.Cleared.Add(stage.Key);
                }
                if (entry.Cleared.Count > 0)
                    Lockstep.Log.LogInfo($"{name} joined and caught up with the group: {string.Join(", ", entry.Cleared)}");
            }
            r.Save();
            Publish();
        }

        public static void PlayerLeft(long id)
        {
            if (!IsServer || id == 0)
                return;
            RosterEntry entry = EnsureRoster().Find(id);
            if (entry != null)
            {
                entry.LastSeen = Roster.Now;
                roster.Save();
            }
            Publish();
        }

        // ------------------------------------------------------------------ credit

        internal struct OnlinePlayer
        {
            public long Id;
            public string Name;
            public Vector3 Position;
        }

        internal static IEnumerable<OnlinePlayer> Online()
        {
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.m_playerID != 0)
                    yield return new OnlinePlayer { Id = peer.m_playerID, Name = peer.m_playerName, Position = peer.m_refPos };
            }
            Player local = Player.m_localPlayer;
            if (local != null && !ZNet.instance.IsDedicated())
                yield return new OnlinePlayer { Id = local.GetPlayerID(), Name = local.GetPlayerName(), Position = local.transform.position };
        }

        /// <summary>A boss's owner says it died; <see cref="KillWitness"/> checks that against the server's world first.</summary>
        private static void OnBossDefeated(long sender, ZDOID boss, string key, string attackers)
        {
            if (!IsServer)
                return;
            Stage stage = Chain.ByKey(key);
            if (stage == null)
            {
                Lockstep.Log.LogInfo($"A boss with key {key} died; it is not in the chain.");
                return;
            }
            KillWitness.Reported(sender, boss, stage, attackers);
        }

        /// <summary>A kill the server has checked: credits the players who earned it and pushes the new summary.</summary>
        internal static void Credit(Stage stage, string attackers, Vector3 position)
        {
            Roster r = EnsureRoster();
            List<string> credited = CreditPlayers(r, stage.Key, attackers, position);
            r.Save();
            Lockstep.Log.LogInfo($"{stage.Name} defeated. Credited: {(credited.Count > 0 ? string.Join(", ", credited) : "nobody new")}.");
            Publish();
        }

        /// <summary>Credits every online player who earned the kill and returns the names that were newly credited.</summary>
        private static List<string> CreditPlayers(Roster r, string key, string attackers, Vector3 position)
        {
            HashSet<string> attackerNames = new HashSet<string>(attackers.Split('\n').Where(n => n.Length > 0), StringComparer.Ordinal);
            List<string> credited = new List<string>();
            foreach (OnlinePlayer player in Online())
            {
                if (!EarnedCredit(player, attackerNames, position))
                    continue;
                RosterEntry entry = r.Touch(player.Id, player.Name, out _);
                if (!entry.Cleared.Contains(key))
                {
                    entry.Cleared.Add(key);
                    credited.Add(player.Name);
                }
            }
            return credited;
        }

        /// <summary>The credit rules: everyone online, hit the boss, or stood within the credit radius.</summary>
        private static bool EarnedCredit(OnlinePlayer player, HashSet<string> attackerNames, Vector3 position)
        {
            if (LockstepConfiguration.CreditEveryoneOnline.Value || attackerNames.Contains(player.Name))
                return true;
            float radius = LockstepConfiguration.CreditRadius.Value;
            return radius > 0 && Vector3.Distance(player.Position, position) <= radius;
        }

        // ------------------------------------------------------------------ the rule

        internal static bool Counts(RosterEntry entry, HashSet<long> online)
        {
            if (entry.Ignored)
                return false;
            if (online.Contains(entry.Id))
                return true;
            if (LockstepConfiguration.CountOnlyOnline.Value)
                return false;
            int days = LockstepConfiguration.InactiveDays.Value;
            return days <= 0 || (DateTime.UtcNow - entry.LastSeenUtc).TotalDays <= days;
        }

        /// <summary>Names of counted players who still miss the previous stage. Empty means the stage is open.</summary>
        public static List<string> WaitingFor(Stage stage) => StillMissing(Chain.Previous(stage), CountedPlayers());

        /// <summary>Counted players without credit for <paramref name="previous"/>; nobody for the first stage.</summary>
        private static List<string> StillMissing(Stage previous, List<RosterEntry> counted)
        {
            if (previous == null || EnsureRoster().Data.InstalledKeys.Contains(previous.Key))
                return new List<string>();
            return counted.Where(e => !e.Cleared.Contains(previous.Key)).Select(e => e.Name).ToList();
        }

        /// <summary>The roster entries that hold the group back, worked out once per summary rather than once per stage.</summary>
        private static List<RosterEntry> CountedPlayers()
        {
            HashSet<long> online = new HashSet<long>(Online().Select(p => p.Id));
            return EnsureRoster().Data.Players.Where(e => Counts(e, online)).ToList();
        }

        /// <summary>Pushes the per-stage summary to every client through the Charter article.</summary>
        public static void Publish()
        {
            // The roster is created by the first player event, once the world (and its global keys) is loaded.
            if (!IsServer || roster == null)
                return;
            StringBuilder text = new StringBuilder();
            List<RosterEntry> counted = CountedPlayers();
            IReadOnlyList<Stage> stages = Chain.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                Stage stage = stages[i], previous = i > 0 ? stages[i - 1] : null;
                text.Append(stage.BossPrefab).Append('\t')
                    .Append(stage.Name).Append('\t')
                    .Append(previous?.Name ?? "").Append('\t')
                    .Append(string.Join(", ", StillMissing(previous, counted))).Append('\n');
            }
            ProgressState.Assign(text.ToString());
        }
    }
}
