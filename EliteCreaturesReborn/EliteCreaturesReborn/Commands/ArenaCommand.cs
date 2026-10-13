using EliteCreaturesReborn.Arena;
using EliteCreaturesReborn.Arena.Trial;
using EliteCreaturesReborn.Arena.Travel;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite arena</c> and <c>elite arena stop</c> (features/arena.md sections 2 and 3), admin only like the other
    /// elite subcommands (in single player the host runs them at once, as <c>elite raid</c>). Bare, it jumps the admin into
    /// Hel's hall, or back out of it (<see cref="ArenaTravel"/>), on their own machine. Stop ends the running trial
    /// exactly as an empty hall does - the arena creatures vanish, every player of the trial reads where it ended - by
    /// asking the core's owner, wherever that is (<see cref="ArenaNet.AskStop"/>).
    /// </summary>
    public static class ArenaCommand
    {
        public static void Run(Terminal.ConsoleEventArgs args)
        {
            string verb = args.Length > 2 ? args[2].ToLowerInvariant() : "";
            if (verb == "stop")
            {
                Stop(args);
            }
            else if (verb.Length > 0)
            {
                EliteCommands.Reply(args, "usage: elite arena | elite arena stop");
            }
            else if (Player.m_localPlayer == null)
            {
                EliteCommands.Reply(args, "elite arena: needs your player in the world.");
            }
            else
            {
                EliteCommands.Reply(args, ArenaTravel.Go(Player.m_localPlayer));
            }
        }

        private static void Stop(Terminal.ConsoleEventArgs args)
        {
            if (ArenaCore.Current != null && ArenaCore.Current.Zdo != null && !ArenaTrial.Running)
            {
                EliteCommands.Reply(args, "elite arena: no trial is running.");
                return;
            }
            ArenaNet.AskStop();
            EliteCommands.Reply(args, "elite arena: the trial is told to end; its creatures vanish, dropping nothing.");
        }
    }
}
