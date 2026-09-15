using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lockstep
{
    /// <summary>
    /// Server side of the <c>lockstep</c> console command: parses the line a client sent, runs the verb against
    /// the roster and answers through the reply RPC. Only <c>status</c> is open to everyone; the rest need admin.
    /// </summary>
    public static class ServerCommands
    {
        private const string Usage = "Usage: lockstep status | grant <player> <stage> | revoke <player> <stage> | ignore <player> | unignore <player> | forget <player>";

        private static bool IsAdmin(long sender)
        {
            if (sender == ZNet.GetUID())
                return true;
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            return peer != null && ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
        }

        private static void Reply(long sender, string text) => ZRoutedRpc.instance.InvokeRoutedRPC(sender, ProgressServer.RpcReply, text);

        public static void OnCommand(long sender, string line)
        {
            if (!ProgressServer.IsServer)
                return;
            string[] args = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string verb = args.Length > 0 ? args[0].ToLowerInvariant() : "status";
            if (verb == "status")
            {
                Reply(sender, Status());
                return;
            }
            if (!IsAdmin(sender))
            {
                Reply(sender, "Lockstep: only admins can change the roster.");
                return;
            }
            Reply(sender, RunAdminVerb(verb, args));
        }

        private static string RunAdminVerb(string verb, string[] args)
        {
            switch (verb)
            {
                case "grant": return Grant(args);
                case "revoke": return Revoke(args);
                case "ignore": return Ignore(args);
                case "unignore": return Unignore(args);
                case "forget": return Forget(args);
                default: return Usage;
            }
        }

        // ------------------------------------------------------------------ verbs

        private static string Grant(string[] args)
        {
            string error = FindPlayerAndStage("grant", args, out RosterEntry entry, out Stage stage);
            if (error != null)
                return error;
            if (!entry.Cleared.Contains(stage.Key))
                entry.Cleared.Add(stage.Key);
            SaveAndPublish();
            return $"{entry.Name}: {stage.Name} granted.";
        }

        private static string Revoke(string[] args)
        {
            string error = FindPlayerAndStage("revoke", args, out RosterEntry entry, out Stage stage);
            if (error != null)
                return error;
            entry.Cleared.Remove(stage.Key);
            SaveAndPublish();
            return $"{entry.Name}: {stage.Name} revoked.";
        }

        private static string Ignore(string[] args) => SetIgnored("ignore", args, true);

        private static string Unignore(string[] args) => SetIgnored("unignore", args, false);

        private static string SetIgnored(string verb, string[] args, bool ignored)
        {
            string error = FindPlayer(verb, args, out RosterEntry entry);
            if (error != null)
                return error;
            entry.Ignored = ignored;
            SaveAndPublish();
            return $"{entry.Name} is {(entry.Ignored ? "now ignored" : "counted again")}.";
        }

        private static string Forget(string[] args)
        {
            string error = FindPlayer("forget", args, out RosterEntry entry);
            if (error != null)
                return error;
            ProgressServer.EnsureRoster().Data.Players.Remove(entry);
            SaveAndPublish();
            return $"{entry.Name} removed from the roster.";
        }

        // ------------------------------------------------------------------ argument lookup

        /// <summary>Resolves the player argument. Returns the reply text on failure, null when <paramref name="entry"/> is set.</summary>
        private static string FindPlayer(string verb, string[] args, out RosterEntry entry)
        {
            entry = null;
            if (args.Length < 2)
                return $"Usage: lockstep {verb} <player>";
            entry = ProgressServer.EnsureRoster().Find(args[1]);
            return entry == null ? $"No player '{args[1]}' in the roster." : null;
        }

        /// <summary>Resolves the player and stage arguments. Returns the reply text on failure, null when both are set.</summary>
        private static string FindPlayerAndStage(string verb, string[] args, out RosterEntry entry, out Stage stage)
        {
            entry = null;
            stage = null;
            if (args.Length < 3)
                return $"Usage: lockstep {verb} <player> <stage>";
            entry = ProgressServer.EnsureRoster().Find(args[1]);
            stage = Chain.Find(args[2]);
            if (entry == null)
                return $"No player '{args[1]}' in the roster.";
            return stage == null ? $"No stage '{args[2]}' in the chain." : null;
        }

        private static void SaveAndPublish()
        {
            ProgressServer.EnsureRoster().Save();
            ProgressServer.Publish();
        }

        // ------------------------------------------------------------------ status

        private static string Status()
        {
            Roster r = ProgressServer.EnsureRoster();
            HashSet<long> online = new HashSet<long>(ProgressServer.Online().Select(p => p.Id));
            StringBuilder text = new StringBuilder("Lockstep status\n");
            foreach (Stage stage in Chain.Stages)
            {
                List<string> waiting = ProgressServer.WaitingFor(stage);
                text.Append("  ").Append(stage.Name).Append(": ")
                    .Append(waiting.Count == 0 ? "open" : $"waiting for {string.Join(", ", waiting)}").Append('\n');
            }
            text.Append("Players\n");
            foreach (RosterEntry entry in r.Data.Players)
                AppendPlayer(text, entry, online);
            return text.ToString().TrimEnd('\n');
        }

        private static void AppendPlayer(StringBuilder text, RosterEntry entry, HashSet<long> online)
        {
            string state = entry.Ignored ? "ignored" : online.Contains(entry.Id) ? "online" : ProgressServer.Counts(entry, online) ? "counted" : "inactive";
            string cleared = string.Join(", ", Chain.Stages.Where(s => entry.Cleared.Contains(s.Key)).Select(s => s.Name));
            text.Append("  ").Append(entry.Name).Append(" (").Append(entry.Id).Append(") ").Append(state)
                .Append(", last seen ").Append(entry.LastSeen)
                .Append(", cleared: ").Append(cleared.Length > 0 ? cleared : "none").Append('\n');
        }
    }
}
