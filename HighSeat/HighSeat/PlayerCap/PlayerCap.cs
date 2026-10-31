using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The player cap is Max players instead of the game's 10. A joining player is turned away with the game's own
    /// "server is full" (error 9) once ZNet.GetNrOfPlayers() reaches the cap, in ZNet.RPC_PeerInfo; that count is every
    /// connected player (the list is refreshed on each join and leave), the host's own included on a hosted game, so the
    /// cap counts the host as the game's 10 does.
    /// The 10 is a compiled-in constant (ZNet.ServerPlayerLimit), so a prefix cannot change it: a transpiler replaces
    /// exactly the 10 loaded right after the GetNrOfPlayers call (and compared by the branch after it) with a call to
    /// <see cref="Limit"/>, read at every join, so a reloaded .cfg applies to the next player. When that pattern is not
    /// there exactly once (a game update, or another mod already changed it) the method is left as it is and a warning
    /// says so: it never patches some other 10. Players already on are never turned out by a lower cap. A crossplay
    /// (PlayFab) server still makes its lobby and network for 10 players: those are left alone (their own limit is
    /// untested), so crossplay stays at 10 whatever the setting. The Steam lobby a hosted game lists: <see cref="LobbySize"/>.
    /// </summary>
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
    internal static class PlayerCap
    {
        /// <summary>The cap, read by the patched check each time a player joins.</summary>
        public static int Limit() => Settings.MaxPlayers.Value;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            try
            {
                int at = FindLimit(code);
                if (at >= 0)
                {
                    CodeInstruction limit = CodeInstruction.Call(typeof(PlayerCap), nameof(Limit));
                    code[at] = limit.MoveLabelsFrom(code[at]).MoveBlocksFrom(code[at]);
                    HighSeat.Log.LogInfo($"player cap: Max players ({Limit()}), not the game's {ZNet.ServerPlayerLimit}");
                    return code;
                }
            }
            catch (Exception e)
            {
                HighSeat.Log.LogWarning($"player cap check not patched: {e.Message}");
                return code;
            }
            HighSeat.Log.LogWarning("the game's player cap check was not found (a game update, or another mod changed it): " +
                "Max players is not applied and the game's own limit stays");
            return code;
        }

        // The index of the one 10 compared with GetNrOfPlayers(); -1 when there is none, or more than one.
        private static int FindLimit(List<CodeInstruction> code)
        {
            MethodInfo count = AccessTools.Method(typeof(ZNet), nameof(ZNet.GetNrOfPlayers));
            int found = -1;
            for (int i = 0; count != null && i + 2 < code.Count; i++)
            {
                if (code[i].Calls(count) && code[i + 1].LoadsConstant(ZNet.ServerPlayerLimit) && code[i + 2].Branches(out _))
                {
                    if (found >= 0)
                    {
                        return -1;
                    }
                    found = i + 1;
                }
            }
            return found;
        }
    }
}
