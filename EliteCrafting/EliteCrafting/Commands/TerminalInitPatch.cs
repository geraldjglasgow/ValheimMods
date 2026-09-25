using HarmonyLib;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// Registers <c>ecraft</c> when the game builds its console command table (game notes Q21). Runs on every peer that
    /// has a terminal: clients, the host and a dedicated server's console. <c>InitTerminal</c> is guarded by the game and
    /// our registration is idempotent, so the console and chat terminals coming up never register twice.
    /// </summary>
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    internal static class TerminalInitPatch
    {
        [HarmonyPostfix]
        private static void Postfix() => EcraftCommand.Register();
    }
}
