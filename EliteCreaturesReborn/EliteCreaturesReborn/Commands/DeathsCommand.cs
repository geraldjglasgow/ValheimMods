using EliteCreaturesReborn.Recap;
using EliteCreaturesReborn.Recap.Window;
using PatchGuard;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>deaths</c>, typed as <c>/deaths</c> in chat or <c>deaths</c> in the F5 console: opens the death recap window,
    /// like its key. Open to every player - not a cheat, no devcommands, no admin check - since it shows only this
    /// player's own deaths, recorded on this machine. The game has no <c>deaths</c> command of its own.
    /// </summary>
    public static class DeathsCommand
    {
        public const string Name = "deaths";

        private static bool _registered;

        /// <summary>Idempotent, like <see cref="DamageCommand.Register"/>, and driven from the same Terminal init.</summary>
        public static void Register()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            new Terminal.ConsoleCommand(Name, "opens the death recap window (Elite Creatures Reborn)",
                (Terminal.ConsoleEvent)OnDeaths);
        }

        private static void OnDeaths(Terminal.ConsoleEventArgs args) =>
            Guard.Run("deaths command", () => Run(args.Context));

        private static void Run(Terminal context)
        {
            if (!RecapWindow.Open())
            {
                context?.AddString("deaths: the recap window can only open in a world.");
                return;
            }
            context?.AddString($"deaths: {RecapStore.Deaths.Count} kept.");
        }
    }
}
