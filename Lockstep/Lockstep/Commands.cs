using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;

namespace Lockstep
{
    /// <summary>
    /// The <c>lockstep</c> console command. Every verb is sent to the server, which answers through a reply RPC,
    /// so the same code path serves the host, a dedicated server and remote admins.
    /// </summary>
    public static class Commands
    {
        private const string Usage = "lockstep status | grant <player> <stage> | revoke <player> <stage> | ignore <player> | unignore <player> | forget <player>";

        public static void Register()
        {
            new Terminal.ConsoleCommand("lockstep", Usage, args => Guard.Run("lockstep command", () => Run(args)),
                isCheat: false, isNetwork: true, onlyServer: false, isSecret: false, allowInDevBuild: false, hideBehindDevCommands: false,
                optionsFetcher: () => new List<string> { "status", "grant", "revoke", "ignore", "unignore", "forget" });
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            if (ZNet.instance == null || ZRoutedRpc.instance == null)
            {
                Print("Lockstep: not connected to a world.");
                return;
            }
            string line = args.Args.Length > 1 ? string.Join(" ", args.Args, 1, args.Args.Length - 1) : "status";
            ZRoutedRpc.instance.InvokeRoutedRPC(ProgressServer.RpcCommand, line);
        }

        /// <summary>Prints a possibly multi-line reply into the console.</summary>
        public static void Print(string text)
        {
            if (Console.instance == null)
            {
                Lockstep.Log.LogInfo(text);
                return;
            }
            foreach (string line in text.Split('\n'))
                Console.instance.AddString(line);
        }
    }

    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    public static class TerminalInitPatch
    {
        private static bool registered;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (registered)
                return;
            registered = true;
            Commands.Register();
        }
    }
}
