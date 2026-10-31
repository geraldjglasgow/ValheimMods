using System;
using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// Players' chat as it passes through the server, for the night vote (<see cref="SleepWord"/>). Every chat message
    /// is a routed RPC that a player's game sends to the server, which handles or forwards it (ZRoutedRpc.RPC_RoutedRPC):
    /// a shout is "ChatMessage" and plain talk (the chat window's default) is the "Say" RPC of the speaker's Talker. Where
    /// the platform checks chat permissions (Steam does) the game sends one copy to each other player
    /// (Chat.CheckPermissionsAndSendChatMessageRPCsAsync), otherwise one to everybody; the copy to the sender itself never
    /// leaves their PC, so a player alone on a dedicated server is never heard here (their bed still counts). The server
    /// unpacks each incoming routed RPC once (RoutedRPCData.Deserialize, called only there); this postfix looks at the
    /// method hash first, so any other RPC costs two compares, and reads a chat's text from a copy of its parameters. The
    /// copies of one message share the sender, so they record one vote. The sender id is the one the sending game wrote,
    /// as the game itself trusts it. A malformed package is ignored: nothing here may throw into the game's networking.
    /// </summary>
    [HarmonyPatch(typeof(ZRoutedRpc.RoutedRPCData), nameof(ZRoutedRpc.RoutedRPCData.Deserialize))]
    internal static class PeerChat
    {
        private static readonly int ChatMessage = "ChatMessage".GetStableHashCode();
        private static readonly int Say = "Say".GetStableHashCode();
        private static bool _warned;

        private static void Postfix(ZRoutedRpc.RoutedRPCData __instance)
        {
            int method = __instance.m_methodHash;
            if ((method != ChatMessage && method != Say) || !Settings.NightSkipVote.Value)
            {
                return;
            }
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }
            try
            {
                SleepWord.Heard(__instance.m_senderPeerID, SleepWord.ReadText(__instance.m_parameters, method == ChatMessage));
            }
            catch (Exception e)
            {
                if (!_warned)
                {
                    _warned = true;
                    HighSeat.Log.LogWarning($"a chat message could not be read for the night vote: {e.Message}");
                }
            }
        }
    }
}
