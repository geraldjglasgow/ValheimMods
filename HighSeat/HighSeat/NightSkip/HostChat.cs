using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The hosting player's own chat, for the night vote (<see cref="SleepWord"/>). On a hosted game the host is a player
    /// but not a peer: its messages are routed RPCs its own game makes (to itself and to each player), never unpacked
    /// from a peer, so <see cref="PeerChat"/> does not see them. Chat.SendText is where the chat window sends every typed
    /// message, plain talk, shout or whisper (the "say", "s" and "w" commands), and it sends nothing without a local
    /// player, so neither does this. A dedicated server has no chat window.
    /// </summary>
    [HarmonyPatch(typeof(Chat), nameof(Chat.SendText))]
    internal static class HostChat
    {
        private static void Prefix(string text)
        {
            bool hosting = ZNet.instance != null && ZNet.instance.IsServer();
            if (Settings.NightSkipVote.Value && hosting && Player.m_localPlayer != null)
            {
                SleepWord.Heard(ZDOMan.GetSessionID(), text);
            }
        }
    }
}
