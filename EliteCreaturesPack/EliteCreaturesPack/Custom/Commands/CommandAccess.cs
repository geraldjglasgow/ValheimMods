using System;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Commands
{
    /// <summary>
    /// Who may run the <c>ecp</c> subcommands, decided by the server (the approach of Elite Creatures Reborn's own admin
    /// check, written again here so neither mod needs the other). A client's copy of the admin list is sent once at join
    /// and can be empty or stale, so it is never consulted: a connected player's command asks the server, the server
    /// checks the ID it sees on that player's connection against its own adminlist.txt (the check the game's kick and ban
    /// use), and the command runs when the answer comes back. The host is the server and runs its commands at once.
    /// </summary>
    internal static class CommandAccess
    {
        public const string Ask = "ecp_admin_ask";
        public const string Answer = "ecp_admin_answer";
        private const float AnswerWait = 5f;

        private static ZRoutedRpc? registeredOn;
        private static Terminal.ConsoleEventArgs? pending;
        private static Action<Terminal.ConsoleEventArgs>? pendingRun;
        private static float askedAt;

        /// <summary>Registers both handlers once per routed-RPC bus. Called at world start on every machine.</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, registeredOn))
            {
                return;
            }
            registeredOn = bus;
            bus.Register(Ask, OnAsk);
            bus.Register<bool, string>(Answer, OnAnswer);
        }

        /// <summary>Runs <paramref name="run"/> now on the host, or once the server confirms this player is an admin.</summary>
        public static void RunAsAdmin(Terminal.ConsoleEventArgs args, Action<Terminal.ConsoleEventArgs> run)
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                EcpCommands.Reply(args, "ecp: no world is loaded.");
                return;
            }
            if (ZNet.instance.IsServer())
            {
                run(args);
                return;
            }
            WarnIfUnanswered(args);
            EnsureRegistered();
            pending = args;
            pendingRun = run;
            askedAt = Time.realtimeSinceStartup;
            ZRoutedRpc.instance.InvokeRoutedRPC(Ask);
        }

        // A server without the mod never answers, and the command would otherwise vanish without a word.
        private static void WarnIfUnanswered(Terminal.ConsoleEventArgs args)
        {
            if (pending != null && Time.realtimeSinceStartup - askedAt > AnswerWait)
            {
                EcpCommands.Reply(args, "ecp: the server did not answer the last admin check. Elite Creatures Pack must be "
                    + "installed on the server as well.");
            }
        }

        private static void OnAsk(long sender) => SafeCall.Run("ecp admin check", static id => AnswerAsk(id), sender);

        // Server side: the ID on the sender's connection, checked against the server's own list.
        private static void AnswerAsk(long sender)
        {
            ZNet net = ZNet.instance;
            if (net == null || !net.IsServer())
            {
                return;
            }
            string id = net.GetPeer(sender)?.m_socket?.GetHostName() ?? "";
            bool admin = id.Length > 0 && net.IsAdmin(id);
            Log.Info($"ecp admin check for '{id}': {(admin ? "admin" : "not on adminlist.txt")}");
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, Answer, admin, id);
        }

        private static void OnAnswer(long sender, bool admin, string id) =>
            SafeCall.Run("ecp admin answer", static (isAdmin, who) => Resolve(isAdmin, who), admin, id);

        // Client side: run the waiting command, or say exactly which ID the server wants in its list.
        private static void Resolve(bool admin, string id)
        {
            Terminal.ConsoleEventArgs? args = pending;
            Action<Terminal.ConsoleEventArgs>? run = pendingRun;
            pending = null;
            pendingRun = null;
            if (args == null || run == null)
            {
                return;
            }
            if (admin)
            {
                run(args);
                return;
            }
            EcpCommands.Reply(args, Refusal(id));
        }

        private static string Refusal(string id) => id.Length == 0
            ? "ecp: requires admin rights on this server. The server could not identify your connection."
            : $"ecp: requires admin rights on this server. The server knows you as '{id}' and that ID is not in its "
                + "adminlist.txt. Add it on a line of its own; the server reads the file again within 10 seconds, no restart "
                + "or reconnect needed.";
    }
}
