using System.Collections.Generic;
using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>One gate whose two pillars are both loaded on this machine and name each other.</summary>
    public sealed class LoadedGate
    {
        public readonly SeaGatePillar Anchor;
        public readonly SeaGatePillar Partner;
        public readonly GateGeometry Geometry;

        public LoadedGate(SeaGatePillar anchor, SeaGatePillar partner)
        {
            Anchor = anchor;
            Partner = partner;
            Geometry = new GateGeometry(anchor.transform.position, partner.transform.position);
        }

        /// <summary>The gate's id: its anchor's id.</summary>
        public long Id => Anchor.Id;
        public ZDO AnchorZdo => Anchor.Zdo;
        public long DestId => SeaGateFields.GetDest(AnchorZdo);
        public int Sides => SeaGateFields.GetSides(AnchorZdo);
        public string Name => SeaGateFields.GetName(AnchorZdo);

        public bool IsAlive => Anchor != null && Partner != null && Anchor.IsValid && Partner.IsValid &&
                               SeaGateFields.IsMutual(Anchor.Zdo, Partner.Zdo);
    }

    /// <summary>The sea gate pillars loaded on this machine (each <see cref="SeaGatePillar"/> adds itself on Awake and
    /// leaves on destroy) and the gates they form. The gate list is rebuilt when a pillar comes or goes and at most
    /// once a second otherwise, since pairing changes arrive as ZDO writes. Only loaded gates: the complete list lives
    /// on the server (<see cref="SeaGateIndex"/>).</summary>
    public static class SeaGateRegistry
    {
        private const float RebuildSeconds = 1f;

        private static readonly List<SeaGatePillar> pillars = new List<SeaGatePillar>();
        private static readonly List<LoadedGate> gates = new List<LoadedGate>();
        private static bool dirty = true;
        private static float builtAt;

        public static IReadOnlyList<SeaGatePillar> Pillars => pillars;

        public static IReadOnlyList<LoadedGate> Gates
        {
            get
            {
                if (dirty || Time.time - builtAt > RebuildSeconds)
                    Rebuild();
                return gates;
            }
        }

        internal static void Add(SeaGatePillar pillar)
        {
            if (!pillars.Contains(pillar))
                pillars.Add(pillar);
            dirty = true;
        }

        internal static void Remove(SeaGatePillar pillar)
        {
            pillars.Remove(pillar);
            dirty = true;
        }

        /// <summary>Marks the gate list stale, for a module that just changed a pairing.</summary>
        public static void Invalidate() => dirty = true;

        public static SeaGatePillar FindLoaded(long id)
        {
            if (id == 0L)
                return null;
            foreach (SeaGatePillar pillar in pillars)
            {
                if (pillar != null && pillar.IsValid && pillar.Id == id)
                    return pillar;
            }
            return null;
        }

        public static LoadedGate FindGate(long gateId)
        {
            foreach (LoadedGate gate in Gates)
            {
                if (gate.Id == gateId && gate.IsAlive)
                    return gate;
            }
            return null;
        }

        /// <summary>The gate a pillar belongs to, as anchor or partner; null when it is unpaired or its partner is not loaded.</summary>
        public static LoadedGate GateOf(SeaGatePillar pillar)
        {
            foreach (LoadedGate gate in Gates)
            {
                if ((gate.Anchor == pillar || gate.Partner == pillar) && gate.IsAlive)
                    return gate;
            }
            return null;
        }

        private static void Rebuild()
        {
            dirty = false;
            builtAt = Time.time;
            gates.Clear();
            pillars.RemoveAll(p => p == null);
            foreach (SeaGatePillar pillar in pillars)
            {
                if (!pillar.IsValid)
                    continue;
                SeaGatePillar partner = FindLoaded(SeaGateFields.GetPartner(pillar.Zdo));
                if (partner == null || !SeaGateFields.IsMutual(pillar.Zdo, partner.Zdo) || !SeaGateFields.IsAnchor(pillar.Zdo, partner.Zdo))
                    continue;
                gates.Add(new LoadedGate(pillar, partner));
            }
        }
    }
}
