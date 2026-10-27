using System;
using HarmonyLib;

namespace DevBridge.World
{
    /// <summary>
    /// A cheat command typed (or sent) while dev commands are off turns them on first, then runs (2026-10-24, the user:
    /// "'goto' is not valid in the current context ... have it automatically do devcommands"). Only where the game would
    /// allow them at all: single player or the host (Terminal.IsCheatsEnabled wants the server); on a client the command
    /// is left to the game, which refuses it as before. DevBridge is a test tool only, so nobody plays with this.
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.TryRunCommand))]
    internal static class AutoDevCommands
    {
        private static void Prefix(Terminal __instance, string text)
        {
            try
            {
                Enable(__instance, text);
            }
            catch (Exception error)
            {
                Events.Publish.WarnOnce("autodevcommands", error);
            }
        }

        private static void Enable(Terminal terminal, string text)
        {
            string word = (text ?? "").Split(' ')[0].ToLower();
            bool host = ZNet.instance != null && ZNet.instance.IsServer();
            if (!host || Terminal.m_cheat || !Terminal.commands.TryGetValue(word, out Terminal.ConsoleCommand command) || !command.IsCheat)
            {
                return;
            }
            terminal.TryRunCommand("devcommands", silentFail: true);
        }
    }
}
