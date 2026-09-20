using System.Text;
using PatchGuard;
using UnityEngine;

namespace Party.Server
{
    /// <summary>Server RPCs, registered everywhere; every handler no-ops unless <see cref="Identity.IsServer"/>.</summary>
    public static class PartyRpcServer
    {
        public const string RpcCreate = "Party_Create";
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
        public const string RpcDeath = "Party_Death";
        public const string RpcDeathDeliver = "Party_DeathDeliver";
        public const string RpcRename = "Party_Rename";
        public const string RpcStatus = "Party_Status";

        public static void RegisterRpcs()
        {
            ZRoutedRpc rpc = ZRoutedRpc.instance;
            rpc.Register<string>(RpcCreate, (s, name) => Guard.Run("party create", () => Server(() => OnCreate(s, name))));
            rpc.Register<string>(RpcInvite, (s, name) => Guard.Run("party invite", () => Server(() => InviteFlow.OnInvite(s, name))));
            rpc.Register<bool>(RpcInviteRespond, (s, ok) => Guard.Run("party invite respond", () => Server(() => InviteFlow.OnRespond(s, ok))));
            rpc.Register(RpcLeave, s => Guard.Run("party leave", () => Server(() => OnLeave(s))));
            rpc.Register<string>(RpcRemove, (s, name) => Guard.Run("party remove", () => Server(() => OnRemove(s, name))));
            rpc.Register<string>(RpcPromote, (s, name) => Guard.Run("party promote", () => Server(() => OnPromote(s, name))));
            rpc.Register<string>(RpcChat, (s, text) => Guard.Run("party chat", () => Server(() => OnChat(s, text))));
            rpc.Register<Vector3>(RpcPing, (s, pos) => Guard.Run("party ping", () => Server(() => OnPing(s, pos))));
            rpc.Register<ZPackage>(RpcVitalsReport,
                (s, pkg) => Guard.Run("party vitals", () => Server(() => OnVitals(s, pkg))));
            rpc.Register<Vector3>(RpcDeath, (s, pos) => Guard.Run("party death", () => Server(() => OnDeath(s, pos))));
            rpc.Register<string>(RpcRename, (s, name) => Guard.Run("party rename", () => Server(() => OnRename(s, name))));
            rpc.Register(RpcStatus, s => Guard.Run("party status", () => Server(() => OnStatus(s))));
        }

        private static void Server(System.Action action)
        {
            if (Identity.IsServer)
                action();
        }

        private static void OnCreate(long senderPeerId, string name)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer player))
                return;
            if (PartyManager.FindPartyOf(player.Id) != null)
            {
                Reply(player.Id, "You already have a party.");
                return;
            }
            PartyRecord party = PartyManager.CreateParty(player.Id, player.Name);
            PartyManager.Rename(party, name);
            PartyManager.SaveAndPublish(party);
            Reply(player.Id, party.Name.Length > 0 ? $"Party '{party.Name}' created." : "Party created.");
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
            RelayToOthers(sender, RpcPingDeliver, sender.Name, pos);
        }

        private static void OnVitals(long senderPeerId, ZPackage pkg)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer sender))
                return;
            RelayToOthers(sender, RpcVitalsDeliver, sender.Id, pkg);
        }

        private static void OnDeath(long senderPeerId, Vector3 pos)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer sender))
                return;
            RelayToOthers(sender, RpcDeathDeliver, sender.Name, pos);
        }

        /// <summary>Sends an RPC to every other online member of the sender's party. No-ops if the sender has none.</summary>
        private static void RelayToOthers(OnlinePlayer sender, string rpcName, params object[] args)
        {
            PartyRecord party = PartyManager.FindPartyOf(sender.Id);
            if (party == null)
                return;
            foreach (PartyMember member in party.Members)
            {
                if (member.Id == sender.Id)
                    continue;
                long peerId = Identity.PeerIdFor(member.Id);
                if (peerId != 0)
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, rpcName, args);
            }
        }

        private static void OnRename(long senderPeerId, string name)
        {
            if (!TryLeaderAction(senderPeerId, out OnlinePlayer leader, out PartyRecord party, out string error))
            {
                Reply(leader.Id, error);
                return;
            }
            PartyManager.Rename(party, name);
            PartyManager.SaveAndPublish(party);
            Reply(leader.Id, party.Name.Length > 0 ? $"Party renamed to '{party.Name}'." : "Party name cleared.");
        }

        private static void OnStatus(long senderPeerId)
        {
            if (!IsAdmin(senderPeerId))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(senderPeerId, RpcReply, "Party: only admins can see server-wide status.");
                return;
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(senderPeerId, RpcReply, BuildStatus());
        }

        private static bool IsAdmin(long senderPeerId)
        {
            if (senderPeerId == ZNet.GetUID())
                return true;
            ZNetPeer peer = ZNet.instance.GetPeer(senderPeerId);
            return peer != null && ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
        }

        private static string BuildStatus()
        {
            System.Collections.Generic.List<PartyRecord> parties = PartyManager.EnsureStore().Data.Parties;
            if (parties.Count == 0)
                return "Party status: no parties.";
            StringBuilder text = new StringBuilder("Party status\n");
            foreach (PartyRecord party in parties)
                AppendParty(text, party);
            return text.ToString().TrimEnd('\n');
        }

        private static void AppendParty(StringBuilder text, PartyRecord party)
        {
            string label = party.Name.Length > 0 ? party.Name : party.PartyId.Substring(0, 6);
            string leaderName = party.Find(party.LeaderId)?.Name ?? "?";
            text.Append("  ").Append(label).Append(" - leader: ").Append(leaderName).Append('\n');
            foreach (PartyMember member in party.Members)
            {
                bool online = Identity.TryFind(member.Id, out _);
                text.Append("    ").Append(member.Name).Append(online ? " (online)" : " (offline)").Append('\n');
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
