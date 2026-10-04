using EliteCreaturesReborn.Commands;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Registers the mod's console commands once the terminal system initialises. <c>Terminal.InitTerminal</c> runs as
    /// the game builds its command table (and again for the chat terminal), so this is the moment custom commands must
    /// be added; <see cref="EliteCommands.Register"/> and <see cref="DamageCommand.Register"/> are idempotent, so a
    /// second call is a no-op. The table is shared: the chat runs a line starting with "/" as the command it names, so
    /// both work in the F5 console and in chat.
    /// </summary>
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    public static class TerminalCommandPatch
    {
        private static void Postfix() => Guard.Run("Terminal.InitTerminal register", Register);

        private static void Register()
        {
            EliteCommands.Register();
            DamageCommand.Register();
            DeathsCommand.Register();
        }
    }
}
