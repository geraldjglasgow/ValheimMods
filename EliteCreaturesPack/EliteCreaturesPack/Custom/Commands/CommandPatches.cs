using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Custom.Commands
{
    /// <summary>
    /// The <c>ecp</c> command is added when the game builds its command table (<c>Terminal.InitTerminal</c>, run again for
    /// the chat terminal; registering is idempotent), so it works in the F5 console and in chat.
    /// </summary>
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    internal static class EcpCommandPatch
    {
        private static void Postfix() => SafeCall.Run("ecp command register", EcpCommands.Register);
    }

    /// <summary>
    /// World start, on every machine: the admin-check messages are registered before anyone can type a command. The
    /// server answers the question and the client receives the answer, so both ends need the handlers.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    internal static class EcpAdminCheckPatch
    {
        private static void Postfix() => SafeCall.Run("ecp admin check register", CommandAccess.EnsureRegistered);
    }
}
