using PatchGuard;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// Registers the single <c>elite</c> console command and routes its subcommands. Nothing in the mod can be
    /// tested without these - mutations are rare by design, so every rule needs a way to be produced on demand. All
    /// of them but <c>tier</c> are admin-gated: on a server they run only for the host or an admin, and are a no-op with
    /// a message for anyone without rights. <c>tier</c> only reads the world's keys, so everyone may use it.
    /// Registration is idempotent and driven from Terminal init, so the console and chat terminals both coming up does
    /// not double-register.
    /// </summary>
    public static class EliteCommands
    {
        public const string Root = "elite";
        private static bool _registered;

        public static void Register()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            new Terminal.ConsoleCommand(Root,
                "elite spawn <prefab> <stars> [mutation... | aspect] | elite inspect | elite purge | elite effects <text> | elite reference | elite tier",
                (Terminal.ConsoleEvent)OnElite);
        }

        public static void Reply(Terminal.ConsoleEventArgs args, string text) => args?.Context?.AddString(text);

        private static void OnElite(Terminal.ConsoleEventArgs args) =>
            Guard.Run("elite command", () => Dispatch(args));

        private static void Dispatch(Terminal.ConsoleEventArgs args)
        {
            string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
            if (sub != "tier" && !IsAdmin())
            {
                Reply(args, "elite: requires admin rights on this server (elite tier is open to everyone).");
                return;
            }
            switch (sub)
            {
                case "spawn": SpawnCommand.Run(args); break;
                case "inspect": InspectCommand.Run(args); break;
                case "purge": PurgeCommand.Run(args); break;
                case "effects": EffectsCommand.Run(args); break;
                case "reference": ReferenceCommand.Run(args); break;
                case "tier": TierCommand.Run(args); break;
                default: Reply(args, "elite: use spawn, inspect, purge, effects, reference or tier."); break;
            }
        }

        // Host is always admin; a connected player must be on the server's admin list. No world means nothing to do.
        private static bool IsAdmin() => ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
    }
}
