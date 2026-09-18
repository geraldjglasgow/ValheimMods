using Party.Commands;
using Party.Server;

namespace Party.Chat
{
    /// <summary>
    /// Party chat never goes through <see cref="Talker"/> (no new <c>Talker.Type</c> can be added to a compiled
    /// enum, per PLAN.md): the raw text goes to the server over <c>Party_Chat</c>, and every online member's own
    /// client formats and displays its own colored line.
    /// </summary>
    public static class PartyChatState
    {
        public static bool ToggleModeOn { get; private set; }

        public static void ToggleMode()
        {
            ToggleModeOn = !ToggleModeOn;
            PartyCommandOutput.Print(ToggleModeOn ? "Party chat mode on. Everything you type goes to your party." : "Party chat mode off.");
        }

        public static void Send(string text)
        {
            if (ZRoutedRpc.instance == null || string.IsNullOrEmpty(text))
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcChat, text);
        }

        /// <summary>A party message arrived for the local player. Formatted in this player's own color preference.</summary>
        public static void OnDeliver(string senderName, string text)
        {
            if (global::Chat.instance == null)
                return;
            string color = PartyConfig.PartyColor.Value;
            global::Chat.instance.AddString($"<color={color}>[Party] {senderName}: {text}</color>");
        }
    }
}
