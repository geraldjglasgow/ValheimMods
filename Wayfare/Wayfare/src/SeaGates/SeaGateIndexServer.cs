using System.Collections.Generic;
using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.SeaGates
{
    /// <summary>The server's half of <see cref="SeaGateIndex"/>: the gates built from the pillars <see cref="SeaGateScan"/>
    /// found, and the peers waiting for a list. A request is answered from a pass at most
    /// <see cref="SeaGateScan.FreshSeconds"/> old; when the last pass is older, the request waits for the next one (a
    /// second or so). Each peer gets only the gates its player may target, by the portals' own access rule, so a
    /// private gate's position is never sent to a player who could not sail to it.</summary>
    internal static class SeaGateIndexServer
    {
        private static readonly HashSet<long> waiting = new HashSet<long>();

        internal static void Reset() => waiting.Clear();

        /// <summary>A peer (or this machine's own routed id, for a host's own map) wants the list.</summary>
        internal static void Enqueue(long peer)
        {
            waiting.Add(peer);
            SeaGateScan.Want();
        }

        /// <summary>Every frame on the server: advance the pass, answer the waiting peers once a fresh one is in.</summary>
        internal static void Tick()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZRoutedRpc.instance == null)
                return;
            SeaGateScan.Step();
            if (waiting.Count == 0)
                return;
            if (!SeaGateScan.IsFresh)
            {
                SeaGateScan.Want();
                return;
            }
            AnswerWaiting(BuildGates());
        }

        private static void AnswerWaiting(List<SeaGateInfo> gates)
        {
            long self = ZRoutedRpc.instance.m_id;
            foreach (long peer in waiting)
            {
                List<SeaGateInfo> visible = VisibleTo(gates, peer);
                if (peer == self)
                    SeaGateIndex.SetSnapshot(visible);
                else
                    SeaGateIndex.Send(peer, visible);
            }
            waiting.Clear();
        }

        private static List<SeaGateInfo> VisibleTo(List<SeaGateInfo> gates, long peer)
        {
            long playerId = SenderIdentity.PlayerId(peer);
            bool isAdmin = SenderIdentity.IsAdmin(peer);
            List<SeaGateInfo> visible = new List<SeaGateInfo>(gates.Count);
            foreach (SeaGateInfo gate in gates)
            {
                if (PortalAccess.MayTarget(gate.Mode, gate.Owner, playerId, isAdmin))
                    visible.Add(gate);
            }
            return visible;
        }

        /// <summary>Every mutual pair among the pillars, once, by its anchor.</summary>
        internal static List<SeaGateInfo> BuildGates()
        {
            List<ZDO> pillars = SeaGateScan.Pillars();
            Dictionary<long, ZDO> byId = ById(pillars);
            List<SeaGateInfo> gates = new List<SeaGateInfo>();
            foreach (ZDO anchor in pillars)
            {
                if (!byId.TryGetValue(SeaGateFields.GetPartner(anchor), out ZDO partner))
                    continue;
                if (SeaGateFields.IsMutual(anchor, partner) && SeaGateFields.IsAnchor(anchor, partner))
                    gates.Add(ToInfo(anchor, partner));
            }
            return gates;
        }

        /// <summary>A gate id to its two pillars, from the last pass; only when they name each other.</summary>
        internal static bool TryFind(long gateId, out ZDO anchor, out ZDO partner)
        {
            anchor = null;
            partner = null;
            List<ZDO> pillars = SeaGateScan.Pillars();
            Dictionary<long, ZDO> byId = ById(pillars);
            if (!byId.TryGetValue(gateId, out ZDO first) || !byId.TryGetValue(SeaGateFields.GetPartner(first), out ZDO second))
                return false;
            if (!SeaGateFields.IsMutual(first, second) || !SeaGateFields.IsAnchor(first, second))
                return false;
            anchor = first;
            partner = second;
            return true;
        }

        private static Dictionary<long, ZDO> ById(List<ZDO> pillars)
        {
            Dictionary<long, ZDO> byId = new Dictionary<long, ZDO>(pillars.Count);
            foreach (ZDO pillar in pillars)
            {
                long id = SeaGateFields.GetId(pillar);
                if (id != 0L)
                    byId[id] = pillar;
            }
            return byId;
        }

        private static SeaGateInfo ToInfo(ZDO anchor, ZDO partner)
        {
            return new SeaGateInfo(SeaGateFields.GetId(anchor), (anchor.GetPosition() + partner.GetPosition()) * 0.5f,
                SeaGateFields.GetName(anchor), PortalFields.GetMode(anchor), PortalFields.GetOwner(anchor));
        }
    }
}
