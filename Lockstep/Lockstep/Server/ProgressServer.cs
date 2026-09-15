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
        public const string RpcBossDefeated = "Lockstep_BossDefeated";
        public const string RpcCommand = "Lockstep_Command";
        public const string RpcReply = "Lockstep_Reply";

        private static Roster roster;

        public static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

        /// <summary>Registers the routed RPCs. Called on every instance; the server handlers check <see cref="IsServer"/>.</summary>
        public static void RegisterRpcs()
        {
            ZRoutedRpc.instance.Register<string, string, Vector3>(RpcBossDefeated,
                (sender, key, attackers, position) => Guard.Run("boss defeated", () => OnBossDefeated(key, attackers, position)));
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

        private static void OnBossDefeated(string key, string attackers, Vector3 position)
        {
            if (!IsServer)
                return;
            Stage stage = Chain.ByKey(key);
            if (stage == null)
            {
                Lockstep.Log.LogInfo($"A boss with key {key} died; it is not in the chain.");
                return;
            }
            Roster r = EnsureRoster();
            List<string> credited = CreditPlayers(r, key, attackers, position);
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
        public static List<string> WaitingFor(Stage stage)
        {
            Stage previous = Chain.Previous(stage);
            if (previous == null)
                return new List<string>();
            Roster r = EnsureRoster();
            if (r.Data.InstalledKeys.Contains(previous.Key))
                return new List<string>();
            HashSet<long> online = new HashSet<long>(Online().Select(p => p.Id));
            return r.Data.Players
                .Where(e => Counts(e, online) && !e.Cleared.Contains(previous.Key))
                .Select(e => e.Name)
                .ToList();
        }

        /// <summary>Pushes the per-stage summary to every client through the Charter article.</summary>
        public static void Publish()
        {
            // The roster is created by the first player event, once the world (and its global keys) is loaded.
            if (!IsServer || roster == null)
                return;
            StringBuilder text = new StringBuilder();
            foreach (Stage stage in Chain.Stages)
            {
                Stage previous = Chain.Previous(stage);
                text.Append(stage.BossPrefab).Append('\t')
                    .Append(stage.Name).Append('\t')
                    .Append(previous?.Name ?? "").Append('\t')
                    .Append(string.Join(", ", WaitingFor(stage))).Append('\n');
            }
            ProgressState.Assign(text.ToString());
        }
    }
}
