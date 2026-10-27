using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a rope ties on to its ship (<see cref="KwRigTie"/>), from the ship's own shape (<see cref="KwRigShip"/>), the
    /// same on every peer. A rope to a side (a shroud, a brace, a sheet) ties to the top of the ship's side on the side it
    /// aims at, at the place along the ship it aims at: on a deck, the hull's side (its gunwale, or its railing); on a cabin's
    /// roof, that roof's outer edge (its railing's), or, where the edge is too near for a sensible angle (out less than
    /// <see cref="Sensible"/> of the rope's height), the hull's side beyond the cabin when that is clear. Never through a deck,
    /// a cabin, a railing, a sail, a yard or a mast (<see cref="KwRigClear"/>): when the straight way is blocked, the nearest
    /// clear place along the side. A rope down the middle (a forestay, a boom's sheet) ties to the deck or roof under it.
    /// </summary>
    public static class KwRigTies
    {
        /// <summary>How much of a rope's height it must reach out from it at its foot to be at a sensible angle.</summary>
        public const float Sensible = 0.25f;

        /// <summary>How near another rope's foot a rope's may be.</summary>
        public const float Spacing = 0.25f;

        // How far along the side a tie may move from where it aims on a deck, and to the hull's side past a roof's edge.
        private const float DeckReach = 6f, PastRoof = 1.5f;

        // The shares of the way out toward a rope's aim down the middle that are tried in turn.
        private static readonly float[] Shares = { 1f, 0.75f, 0.5f, 0.25f };

        /// <summary>
        /// A rope from a point to the ship's side its aim is on, made fast by a kind of fitting, away from the places other
        /// ropes of its piece took already (<see cref="Spacing"/>), where there is room.
        /// </summary>
        public static KwRigTie Side(KwRigShip ship, Vector3 from, Vector3 aim, KwTieKind kind, List<Vector3>? taken = null)
        {
            (float along, float side) = ship.Place(from, aim);
            KwRigAsk ask = new KwRigAsk(ship, from, kind, taken);
            KwRigCabin? roof = ship.Base;
            KwRigTie? tie = roof != null && Nearest(ship, e => e.Roof == roof && e.Side == side, along) is KwRigEdge edge
                ? OnRoof(ask, along, side, edge)
                : OnDeck(ask, along, side);
            return tie ?? Down(ship, from, aim);
        }

        // A mast on a cabin's roof: the roof's edge at a sensible angle; else the hull's side just past it; else the roof's
        // edge anyway (its nearest clear place, else where it aims).
        private static KwRigTie OnRoof(KwRigAsk ask, float along, float side, KwRigEdge edge)
        {
            KwRigTie own = ask.At(edge);
            if (Angled(ask.From, own.End) && ask.Free(own) && ask.Clear(own))
                return own;
            KwRigCabin roof = edge.Roof!;
            return Search(ask, along, side, e => e.Roof == null, PastRoof, true)
                ?? Search(ask, along, side, e => e.Roof == roof, float.MaxValue, false)
                ?? own;
        }

        // A mast on a deck: the hull's side near where it aims; else a cabin roof's edge there at a sensible angle; else the
        // nearest clear place on the hull's side, or a roof's edge, anywhere; else the hull's side anyway.
        private static KwRigTie? OnDeck(KwRigAsk ask, float along, float side)
        {
            KwRigTie? tie = Search(ask, along, side, e => e.Roof == null, DeckReach, false)
                ?? Search(ask, along, side, e => e.Roof != null, DeckReach, true)
                ?? Search(ask, along, side, e => e.Roof == null, float.MaxValue, false)
                ?? Search(ask, along, side, e => e.Roof != null, float.MaxValue, false);
            KwRigEdge? nearest = tie == null ? Nearest(ask.Ship, e => e.Roof == null && e.Side == side, along) : null;
            return tie ?? (nearest != null ? ask.At(nearest) : null);
        }

        // The clear, free place nearest the aim along one side among some edges, within a reach of it.
        private static KwRigTie? Search(KwRigAsk ask, float along, float side, Predicate<KwRigEdge> which, float reach, bool angled)
        {
            List<KwRigEdge> edges = ask.Ship.Edges.FindAll(e => e.Side == side && Mathf.Abs(e.Along - along) <= reach && which(e));
            edges.Sort((a, b) => Mathf.Abs(a.Along - along).CompareTo(Mathf.Abs(b.Along - along)));
            foreach (KwRigEdge edge in edges)
            {
                KwRigTie tie = ask.At(edge);
                if ((!angled || Angled(ask.From, tie.End)) && ask.Free(tie) && ask.Clear(tie))
                    return tie;
            }
            return null;
        }

        private static KwRigEdge? Nearest(KwRigShip ship, Predicate<KwRigEdge> which, float along)
        {
            KwRigEdge? best = null;
            foreach (KwRigEdge edge in ship.Edges)
            {
                if (which(edge) && (best == null || Mathf.Abs(edge.Along - along) < Mathf.Abs(best.Along - along)))
                    best = edge;
            }
            return best;
        }

        /// <summary>Whether a rope from a top to an end reaches out from its height at a sensible angle.</summary>
        public static bool Angled(Vector3 top, Vector3 end) =>
            new Vector2(top.x - end.x, top.y - end.y).magnitude >= Sensible * (top.z - end.z);

        /// <summary>
        /// A rope from a point down to the deck or roof under its aim (an eyebolt there), or nearer the piece's foot when that
        /// way is blocked; at last an eyebolt beside the foot.
        /// </summary>
        public static KwRigTie Down(KwRigShip? ship, Vector3 from, Vector3 aim)
        {
            KwBiomeTier tier = ship?.Tier ?? KwBiomeTiers.Meadows;
            Vector3 outward = new Vector3(aim.x, aim.y, 0f).sqrMagnitude > 1e-4f ? new Vector3(aim.x, aim.y, 0f).normalized : Vector3.right;
            foreach (float share in Shares)
            {
                Vector3 at = new Vector3(aim.x * share, aim.y * share, 0f);
                float? top = ship?.Top(at, from.z - 0.3f);
                if (top == null)
                    continue;
                KwRigTie tie = KwRigTie.Eyebolt(new Vector3(at.x, at.y, top.Value), outward, tier);
                if (KwRigClear.Clear(ship!, from, tie.End))
                    return tie;
            }
            return KwRigTie.Eyebolt(new Vector3(aim.x * 0.1f, aim.y * 0.1f, 0f), outward, tier);
        }
    }

    /// <summary>One rope being tied on (<see cref="KwRigTies"/>): its ship, its top, what holds its foot, and the places other ropes took.</summary>
    public readonly struct KwRigAsk
    {
        public KwRigAsk(KwRigShip ship, Vector3 from, KwTieKind kind, List<Vector3>? taken) => (Ship, From, Kind, Taken) = (ship, from, kind, taken);

        public KwRigShip Ship { get; }
        public Vector3 From { get; }
        public KwTieKind Kind { get; }
        public List<Vector3>? Taken { get; }

        public KwRigTie At(KwRigEdge edge) => KwRigTie.At(edge, Kind, From);

        /// <summary>Whether a tie's foot is away from every other rope's.</summary>
        public bool Free(KwRigTie tie)
        {
            Vector3 end = tie.End;
            return Taken == null || !Taken.Exists(p => (p - end).sqrMagnitude < KwRigTies.Spacing * KwRigTies.Spacing);
        }

        public bool Clear(KwRigTie tie) => KwRigClear.Clear(Ship, From, tie.End);
    }

    /// <summary>
    /// Whether a rope is clear of its ship's solids (<see cref="KwRigShip.Solids"/>): sampled every <see cref="Step"/> along it
    /// as it hangs (its sag, <see cref="KwSailRigging.Sag"/>), from its end up, kept <see cref="Rope"/> off every solid; its
    /// last few centimetres at each end left out (its fitting at the foot, its sail, yard or mast at the top).
    /// </summary>
    public static class KwRigClear
    {
        /// <summary>The step along a rope, its clearance, and how much of it at its foot and its top is not tested.</summary>
        public const float Step = 0.1f, Rope = 0.03f, Foot = 0.25f, Head = 0.35f;

        public static bool Clear(KwRigShip ship, Vector3 top, Vector3 end)
        {
            float length = (top - end).magnitude, sag = KwSailRigging.SagOf(length);
            List<KwAnchorSolid> near = ship.Near(top, end, Rope + sag);
            for (float d = Foot; d < length - Head; d += Step)
            {
                Vector3 p = KwSailRigging.Hanging(top, end, 1f - d / length, sag);
                foreach (KwAnchorSolid solid in near)
                {
                    if (solid.Blocks(p, Rope, false))
                        return false;
                }
            }
            return true;
        }
    }
}
