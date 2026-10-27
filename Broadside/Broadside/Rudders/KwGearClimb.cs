using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The steering gear to a captain's wheel on a cabin's roof (the user's request, 2026-10-25: "allow the captain's wheel
    /// to be placed up on a cabin's rooftop ... the cogs and wooden pieces ... go up the back wall of the cabin, then over the
    /// top of the ceiling, to wherever the captain's wheel is"), in the rudder's kit frame. The quadrant and pinion stay as
    /// for a deck's wheel; the line shaft comes forward under the deck the lowest cabin stands on to the cabin's back wall
    /// (<see cref="KwRudderGearRoute.Lay"/>), a bevel box turns it up, it climbs the wall's inside through the floor in
    /// brackets (<see cref="KwGearRisers"/>), a box under the roof turns it forward along the roof's underside in hangers
    /// (<see cref="KwGearCeiling"/>), and the box under the wheel turns it up through the roof into the wheel's stand, as
    /// through a deck. A cabin on a cabin is climbed in turn: from under the lower roof up the upper cabin's back wall, one
    /// straight climb through both where their back walls meet. The climbs are tried best first (<see cref="KwGearRisers.For"/>),
    /// the first whose whole way is clear taken; refused (<see cref="KwGearWhy.Climb"/>, <see cref="KwGearWhy.Ceiling"/>,
    /// <see cref="KwGearWhy.Route"/>) only when none is. Plain maths over the ship's pieces, so every peer lays the same.
    /// </summary>
    public static class KwGearClimb
    {
        // How many climbs of each cabin are tried; how far forward of where the shaft may first bend a climb off its line must be.
        private const int Tries = 6;
        private const float Bends = 0.3f;

        /// <summary>The gear to a wheel standing on the chain's top roof, or why there is none.</summary>
        internal static KwRudderGear Find(KwGearShip ship, KwGearRooms rooms, Vector3 pinion, float post, Vector3 wheel, float level)
        {
            KwGearLine line = new KwGearLine(ship, post, level);
            int last = rooms.Chain.Count - 1;
            Vector3 under = new Vector3(wheel.x, wheel.y, rooms.Chain[last].Top - KwGearTrain.Below);
            if (!rooms.Ceiling(last).Corner(under))
                return KwRudderGear.Refused(wheel, KwGearWhy.Ceiling);
            KwGearWhy why = KwGearWhy.Climb;
            foreach (KwGearRiser first in Firsts(rooms, line, pinion, post))
            {
                List<(KwGearRiser Riser, List<Vector3> Route)>? up = Above(rooms, 0, first, under, ref why);
                List<Vector3>? low = up != null ? KwRudderGearRoute.Lay(line, pinion, first.Foot, post) : null;
                if (up != null && low == null)
                    why = KwGearWhy.Route;
                if (low != null)
                    return Built(rooms, line, wheel, post, low, up!);
            }
            return KwRudderGear.Refused(wheel, why);
        }

        // The lowest cabin's climbs whose foot is under the deck with a bevel box's room there and the shaft can reach it
        // (nearly in line with the bore, or far enough forward of the post to bend to it), best first.
        private static List<KwGearRiser> Firsts(KwGearRooms rooms, KwGearLine line, Vector3 pinion, float post)
        {
            List<KwGearRiser> firsts = KwGearRisers.For(rooms, 0, new Vector2(pinion.x, pinion.y), (x, y) =>
            {
                bool reached = Mathf.Abs(y - pinion.y) <= KwRudderGearRoute.Skew * (x - pinion.x) || x >= post + KwRudderGearFind.StemGap + Bends;
                float? z = reached ? line.At(x, y) : null;
                return z != null && line.Corner(new Vector3(x, y, z.Value)) ? z : null;
            });
            if (firsts.Count > Tries)
                firsts.RemoveRange(Tries, firsts.Count - Tries);
            return firsts;
        }

        // From a climb of cabin i on up: under its roof to the wheel (the top cabin) or to a climb of the cabin on it, and
        // on from there; each climb with its roof's way after it. Null with no clear way, saying how far it got.
        private static List<(KwGearRiser Riser, List<Vector3> Route)>? Above(KwGearRooms rooms, int i, KwGearRiser riser, Vector3 under, ref KwGearWhy why)
        {
            KwGearCeiling ceiling = rooms.Ceiling(i);
            if (i == rooms.Chain.Count - 1)
            {
                List<Vector3>? route = ceiling.Route(riser.Head, under);
                if (route == null)
                    why = Worse(why, KwGearWhy.Ceiling);
                return route == null ? null : new List<(KwGearRiser Riser, List<Vector3> Route)> { (riser, route) };
            }
            float footZ = riser.HeadZ;
            foreach (KwGearRiser next in Nexts(rooms, i + 1, riser, footZ))
            {
                List<Vector3>? route = Couples(riser, next) ? new List<Vector3>() : ceiling.Corner(next.Foot) ? ceiling.Route(riser.Head, next.Foot) : null;
                List<(KwGearRiser Riser, List<Vector3> Route)>? rest = route != null ? Above(rooms, i + 1, next, under, ref why) : null;
                if (route == null)
                    why = Worse(why, KwGearWhy.Ceiling);
                if (rest != null)
                {
                    rest.Insert(0, (riser, route!));
                    return rest;
                }
            }
            return null;
        }

        // The climbs of the cabin on cabin i - 1's roof, their feet under that roof, best first (straight on up from the one below first).
        private static List<KwGearRiser> Nexts(KwGearRooms rooms, int i, KwGearRiser below, float footZ)
        {
            KwGearRoom lower = rooms.Chain[i - 1];
            List<KwGearRiser> all = KwGearRisers.For(rooms, i, new Vector2(below.Foot.x, below.Foot.y), (x, y) => lower.Covers(x, y) ? footZ : (float?)null);
            if (all.Count > Tries)
                all.RemoveRange(Tries, all.Count - Tries);
            return all;
        }

        // Whether the climb above starts where the one below ends: one straight climb through the roof between.
        private static bool Couples(KwGearRiser below, KwGearRiser above) =>
            new Vector2(above.Foot.x - below.Foot.x, above.Foot.y - below.Foot.y).sqrMagnitude < 1e-4f;

        // The further of two refusals along the way: no climb, then no way under a roof, then no way under the deck.
        private static KwGearWhy Worse(KwGearWhy a, KwGearWhy b) => a == KwGearWhy.Route || b == KwGearWhy.Climb ? a : b;

        // The gear: under the deck from the pinion to the first climb's foot, then each climb's head and its roof's way,
        // the hangers under the deck and the roofs, the brackets on the walls.
        private static KwRudderGear Built(KwGearRooms rooms, KwGearLine line, Vector3 wheel, float post, List<Vector3> low, List<(KwGearRiser Riser, List<Vector3> Route)> up)
        {
            List<Vector3> corners = new List<Vector3>(low);
            List<Vector4> hangers = new List<Vector4>(KwRudderGearRoute.Hangers(line, low, post));
            List<KwGearBracket> brackets = new List<KwGearBracket>();
            for (int i = 0; i < up.Count; i++)
            {
                (KwGearRiser riser, List<Vector3> route) = up[i];
                corners.Add(riser.Head);
                KwGearRisers.Brackets(rooms, i, riser, brackets);
                foreach (Vector3 c in route)
                {
                    KwRudderGearRoute.Along(rooms.Ceiling(i), corners[corners.Count - 1], c, float.MinValue, hangers);
                    corners.Add(c);
                }
            }
            return KwRudderGear.Shafted(wheel, corners.ToArray(), hangers.ToArray(), brackets.ToArray());
        }
    }
}
