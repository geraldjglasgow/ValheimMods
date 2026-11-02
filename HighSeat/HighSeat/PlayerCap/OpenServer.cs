using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// A dedicated server nobody needs a password for. With "Open server" on, the password the server was started with
    /// is dropped as the game takes it (ZNet.SetServer, which hashes it into the password every joining player is asked
    /// for, and from which the server list's lock is set), so a panel that always passes -password still gives an open
    /// server. The game also quits a public server started without a password ("Error bad password":
    /// FejdStartup.IsPublicPasswordValid wants at least 5 characters, not part of the world's name or seed); an empty
    /// password passes here instead. A password given is checked as the game checks it. Only on a dedicated server: a
    /// hosting player's own start screen keeps the game's rule.
    /// </summary>
    [HarmonyPatch]
    internal static class OpenServer
    {
        private static bool Prepare() => Application.isBatchMode;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.SetServer))]
        [HarmonyPrefix]
        private static void DropPassword(bool server, ref string password)
        {
            if (server && Settings.OpenServer.Value && !string.IsNullOrEmpty(password))
            {
                password = "";
                HighSeat.Log.LogInfo("Open server: the server's password is not asked for");
            }
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.IsPublicPasswordValid))]
        [HarmonyPrefix]
        private static bool EmptyPassword(string password, ref bool __result)
        {
            if (!string.IsNullOrEmpty(password))
            {
                return true;
            }
            __result = true;
            HighSeat.Log.LogInfo("public server without a password: anyone can join");
            return false;
        }
    }
}
