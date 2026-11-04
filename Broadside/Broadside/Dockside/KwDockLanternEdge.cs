using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Which edge a dock lantern goes on, and where (DOCKS.md, "Dock lanterns"). On a dock section (<see cref="Dock"/>): the
    /// edge of its deck nearest a point, within <see cref="Zone"/>; on a side, halfway between the two stations nearest the
    /// point (clear of the pilings and the rough cap beams' ends), refused where another dock branches off within
    /// <see cref="Clear"/> of it; on an end or start only when no other dock carries on from it, at the nearest
    /// <see cref="KwPierLadderEdge.Snap"/> across it, kept <see cref="Clear"/> in from its corners. On a game floor
    /// (<see cref="Floor"/>, as the dock ladders read one: a level box with "floor" in its name, <see cref="KwPierLadderEdge.Floor"/>)
    /// the nearest side of its box, over the water only, in wood (core wood on a hard wood floor). Then its post: straight
    /// down from the deck's top to the sea floor or the ground under it, sunk <see cref="KwPierPlan.Embed"/> into it
    /// (<see cref="Post"/>). Plain maths and one cast.
    /// </summary>
    public static class KwDockLanternEdge
    {
        public const float Zone = 1.2f, Clear = 0.3f;
        private const float Wet = 0.1f;
        private static readonly int Ground = LayerMask.GetMask("terrain", "static_solid", "Default", "Default_small");

        /// <summary>A dock section's edge near a point (the point's along and across on its deck).</summary>
        public static KwDockLanternSite Dock(KwPier pier, Vector3 point)
        {
            KwPierDeck? deck = KwPierDeck.Of(pier);
            if (deck == null || pier.Spec == null)
                return KwDockLanternSite.Refused("This dock has not finished building here yet");
            Vector3 d = point - deck.Start;
            float along = Vector3.Dot(d, deck.Forward), across = Vector3.Dot(d, deck.Right), half = deck.Width * 0.5f;
            float[] near = { half + across, half - across, along, deck.Length - along };
            int edge = Array.IndexOf(near, Mathf.Min(near[0], near[1], near[2], near[3]));
            if (near[edge] > Zone)
                return KwDockLanternSite.Refused("Aim at the dock's side where the lantern should stand");
            KwPierEdge side = deck.Edges[edge];
            bool end = edge >= KwPierGaps.Start;
            if (end && !side.Open)
                return KwDockLanternSite.Refused("Another dock carries on from there");
            float at = end ? Across(across + half, deck.Width) : Between(pier.Spec, along);
            if (!end && Meets(side.Joins, at))
                return KwDockLanternSite.Refused("A dock branches off there");
            Vector3 top = side.A + (side.B - side.A).normalized * at;
            return KwDockLanternSite.At(new Vector3(top.x, deck.Top, top.z), side.Outward, pier.Material, pier);
        }

        // Halfway between the two stations either side of a point along a section.
        private static float Between(KwPierSpec spec, float along)
        {
            float best = spec.Length * 0.5f;
            for (int i = 0; i + 1 < spec.Stations; i++)
            {
                float mid = (spec.StationZ(i) + spec.StationZ(i + 1)) * 0.5f;
                if (Mathf.Abs(mid - along) < Mathf.Abs(best - along))
                    best = mid;
            }
            return best;
        }

        // Metres across an end snapped, kept clear of its corners (its middle on an end too narrow for that).
        private static float Across(float at, float width) =>
            width > 2f * Clear ? Mathf.Clamp(Mathf.Round(at / KwPierLadderEdge.Snap) * KwPierLadderEdge.Snap, Clear, width - Clear) : width * 0.5f;

        private static bool Meets(IReadOnlyList<Vector2> joins, float at)
        {
            foreach (Vector2 join in joins)
            {
                if (join.x < at + Clear && join.y > at - Clear)
                    return true;
            }
            return false;
        }

        /// <summary>A game floor's edge at a hit, over the water; null for anything that is not a level floor with a box.</summary>
        public static KwDockLanternSite? Floor(Piece piece, RaycastHit hit)
        {
            KwPierLadderSite? edge = KwPierLadderEdge.Floor(piece, hit);
            if (edge == null)
                return null;
            if (edge.Why != null)
                return KwDockLanternSite.Refused(edge.Why.StartsWith("Aim", StringComparison.Ordinal) ? "Aim at the floor's edge where the lantern should stand" : edge.Why);
            KwDockLanternSite site = KwDockLanternSite.At(edge.Top, edge.Outward, edge.Kind, null);
            float water = Floating.GetLiquidLevel(new Vector3(site.Origin.x, KwPierAim.Sea, site.Origin.z), 0f, LiquidType.Water);
            float? floor = Bottom(site.Origin);
            if (water < -1000f || floor == null || floor.Value > water - Wet)
                site.Why = "A dock lantern stands beside a dock, or a floor over the water";
            return site;
        }

        /// <summary>The post's length from the deck's top to its foot sunk in the ground under its axis; why not when it cannot stand.</summary>
        public static string? Post(KwDockLanternSite site)
        {
            float? bottom = Bottom(site.Origin);
            if (bottom == null)
                return $"Too deep here for the lantern's post ({KwPierPlan.MaxPiling:0} m at most)";
            float length = Mathf.Ceil((site.Origin.y - bottom.Value + KwPierPlan.Embed) / 0.05f - 0.01f) * 0.05f;
            if (length < KwPierPlan.Embed + 0.05f)
                return "The ground rises past the deck here";
            site.Length = length;
            return length > KwDockLanternSpec.MaxLength ? $"Too deep here for the lantern's post ({KwPierPlan.MaxPiling:0} m at most)" : null;
        }

        // The sea floor or ground straight under a point (from just over it); null with none within the longest post.
        private static float? Bottom(Vector3 at) =>
            Physics.Raycast(at + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, KwDockLanternSpec.MaxLength + 0.5f, Ground, QueryTriggerInteraction.Ignore) ? hit.point.y : (float?)null;
    }
}
