using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock section's four deck edges (<see cref="KwPierEdge"/>) and which face the water, from its saved piling feet: the
    /// ground under a piling is its foot less the depth it was sunk (<see cref="KwPierPlan.Embed"/>), over the water when
    /// below the sea's level. A side is over the water when half or more of its pilings are; its start when no other
    /// section meets it and its first pilings are; its end when no loaded section continues it and its last pilings are.
    /// Where other sections meet each edge is the section's gaps (<see cref="KwPierGaps"/>, as it was last built): a side
    /// a branch leaves is not open, its spans the branches'. Plain maths on what is saved; nothing cast.
    /// </summary>
    public static class KwPierEdges
    {
        public static IReadOnlyList<KwPierEdge> Of(KwPierDeck deck, KwPierSpec spec, KwPierGaps gaps)
        {
            float water = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
            Vector3 s = deck.Start, e = deck.End, r = deck.Right * (deck.Width * 0.5f);
            Vector2 first = spec.Depths[0], last = spec.Depths[spec.Stations - 1];
            IReadOnlyList<Vector2> left = gaps.On(KwPierGaps.Left), right = gaps.On(KwPierGaps.Right), start = gaps.On(KwPierGaps.Start), end = gaps.On(KwPierGaps.End);
            bool endOpen = !deck.ContinuedAtEnd() && end.Count == 0, startOpen = !deck.ContinuedAtStart() && start.Count == 0;
            return new[]
            {
                new KwPierEdge("left", s - r, e - r, -deck.Right, Share(spec, deck.Top, water, false) >= 0.5f, left.Count == 0, left),
                new KwPierEdge("right", s + r, e + r, deck.Right, Share(spec, deck.Top, water, true) >= 0.5f, right.Count == 0, right),
                new KwPierEdge("start", s - r, s + r, -deck.Forward, startOpen && Wet(first, deck.Top, water), startOpen, start),
                new KwPierEdge("end", e - r, e + r, deck.Forward, endOpen && Wet(last, deck.Top, water), endOpen, end),
            };
        }

        // The share of one side's pilings standing in the water.
        private static float Share(KwPierSpec spec, float top, float water, bool right)
        {
            int all = 0, wet = 0;
            foreach (Vector2 depth in spec.Depths)
            {
                float d = right ? depth.y : depth.x;
                if (d <= 0f)
                    continue;
                all++;
                wet += Ground(d, top) < water ? 1 : 0;
            }
            return all > 0 ? (float)wet / all : 0f;
        }

        // A station over the water: the ground under either of its pilings below the sea's level.
        private static bool Wet(Vector2 depth, float top, float water) =>
            depth.x > 0f && Ground(depth.x, top) < water || depth.y > 0f && Ground(depth.y, top) < water;

        private static float Ground(float depth, float top) => top - depth + KwPierPlan.Embed;
    }
}
