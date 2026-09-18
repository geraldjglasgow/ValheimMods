using System.Collections.Generic;
using UnityEngine;
using Party.Api;

namespace Party.Client
{
    /// <summary>One member of the local player's own party, as last reported by the server.</summary>
    public sealed class PartyMemberView
    {
        public long Id;
        public string Name = "";
        public bool Online;
        public float Health = 1f;
        public float Stamina = 1f;
        public float Eitr = 1f;
        public Vector3 Position;
        public bool PositionValid;

        public bool IsLeader => Id == PartyClientState.LeaderId;
    }

    /// <summary>
    /// The local player's view of their own party: applied from <c>Party_Roster</c> (membership) and
    /// <c>Party_VitalsDeliver</c> (health/stamina/eitr/position). Nothing here is authoritative - the server owns
    /// the roster - this is only what the server has told this client about itself.
    /// </summary>
    public static class PartyClientState
    {
        public static string PartyId { get; private set; } = "";
        public static long LeaderId { get; private set; }
        public static List<PartyMemberView> Members { get; private set; } = new List<PartyMemberView>();

        public static bool InParty => Members.Count > 0;
        public static bool IsLeader => InParty && LeaderId == Identity.LocalPlayerId;

        public static PartyMemberView Find(long id) => Members.Find(m => m.Id == id);

        /// <summary>Parses the <c>Party_Roster</c> wire text (see <c>PartyPublisher.Encode</c>); an empty string means no party.</summary>
        public static void ApplyRoster(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Reset();
                return;
            }
            string[] lines = text.Split('\n');
            string[] header = lines[0].Split('\t');
            PartyId = header[0];
            LeaderId = long.Parse(header[1]);
            Members = ParseMembers(lines);
            PartyApi.RaiseChanged(Identity.LocalPlayerId);
        }

        private static List<PartyMemberView> ParseMembers(string[] lines)
        {
            List<PartyMemberView> parsed = new List<PartyMemberView>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0)
                    continue;
                string[] fields = lines[i].Split('\t');
                PartyMemberView member = Find(long.Parse(fields[0])) ?? new PartyMemberView { Id = long.Parse(fields[0]) };
                member.Name = fields[1];
                member.Online = fields[2] == "1";
                parsed.Add(member);
            }
            return parsed;
        }

        public static void ApplyVitals(long id, float health, float stamina, float eitr, Vector3 pos, bool posValid)
        {
            PartyMemberView member = Find(id);
            if (member == null)
                return;
            member.Health = health;
            member.Stamina = stamina;
            member.Eitr = eitr;
            member.Position = pos;
            member.PositionValid = posValid;
        }

        public static void Reset()
        {
            bool wasInParty = InParty;
            PartyId = "";
            LeaderId = 0;
            Members = new List<PartyMemberView>();
            if (wasInParty)
                PartyApi.RaiseChanged(Identity.LocalPlayerId);
        }
    }
}
