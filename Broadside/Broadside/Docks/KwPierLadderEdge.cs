using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Which edge the ladder tool's aim picks for a dock ladder (<see cref="KwPierLadderAim"/>), and where on it. On a dock
    /// section (<see cref="Dock"/>): the edge of its deck nearest the aim, within <see cref="Zone"/>; on a side, at the
    /// nearest <see cref="Snap"/> along it, kept <see cref="KwPierGaps.LadderSpan"/> in from the section's ends, refused where
    /// another dock branches off within that span; on an end or a start only when no other dock carries on from it, at
    /// the nearest <see cref="Snap"/> across it, kept that far in from its corners. On a game floor (<see cref="Floor"/>, a
    /// piece of the game's or another mod's with "floor" in its name, its box collider level): the nearest side of its box
    /// the same way, refused when another piece carries on past that edge; its ladder in wood, or core wood on a hard wood
    /// floor. Plain maths on what is loaded.
    /// </summary>
    public static class KwPierLadderEdge
    {
        public const float Zone = 1.2f, Snap = 0.25f;
        private const float FloorZone = 0.6f, Beyond = 0.35f;
        private static readonly int Pieces = LayerMask.GetMask("piece");
        private static readonly Collider[] found = new Collider[16];

        /// <summary>A dock section's edge at the aim.</summary>
        public static KwPierLadderSite Dock(KwPier pier, Vector3 point)
        {
            KwPierDeck? deck = KwPierDeck.Of(pier);
            if (deck == null)
                return KwPierLadderSite.Refused("This dock has not finished building here yet");
            Vector3 d = point - deck.Start;
            float along = Vector3.Dot(d, deck.Forward), across = Vector3.Dot(d, deck.Right), half = deck.Width * 0.5f;
            float[] near = { half + across, half - across, along, deck.Length - along };
            int edge = Array.IndexOf(near, Mathf.Min(near[0], near[1], near[2], near[3]));
            if (near[edge] > Zone)
                return KwPierLadderSite.Refused("Aim at the edge of the dock where the ladder should hang");
            KwPierEdge side = deck.Edges[edge];
            bool end = edge >= KwPierGaps.Start;
            if (end && !side.Open)
                return KwPierLadderSite.Refused("Another dock carries on from there");
            float length = end ? deck.Width : deck.Length, at = Within(end ? across + half : along, length);
            if (!end && Meets(side.Joins, at))
                return KwPierLadderSite.Refused("A dock branches off there");
            Vector3 top = side.A + (side.B - side.A).normalized * at;
            return new KwPierLadderSite { Top = new Vector3(top.x, deck.Top, top.z), Outward = side.Outward, Kind = pier.Material, Pier = pier, OnDock = true };
        }

        // Metres along an edge snapped, kept clear of its ends (its middle on an edge too short for that).
        private static float Within(float at, float length)
        {
            float clear = KwPierGaps.LadderSpan;
            return length > 2f * clear ? Mathf.Clamp(Mathf.Round(at / Snap) * Snap, clear, length - clear) : length * 0.5f;
        }

        private static bool Meets(IReadOnlyList<Vector2> joins, float at)
        {
            foreach (Vector2 join in joins)
            {
                if (join.x < at + KwPierGaps.LadderSpan && join.y > at - KwPierGaps.LadderSpan)
                    return true;
            }
            return false;
        }

        /// <summary>A game floor's edge at the aim; null for anything that is not a level floor with a box (the kit's ladder tool's).</summary>
        public static KwPierLadderSite? Floor(Piece piece, RaycastHit hit)
        {
            string name = Utils.GetPrefabName(piece.gameObject);
            if (!(hit.collider is BoxCollider box) || name.StartsWith("KW_", StringComparison.Ordinal) || name.StartsWith("BS_", StringComparison.Ordinal)
                || name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) < 0 || Vector3.Dot(box.transform.up, Vector3.up) < 0.98f)
                return null;
            Transform t = box.transform;
            Vector3 half = box.size * 0.5f, p = t.InverseTransformPoint(hit.point) - box.center, scale = t.lossyScale;
            float dx = (half.x - Mathf.Abs(p.x)) * Mathf.Abs(scale.x), dz = (half.z - Mathf.Abs(p.z)) * Mathf.Abs(scale.z);
            bool onX = dx < dz;
            if (Mathf.Min(dx, dz) > FloorZone)
                return KwPierLadderSite.Refused("Aim at the floor's edge where the ladder should hang");
            float sign = Mathf.Sign(onX ? p.x : p.z), extent = onX ? half.z * Mathf.Abs(scale.z) : half.x * Mathf.Abs(scale.x);
            float at = Within((onX ? p.z * Mathf.Abs(scale.z) : p.x * Mathf.Abs(scale.x)) + extent, 2f * extent) - extent;
            Vector3 local = box.center + (onX ? new Vector3(sign * half.x, half.y, at / Mathf.Abs(scale.z)) : new Vector3(at / Mathf.Abs(scale.x), half.y, sign * half.z));
            Vector3 top = t.TransformPoint(local), outward = t.TransformDirection(onX ? Vector3.right * sign : Vector3.forward * sign);
            outward = new Vector3(outward.x, 0f, outward.z).normalized;
            if (Continues(piece, top, outward))
                return KwPierLadderSite.Refused("Another piece carries on past this edge");
            WearNTear? wear = piece.GetComponent<WearNTear>();
            KwPierKind kind = wear != null && wear.m_materialType == WearNTear.MaterialType.HardWood ? KwPierKinds.CoreWood : KwPierKinds.Wood;
            return new KwPierLadderSite { Top = top, Outward = outward, Kind = kind, OnDock = false };
        }

        // Another piece just past the edge at the floor's height (a floor carrying on): no ladder there.
        private static bool Continues(Piece piece, Vector3 top, Vector3 outward)
        {
            int count = Physics.OverlapBoxNonAlloc(top + outward * Beyond + Vector3.down * 0.08f, new Vector3(0.25f, 0.06f, 0.25f), found, Quaternion.LookRotation(outward), Pieces, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Piece? other = found[i].GetComponentInParent<Piece>();
                if (other != null && other != piece && other.GetComponent<KwPierLadder>() == null)
                    return true;
            }
            return false;
        }
    }
}
