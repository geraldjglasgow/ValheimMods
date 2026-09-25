using System;
using System.Collections.Generic;
using PatchGuard;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// The one console command, <c>ecraft</c>, and its sub-command table (console-commands.md section 1). Registered
    /// without the game's <c>isCheat</c> flag (it would mark the player's profile as cheated) and without
    /// <c>onlyAdmin</c> / <c>onlyServer</c> (the game turns <c>onlyAdmin</c> into server-only); access is checked by
    /// <see cref="CommandAccess"/> on the machine that runs the command. Every sub-command runs on the caller's own
    /// machine (client, host, or a dedicated server's console) and changes at most the caller's inventory or files.
    /// Tab completion offers the sub-command names: the game completes only the word after the command.
    /// </summary>
    internal static class EcraftCommand
    {
        public const string Name = "ecraft";
        private static bool _registered;

        private static readonly List<SubCommand> Table = new List<SubCommand>
        {
            new SubCommand("help", "ecraft [help]", Access.ReadOnly, "lists the sub-commands you may run", HelpCommand.Run),
            new SubCommand("inspect", InspectCommand.Grammar, Access.ReadOnly, "an item's ecf_ data and its parse", InspectCommand.Run),
            new SubCommand("stats", "ecraft stats", Access.ReadOnly, "your aggregated affix totals", StatsCommand.Run),
            new SubCommand("list", ListCommand.Grammar, Access.ReadOnly, "the running configuration", ListCommand.Run),
            new SubCommand("give", GiveCommand.Grammar, Access.Admin, "stones, essences and shards into your inventory", GiveCommand.Run),
            new SubCommand("roll", RollCommand.Grammar, Access.Admin, "a rolled magic item into your inventory", RollCommand.Run),
            new SubCommand("reroll", RerollCommand.Grammar, Access.Admin, "rerolls every affix of an item, keeping rarity", RerollCommand.Run),
            new SubCommand("affix", AffixCommand.Grammar, Access.Admin, "adds or replaces one affix on an item", AffixCommand.Run),
            new SubCommand("reload", "ecraft reload", Access.Author, "re-reads the YAML, translations and .cfg now", ReloadCommand.Run),
            new SubCommand("dump", DumpCommand.Grammar, Access.Admin, "writes the merged configuration or the item survey", DumpCommand.Run),
            new SubCommand("tiers", "ecraft tiers", Access.Admin, "writes the item tier reference file", TiersCommand.Run),
            new SubCommand("ecr", EcrCommand.Grammar, Access.ReadOnly, "the Elite Creatures Reborn synergy and the hovered creature", EcrCommand.Run),
        };

        public static IReadOnlyList<SubCommand> SubCommands => Table;

        public static void Register()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            new Terminal.ConsoleCommand(Name, "[help|inspect|stats|list|give|roll|reroll|affix|reload|dump|tiers|ecr] - EliteCrafting",
                (Terminal.ConsoleEvent)OnCommand, optionsFetcher: Names);
        }

        private static List<string> Names() => Table.ConvertAll(c => c.Name);

        private static void OnCommand(Terminal.ConsoleEventArgs args)
        {
            CommandCall call = new CommandCall(args);
            SubCommand? command = Table.Find(c => c.Name == call.Sub);
            if (command == null)
            {
                call.ReplyPlain($"{Name}: unknown sub-command '{call.Sub}'. '{Name} help' lists them.");
                return;
            }
            if (CommandAccess.Check(command, call))
            {
                Execute(command, call);
            }
        }

        // PatchGuard logs the exception under the mod's name and rethrows; the console gets one line.
        private static void Execute(SubCommand command, CommandCall call)
        {
            try
            {
                Guard.Run(Name + " " + command.Name, () => command.Run(call));
            }
            catch (Exception)
            {
                call.Reply("failed, see the log.");
            }
        }
    }

    /// <summary>Who may run a sub-command (console-commands.md section 2).</summary>
    internal enum Access
    {
        Everyone,
        /// <summary>Everyone while <c>Read-only commands for everyone</c> is on, else admins.</summary>
        ReadOnly,
        Admin,
        /// <summary>Admins, and only where this machine's files are in force.</summary>
        Author,
    }

    /// <summary>One row of the sub-command table.</summary>
    internal sealed class SubCommand
    {
        public SubCommand(string name, string grammar, Access access, string summary, Action<CommandCall> run)
        {
            Name = name;
            Grammar = grammar;
            Access = access;
            Summary = summary;
            Run = run;
        }

        public string Name { get; }
        public string Grammar { get; }
        public Access Access { get; }
        public string Summary { get; }
        public Action<CommandCall> Run { get; }
    }
}
