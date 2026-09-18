using PatchGuard;
using UnityEngine;

namespace Party.Server
{
    /// <summary>
    /// Server side RPC registrations. Registered on every instance (client, host, dedicated server); every handler
    /// no-ops unless <see cref="Identity.IsServer"/>, the same shape as Lockstep's <c>ProgressServer</c>.
    /// </summary>
    public static class PartyRpcServer
    {
        public const string RpcInvite = "Party_Invite";
        public const string RpcInvitePrompt = "Party_InvitePrompt";
        public const string RpcInviteRespond = "Party_InviteRespond";
        public const string RpcLeave = "Party_Leave";
        public const string RpcRemove = "Party_Remove";
        public const string RpcPromote = "Party_Promote";
        public const string RpcReply = "Party_Reply";
        public const string RpcChat = "Party_Chat";
        public const string RpcChatDeliver = "Party_ChatDeliver";
        public const string RpcPing = "Party_Ping";
        public const string RpcPingDeliver = "Party_PingDeliver";
        public const string RpcVitalsReport = "Party_VitalsReport";
        public const string RpcVitalsDeliver = "Party_VitalsDeliver";

        public static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            rpc.Register<string>(RpcInvite, (s, name) => Guard.Run("party invite", () => Server(() => InviteFlow.OnInvite(s, name))));
            rpc.Register<bool>(RpcInviteRespond, (s, ok) => Guard.Run("party invite respond", () => Server(() => InviteFlow.OnRespond(s, ok))));
            rpc.Register(RpcLeave, s => Guard.Run("party leave", () => Server(() => OnLeave(s))));
            rpc.Register<string>(RpcRemove, (s, name) => Guard.Run("party remove", () => Server(() => OnRemove(s, name))));
            rpc.Register<string>(RpcPromote, (s, name) => Guard.Run("party promote", () => Server(() => OnPromote(s, name))));
            rpc.Register<string>(RpcChat, (s, text) => Guard.Run("party chat", () => Server(() => OnChat(s, text))));
            rpc.Register<Vector3>(RpcPing, (s, pos) => Guard.Run("party ping", () => Server(() => OnPing(s, pos))));
            rpc.Register<float, float, float, Vector3, bool>(RpcVitalsReport,
                (s, hp, st, ei, pos, valid) => Guard.Run("party vitals", () => Server(() => OnVitals(s, hp, st, ei, pos, valid))));
        }

        private static void Server(System.Action action)
        {
            if (Identity.IsServer)
                action();
        }

        private static void OnLeave(long senderPeerId)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer player))
                return;
            PartyRecord party = PartyManager.FindPartyOf(player.Id);
            if (party == null)
            {
                Reply(player.Id, "You are not in a party.");
                return;
            }
            PartyManager.RemoveMember(party, player.Id);
            PartyManager.EnsureStore().Save();
            if (party.Members.Count > 0)
                PartyPublisher.Publish(party);
            PartyPublisher.PublishEmpty(player.Id);
            Reply(player.Id, "You left the party.");
        }

        private static void OnRemove(long senderPeerId, string targetName)
        {
            if (!TryLeaderAction(senderPeerId, out OnlinePlayer leader, out PartyRecord party, out string error))
            {
                Reply(leader.Id, error);
                return;
            }
            PartyMember target = FindMemberByName(party, targetName);
            if (target == null)
            {
                Reply(leader.Id, $"No party member named '{targetName}'.");
                return;
            }
            PartyManager.RemoveMember(party, target.Id);
            PartyManager.SaveAndPublish(party);
            PartyPublisher.PublishEmpty(target.Id);
            Reply(leader.Id, $"Removed {target.Name} from the party.");
        }

        private static void OnPromote(long senderPeerId, string targetName)
        {
            if (!TryLeaderAction(senderPeerId, out OnlinePlayer leader, out PartyRecord party, out string error))
            {
                Reply(leader.Id, error);
                return;
            }
            PartyMember target = FindMemberByName(party, targetName);
            if (target == null)
            {
                Reply(leader.Id, $"No party member named '{targetName}'.");
                return;
            }
            PartyManager.PromoteLeader(party, target.Id);
            PartyManager.SaveAndPublish(party);
            Reply(leader.Id, $"{target.Name} is now the party leader.");
        }

        private static void OnChat(long senderPeerId, string text)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer sender))
                return;
            PartyRecord party = PartyManager.FindPartyOf(sender.Id);
            if (party == null)
            {
                Reply(sender.Id, "You are not in a party.");
                return;
            }
            foreach (PartyMember member in party.Members)
            {
                long peerId = Identity.PeerIdFor(member.Id);
                if (peerId != 0)
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, RpcChatDeliver, sender.Name, text);
            }
        }

        private static void OnPing(long senderPeerId, Vector3 pos)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer sender))
                return;
            PartyRecord party = PartyManager.FindPartyOf(sender.Id);
            if (party == null)
                return;
            foreach (PartyMember member in party.Members)
            {
                if (member.Id == sender.Id)
                    continue;
                long peerId = Identity.PeerIdFor(member.Id);
                if (peerId != 0)
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, RpcPingDeliver, sender.Name, pos);
            }
        }

        private static void OnVitals(long senderPeerId, float health, float stamina, float eitr, Vector3 pos, bool posValid)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer sender))
                return;
            PartyRecord party = PartyManager.FindPartyOf(sender.Id);
            if (party == null)
                return;
            foreach (PartyMember member in party.Members)
            {
                if (member.Id == sender.Id)
                    continue;
                long peerId = Identity.PeerIdFor(member.Id);
                if (peerId != 0)
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, RpcVitalsDeliver, sender.Id, health, stamina, eitr, pos, posValid);
            }
        }

        private static bool TryLeaderAction(long senderPeerId, out OnlinePlayer leader, out PartyRecord party, out string error)
        {
            party = null;
            error = null;
            Identity.TryFindByPeerId(senderPeerId, out leader);
            party = PartyManager.FindPartyOf(leader.Id);
            if (party == null)
            {
                error = "You are not in a party.";
                return false;
            }
            if (party.LeaderId != leader.Id)
            {
                error = "Only the party leader can do that.";
                return false;
            }
            return true;
        }

        private static PartyMember FindMemberByName(PartyRecord party, string name) =>
            party.Members.Find(m => string.Equals(m.Name, name, System.StringComparison.OrdinalIgnoreCase));

        public static void Reply(long playerId, string text)
        {
            long peerId = Identity.PeerIdFor(playerId);
            if (peerId != 0)
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, RpcReply, text);
        }
    }
}
