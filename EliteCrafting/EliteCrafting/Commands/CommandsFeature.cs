namespace EliteCrafting.Commands
{
    /// <summary>
    /// Entry point of the Commands area, called once from plugin Awake after the rules, settings and words are loaded.
    /// Owns: the <c>ecraft</c> console command (console-commands.md). The command is registered from a postfix on
    /// <c>Terminal.InitTerminal</c> (<see cref="TerminalInitPatch"/>); if the terminal already initialised before this
    /// plugin loaded, it is registered here instead. Registration is idempotent.
    /// </summary>
    public static class CommandsFeature
    {
        public static void Init()
        {
            if (Terminal.m_terminalInitialized)
            {
                EcraftCommand.Register();
            }
        }
    }
}
