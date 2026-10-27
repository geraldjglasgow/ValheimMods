using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A barrel drawn longer, a ship cannon's (<see cref="KwShipCannons"/>), along the barrel from the trunnions: every
    /// point of it at or behind <see cref="BreechCut"/> moved back by <see cref="Back"/> and every point at or ahead of
    /// <see cref="ChaseCut"/> moved forward by <see cref="Fore"/>. Each cut lies in a plain run of the barrel (the reinforce
    /// behind the trunnions, the chase ahead of them), so those runs grow longer while every ring, hoop and band, the breech
    /// with its door and loads, and the muzzle keep their shapes. <see cref="None"/> (the default) leaves a barrel at its
    /// shape's own length.
    /// </summary>
    public readonly struct KwStretch
    {
        public KwStretch(float breechCut, float back, float chaseCut, float fore)
        {
            (BreechCut, Back, ChaseCut, Fore) = (breechCut, back, chaseCut, fore);
        }

        public static KwStretch None => default;

        public float BreechCut { get; }
        public float Back { get; }
        public float ChaseCut { get; }
        public float Fore { get; }

        /// <summary>Where a point along the barrel (x from the trunnions) lies on the longer barrel.</summary>
        public float X(float x) => x <= BreechCut ? x - Back : x >= ChaseCut ? x + Fore : x;

        /// <summary>A point of the barrel (its pitch part's kit axes) on the longer barrel.</summary>
        public Vector3 At(Vector3 p) => new Vector3(X(p.x), p.y, p.z);

        /// <summary>A barrel's outline, (x, radius) back to front, drawn longer.</summary>
        public Vector2[] Outline(IReadOnlyList<Vector2> outline)
        {
            Vector2[] longer = new Vector2[outline.Count];
            for (int i = 0; i < longer.Length; i++)
                longer[i] = new Vector2(X(outline[i].x), outline[i].y);
            return longer;
        }

        /// <summary>The breech at the back of the longer barrel (the same breech when it is not moved back).</summary>
        public KwBreech Breech(KwBreech breech) => Back == 0f ? breech : breech.Moved(-Back);
    }
}
