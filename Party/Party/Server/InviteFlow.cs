using System;

namespace Party.Server
{
    /// <summary>The <c>/invite</c> lifecycle: validation, the pending invite, and its three outcomes.</summary>
    public static class InviteFlow
    {
        public static void OnInvite(long senderPeerId, string targetName)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer inviter))
                return;
            if (!Identity.TryFindByName(targetName, out OnlinePlayer target))
            {
                Reply(inviter.Id, $"No player named '{targetName}' online.");
                return;
            }
            string error = Validate(inviter, target);
            if (error != null)
            {
                Reply(inviter.Id, error);
                return;
            }
            Start(inviter, target);
        }

        /// <summary>Everything that can be checked before the invite exists. Creates the inviter's own party of one
        /// if they had none, per spec ("this creates one and makes you the leader").</summary>
        private static string Validate(OnlinePlayer inviter, OnlinePlayer target)
        {
            if (target.Id == inviter.Id)
                return "You can't invite yourself.";
            if (InviteManager.HasPending(target.Id))
                return $"{target.Name} already has a pending invite.";
            PartyRecord inviterParty = PartyManager.FindPartyOf(inviter.Id);
            if (inviterParty == null)
            {
                inviterParty = PartyManager.CreateParty(inviter.Id, inviter.Name);
                PartyManager.SaveAndPublish(inviterParty);
                return null;
            }
            // Any member may invite - only /remove and /promote are spec'd "leader only".
            if (PartyManager.IsFull(inviterParty))
                return "Your party is full.";
            return null;
        }

        private static void Start(OnlinePlayer inviter, OnlinePlayer target)
        {
            int timeout = PartyConfig.InviteTimeoutSeconds.Value;
            InviteManager.Add(new PendingInvite
            {
                InviterId = inviter.Id,
                InviterName = inviter.Name,
                TargetId = target.Id,
                TargetName = target.Name,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(timeout),
            });
            ZRoutedRpc.instance.InvokeRoutedRPC(target.PeerId, PartyRpcServer.RpcInvitePrompt, inviter.Name, timeout);
            Reply(inviter.Id, $"Invite sent to {target.Name}.");
        }

        public static void OnRespond(long senderPeerId, bool accept)
        {
            if (!Identity.TryFindByPeerId(senderPeerId, out OnlinePlayer target))
                return;
            if (!InviteManager.TryTake(target.Id, out PendingInvite invite))
                return;
            if (accept)
                Accept(invite, target);
            else
                Reply(invite.InviterId, $"{target.Name} declined your invite.");
        }

        private static void Accept(PendingInvite invite, OnlinePlayer target)
        {
            PartyRecord inviterParty = PartyManager.FindPartyOf(invite.InviterId);
            if (inviterParty == null)
            {
                Reply(target.Id, "That invite is no longer valid.");
                return;
            }
            LeaveCurrentParty(target.Id);
            PartyManager.AddMember(inviterParty, target.Id, target.Name);
            PartyManager.SaveAndPublish(inviterParty);
            Reply(invite.InviterId, $"{target.Name} accepted your invite.");
        }

        /// <summary>A player can belong to at most one party; accepting a new invite quietly leaves the old one.</summary>
        private static void LeaveCurrentParty(long playerId)
        {
            PartyRecord current = PartyManager.FindPartyOf(playerId);
            if (current == null)
                return;
            PartyManager.RemoveMember(current, playerId);
            PartyManager.EnsureStore().Save();
            if (current.Members.Count > 0)
                PartyPublisher.Publish(current);
        }

        public static void Expire(long targetId)
        {
            if (!InviteManager.TryTake(targetId, out PendingInvite invite))
                return;
            Reply(invite.InviterId, $"{invite.TargetName} did not respond in time.");
        }

        private static void Reply(long playerId, string text) => PartyRpcServer.Reply(playerId, text);
    }
}
