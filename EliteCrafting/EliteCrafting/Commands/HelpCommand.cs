namespace EliteCrafting.Commands
{
    /// <summary><c>ecraft [help]</c>: one line per sub-command this player may run (console-commands.md section 1).</summary>
    internal static class HelpCommand
    {
        public static void Run(CommandCall call)
        {
            call.Reply("sub-commands you may run here (ids are case-insensitive):");
            foreach (SubCommand command in EcraftCommand.SubCommands)
            {
                if (CommandAccess.Allows(command.Access))
                {
                    call.Detail($"{command.Grammar} - {command.Summary}");
                }
            }
            if (!CommandAccess.IsAdmin)
            {
                call.Detail("give, roll, reroll, affix, reload, dump and tiers need admin rights on this server.");
            }
        }
    }
}
