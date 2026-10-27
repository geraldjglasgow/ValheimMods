using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One place a ladder's top may go: on an edge (kit x, y), and the way it faces, square off that edge (into a hatch's opening, out over a deck's edge).</summary>
    public readonly struct KwLadderSpot
    {
        public KwLadderSpot(Vector2 at, Vector2 facing)
        {
            At = at;
            Facing = facing;
        }

        public Vector2 At { get; }
        public Vector2 Facing { get; }
    }

    /// <summary>
    /// Where the ladder tool's one click puts a ladder (<see cref="KwLadderTool"/>), snapped. In a hatch's opening each of
    /// its four edges has five spots: one rail in a corner, the ladder's middle a quarter, half and three quarters along,
    /// the other rail in the other corner, all kept <see cref="Room"/> inside the opening, facing into it; the one nearest
    /// the aim is taken. On a deck's or a cabin roof's open edge the ladder's middle snaps every <see cref="Step"/> along it
    /// from the edge's middle (<see cref="Grid"/>), kept within the edge and clear of the corner posts at its ends, where no
    /// free span's middle or quarter point is near (<see cref="KwLadderSpans"/>). Kit axes (x along, y across). Pure.
    /// </summary>
    public static class KwLadderSpots
    {
        /// <summary>The ladder's half width over its rails, the room kept between a rail and a corner, and an open edge's snap.</summary>
        public const float Half = 0.25f, Room = 0.02f, Step = 0.25f;

        /// <summary>
        /// The five places along an edge that long, as the ladder's middle from the edge's middle: a rail in one corner, a
        /// quarter, half and three quarters along, a rail in the other corner, each kept inside the edge.
        /// </summary>
        public static float[] Along(float length)
        {
            float most = Mathf.Max(0f, length / 2f - Half - Room);
            return new[] { -most, Mathf.Clamp(-length / 4f, -most, most), 0f, Mathf.Clamp(length / 4f, -most, most), most };
        }

        /// <summary>A hatch opening's twenty spots (its length along x, its width across y; the origin in its middle), five an edge, each facing into it.</summary>
        public static List<KwLadderSpot> Hatch(Vector2 opening)
        {
            List<KwLadderSpot> spots = new List<KwLadderSpot>();
            float hx = opening.x / 2f, hy = opening.y / 2f;
            foreach (float sign in new[] { 1f, -1f })
            {
                foreach (float y in Along(opening.y))
                    spots.Add(new KwLadderSpot(new Vector2(sign * hx, y), new Vector2(-sign, 0f)));
                foreach (float x in Along(opening.x))
                    spots.Add(new KwLadderSpot(new Vector2(x, sign * hy), new Vector2(0f, -sign)));
            }
            return spots;
        }

        /// <summary>The spot nearest a point (the aim, kit); the first for none.</summary>
        public static KwLadderSpot Nearest(List<KwLadderSpot> spots, Vector2 aim)
        {
            KwLadderSpot best = spots[0];
            foreach (KwLadderSpot spot in spots)
            {
                if ((spot.At - aim).sqrMagnitude < (best.At - aim).sqrMagnitude)
                    best = spot;
            }
            return best;
        }

        /// <summary>
        /// The ladder's middle on an open edge that long (from its start): the aim's place along it snapped to a whole number
        /// of steps from the edge's middle, kept so the ladder stays between lo and hi along it (the edge's ends, or the
        /// corner posts in from them); null when no step lies there.
        /// </summary>
        public static float? Grid(float length, float lo, float hi, float aim)
        {
            float middle = length / 2f;
            int first = Mathf.CeilToInt((lo + Half + Room - middle) / Step - 0.001f), last = Mathf.FloorToInt((hi - Half - Room - middle) / Step + 0.001f);
            if (last < first)
                return null;
            return middle + Mathf.Clamp(Mathf.Round((aim - middle) / Step), first, last) * Step;
        }
    }
}
