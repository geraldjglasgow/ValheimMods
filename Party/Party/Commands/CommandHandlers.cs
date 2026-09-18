using System.Linq;
using Party.Chat;
using Party.Server;

namespace Party.Commands
{
    /// <summary>The five verbs, shared by the <c>/party</c> umbrella command and the short aliases.</summary>
    public static class CommandHandlers
    {
        public static bool NotConnected()
        {
            if (ZNet.instance != null && ZRoutedRpc.instance != null)
                return false;
            PartyCommandOutput.Print("Party: not connected to a world.");
            return true;
        }

        public static void Invite(string[] rest)
        {
            if (NotConnected())
                return;
            if (rest.Length < 1)
            {
                PartyCommandOutput.Print("Usage: party invite <name>");
                return;
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcInvite, rest[0]);
        }

        public static void Leave()
        {
            if (NotConnected())
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcLeave);
        }

        public static void Remove(string[] rest)
        {
            if (NotConnected())
                return;
            if (rest.Length < 1)
            {
                PartyCommandOutput.Print("Usage: party remove <name>");
                return;
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcRemove, rest[0]);
        }

        public static void Promote(string[] rest)
        {
            if (NotConnected())
                return;
            if (rest.Length < 1)
            {
                PartyCommandOutput.Print("Usage: party promote <name>");
                return;
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcPromote, rest[0]);
        }

        public static void P(string text)
        {
            if (NotConnected())
                return;
            if (string.IsNullOrEmpty(text))
            {
                PartyChatState.ToggleMode();
                return;
            }
            PartyChatState.Send(text);
        }

        public static void Panel(string[] rest)
        {
            if (rest.Length < 1)
            {
                PartyCommandOutput.Print("Usage: party panel edit|done");
                return;
            }
            bool edit = rest[0].Equals("edit", System.StringComparison.OrdinalIgnoreCase);
            UI.HealthPanel.ToggleEditMode(edit);
            PartyCommandOutput.Print(edit ? "Party panel: drag it, then 'party panel done'." : "Party panel: back to normal play.");
        }

        public static void Name(string text)
        {
            if (NotConnected())
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcRename, text ?? "");
        }

        public static void Status()
        {
            if (NotConnected())
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(PartyRpcServer.RpcStatus);
        }

        /// <summary>Tab-complete source for <c>/invite</c>: everyone currently online.</summary>
        public static System.Collections.Generic.List<string> OnlinePlayerNames() =>
            ZNet.instance != null ? ZNet.instance.GetPlayerList().Select(p => p.m_name).Distinct().ToList() : new System.Collections.Generic.List<string>();

        /// <summary>Tab-complete source for <c>/remove</c> and <c>/promote</c>: this player's own party.</summary>
        public static System.Collections.Generic.List<string> PartyMemberNames() =>
            Client.PartyClientState.Members.Select(m => m.Name).ToList();
    }
}
