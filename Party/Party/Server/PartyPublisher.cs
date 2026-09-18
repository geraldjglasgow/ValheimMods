using System.Linq;
using System.Text;
using Party.Api;

namespace Party.Server
{
    /// <summary>Builds the <c>Party_Roster</c> wire text and sends it to every online member of a party.</summary>
    public static class PartyPublisher
    {
        public const string Rpc = "Party_Roster";

        public static void Publish(PartyRecord party)
        {
            if (!Identity.IsServer)
                return;
            string text = Encode(party);
            foreach (PartyMember member in party.Members)
            {
                long peerId = Identity.PeerIdFor(member.Id);
                if (peerId != 0)
                    ZRoutedRpc.instance.InvokeRoutedRPC(peerId, Rpc, text);
                PartyApi.RaiseChanged(member.Id);
            }
        }

        /// <summary>Tells one player they have no party, e.g. after being removed or after their party dissolved.</summary>
        public static void PublishEmpty(long playerId)
        {
            long peerId = Identity.PeerIdFor(playerId);
            if (peerId != 0)
                ZRoutedRpc.instance.InvokeRoutedRPC(peerId, Rpc, "");
        }

        public static void PublishAll(PartyFile file)
        {
            foreach (PartyRecord party in file.Parties)
                Publish(party);
        }

        /// <summary>partyId\tleaderId, then one id\tname\tonline(0/1) line per member.</summary>
        private static string Encode(PartyRecord party)
        {
            StringBuilder text = new StringBuilder();
            text.Append(party.PartyId).Append('\t').Append(party.LeaderId).Append('\n');
            foreach (PartyMember member in party.Members)
            {
                bool online = Identity.TryFind(member.Id, out _);
                text.Append(member.Id).Append('\t').Append(member.Name).Append('\t').Append(online ? '1' : '0').Append('\n');
            }
            return text.ToString();
        }
    }
}
