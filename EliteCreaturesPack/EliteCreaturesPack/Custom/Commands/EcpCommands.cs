using System;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Export;

namespace EliteCreaturesPack.Custom.Commands
{
    /// <summary>
    /// The mod's one console command, <c>ecp</c>, and its subcommands: <c>ecp export &lt;prefab&gt;</c> and <c>ecp export
    /// human</c> for now. Every subcommand is admin-gated: on a server it runs only for the host or a player the server
    /// confirms as an admin (<see cref="CommandAccess"/>). Registered when the game builds its command table, for the F5
    /// console and chat alike; registering twice does nothing.
    /// </summary>
    public static class EcpCommands
    {
        public const string Root = "ecp";
        private static bool registered;

        public static void Register()
        {
            if (registered)
            {
                return;
            }
            registered = true;
            new Terminal.ConsoleCommand(Root, ExportCommand.Usage, (Terminal.ConsoleEvent)OnCommand);
        }

        public static void Reply(Terminal.ConsoleEventArgs args, string text) => args?.Context?.AddString(text);

        private static void OnCommand(Terminal.ConsoleEventArgs args) =>
            SafeCall.Run("ecp command", static command => Dispatch(command), args);

        private static void Dispatch(Terminal.ConsoleEventArgs args)
        {
            string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
            Action<Terminal.ConsoleEventArgs>? run = Find(sub);
            if (run == null)
            {
                Reply(args, "ecp: use " + ExportCommand.Usage + ".");
                return;
            }
            CommandAccess.RunAsAdmin(args, run);
        }

        private static Action<Terminal.ConsoleEventArgs>? Find(string sub)
        {
            switch (sub)
            {
                case "export": return ExportCommand.Run;
                default: return null;
            }
        }
    }
}
