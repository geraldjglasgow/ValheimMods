using System;
using PatchGuard;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// Registers the single <c>elite</c> console command and routes its subcommands. Nothing in the mod can be
    /// tested without these - mutations are rare by design, so every rule needs a way to be produced on demand. All
    /// of them but <c>tier</c> and <c>inspect</c> are admin-gated: on a server they run only for the host or a player
    /// the server confirms as admin (<see cref="CommandAccess"/>), and anyone else gets a message naming the ID the
    /// server knows them by. <c>tier</c> and <c>inspect</c> only read what every machine already has, so everyone may
    /// use them.
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
                "elite spawn <prefab> <stars> [mutation... | aspect] | elite inspect | elite purge [radius] | elite effects <text> | elite reference | elite tier | elite raid start <coins> [tier] | elite raid stop | elite arena | elite arena stop",
                (Terminal.ConsoleEvent)OnElite);
        }

        public static void Reply(Terminal.ConsoleEventArgs args, string text) => args?.Context?.AddString(text);

        private static void OnElite(Terminal.ConsoleEventArgs args) =>
            Guard.Run("elite command", () => Dispatch(args));

        private static void Dispatch(Terminal.ConsoleEventArgs args)
        {
            string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
            Action<Terminal.ConsoleEventArgs>? run = Find(sub);
            if (run == null)
            {
                Reply(args, "elite: use spawn, inspect, purge, effects, reference, tier, raid or arena.");
            }
            else if (sub == "tier" || sub == "inspect")
            {
                run(args);
            }
            else
            {
                CommandAccess.RunAsAdmin(args, run);
            }
        }

        private static Action<Terminal.ConsoleEventArgs>? Find(string sub)
        {
            switch (sub)
            {
                case "spawn": return SpawnCommand.Run;
                case "inspect": return InspectCommand.Run;
                case "purge": return PurgeCommand.Run;
                case "effects": return EffectsCommand.Run;
                case "reference": return ReferenceCommand.Run;
                case "tier": return TierCommand.Run;
                case "raid": return RaidCommand.Run;
                case "arena": return ArenaCommand.Run;
                default: return null;
            }
        }
    }
}
