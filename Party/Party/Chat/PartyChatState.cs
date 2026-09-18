using Party.Commands;
using Party.Server;

namespace Party.Chat
{
    /// <summary>Party chat bypasses <see cref="Talker"/> entirely; each client formats and colors its own line.</summary>
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

        public static void OnDeliver(string senderName, string text) => PartyAnnounce.Print($"{senderName}: {text}");
    }
}
