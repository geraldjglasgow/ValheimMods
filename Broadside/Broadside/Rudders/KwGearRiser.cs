using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One climb of the steering gear's shaft (<see cref="KwGearClimb"/>): from its foot, a corner under a floor (the deck's,
    /// or a lower cabin's roof), straight up through that floor and up the inside of a cabin's wall to its head, a corner
    /// under the cabin's roof; the wall it is held to (the way from the shaft to it, level), and how good a place it is (less
    /// is better: hugging the back wall in line with the shaft below first, a side wall near the back after).
    /// </summary>
    internal readonly struct KwGearRiser
    {
        public KwGearRiser(Vector3 foot, float head, Vector3 wall, float score) => (Foot, HeadZ, Wall, Score) = (foot, head, wall, score);

        public Vector3 Foot { get; }
        public float HeadZ { get; }
        public Vector3 Wall { get; }
        public float Score { get; }

        public Vector3 Head => new Vector3(Foot.x, Foot.y, HeadZ);
    }

    /// <summary>
    /// The places a shaft may climb a cabin of the chain at (<see cref="KwGearRooms"/>), best first: up its back wall (the
    /// end nearer the stern), as near it as the shaft and its bevel boxes fit, in every lane across it that is clear all the
    /// way up (of doorways, windows, corner posts, the hull's narrowing at a pointed stern, masts, ladders and windlasses),
    /// the lane nearest the shaft below first; else up a side wall, at the nearest clear place to the back. And the brackets
    /// holding a climb to its wall, no more than <see cref="Spacing"/> apart. Kit axes of the rudder.
    /// </summary>
    internal static class KwGearRisers
    {
        /// <summary>How far a climbing shaft keeps from all round it; the brackets' spacing.</summary>
        public const float Margin = 0.12f, Spacing = 1.2f;

        // Lanes across; steps in from a wall; how far in from the back wall a climb still hugs it; how much a lane's distance
        // from the one wanted counts against it; a side wall's places' steps, how far back they may be, how far apart those
        // kept are and how far in from the wall they may stand (a flaring hull is narrower under the deck than at the floor);
        // how far a bracket may reach and the steps its way is looked for in. The most places kept up the back and up each side.
        private const float Lane = 0.1f, Step = 0.05f, Hug = 0.28f, Off = 0.6f, SideStep = 0.1f, SideFar = 2.5f, SideApart = 0.3f, SideIn = 0.5f;
        private const float Reach = 0.8f, Turn = 5f;
        private const int Kept = 6, SideKept = 3;

        /// <summary>The places cabin i of the chain may be climbed at, best first, each with its foot at a height (or by a function of its place).</summary>
        public static List<KwGearRiser> For(KwGearRooms rooms, int i, Vector2 prefer, System.Func<float, float, float?> foot)
        {
            List<KwGearRiser> found = new List<KwGearRiser>();
            if (i > 0)
                Straight(rooms, i, prefer, foot, found);
            Back(rooms, i, prefer, foot, found);
            foreach (bool left in new[] { true, false })
                Side(rooms, i, left, foot, found);
            found.Sort((a, b) => a.Score.CompareTo(b.Score));
            return found;
        }

        // Up the back wall: each lane's nearest clear place to it, the lanes nearest the one wanted first, the best few kept.
        private static void Back(KwGearRooms rooms, int i, Vector2 prefer, System.Func<float, float, float?> foot, List<KwGearRiser> found)
        {
            KwGearRoom room = rooms.Chain[i];
            float left = room.Reach(true, Margin + Hug), right = room.Reach(false, Margin + Hug);
            List<float> lanes = new List<float>();
            for (float y = -right + Margin; y <= left - Margin; y += Lane)
                lanes.Add(y);
            float wanted = room.Plan(prefer.x, prefer.y).y;
            if (wanted > -right + Margin && wanted < left - Margin)
                lanes.Add(wanted);
            lanes.Sort((a, b) => Mathf.Abs(room.Point(0f, a, 0f).y - prefer.y).CompareTo(Mathf.Abs(room.Point(0f, b, 0f).y - prefer.y)));
            int kept = 0;
            foreach (float y in lanes)
            {
                float off = Off * Mathf.Abs(room.Point(0f, y, 0f).y - prefer.y);
                if (kept >= Kept && off > found[found.Count - 1].Score)
                    break;
                KwGearRiser? r = Hugging(rooms, i, y, prefer, foot, off);
                if (r == null)
                    continue;
                Keep(found, r.Value);
                kept++;
            }
        }

        // A lane's nearest clear place to the back wall, within a hug of it.
        private static KwGearRiser? Hugging(KwGearRooms rooms, int i, float y, Vector2 prefer, System.Func<float, float, float?> foot, float off)
        {
            KwGearRoom room = rooms.Chain[i];
            for (float along = KwGearCeiling.Box + 0.01f; along <= Margin + Hug + 1e-3f; along += Step)
            {
                Vector3 p = room.Point(along, y, 0f);
                float? z = foot(p.x, p.y);
                float score = along + off + (i > 0 ? Off * Mathf.Abs(p.x - prefer.x) : 0f);
                if (z != null && Climbs(rooms, i, p.x, p.y, z.Value))
                    return new KwGearRiser(new Vector3(p.x, p.y, z.Value), room.Top - KwGearTrain.Below, room.AftWay, score);
            }
            return null;
        }

        // Straight on up from the climb in the cabin below, when it fits there: the best of all (one climb through both).
        private static void Straight(KwGearRooms rooms, int i, Vector2 prefer, System.Func<float, float, float?> foot, List<KwGearRiser> found)
        {
            KwGearRoom room = rooms.Chain[i];
            float? z = foot(prefer.x, prefer.y);
            if (z == null || !Climbs(rooms, i, prefer.x, prefer.y, z.Value))
                return;
            Vector2 plan = room.Plan(prefer.x, prefer.y);
            Vector3 wall = plan.x <= Margin + Hug + Step ? room.AftWay : room.SideWay(plan.y > 0f);
            found.Add(new KwGearRiser(new Vector3(prefer.x, prefer.y, z.Value), room.Top - KwGearTrain.Below, wall, -1f));
        }

        // Up a side wall: the nearest few clear places to the back, a little apart, each hugging the wall as near as it fits.
        private static void Side(KwGearRooms rooms, int i, bool left, System.Func<float, float, float?> foot, List<KwGearRiser> found)
        {
            KwGearRoom room = rooms.Chain[i];
            int kept = 0;
            for (float along = Margin + 0.2f; along <= Mathf.Min(SideFar, room.Length / 2f) && kept < SideKept; along += SideStep)
            {
                KwGearRiser? r = Beside(rooms, i, left, along, foot);
                if (r == null)
                    continue;
                found.Add(r.Value);
                (kept, along) = (kept + 1, along + SideApart - SideStep);
            }
        }

        // A side wall's place at a distance in from the back: as near the wall as it fits, if it does.
        private static KwGearRiser? Beside(KwGearRooms rooms, int i, bool left, float along, System.Func<float, float, float?> foot)
        {
            KwGearRoom room = rooms.Chain[i];
            float reach = room.Reach(left, along);
            for (float inward = Margin; inward <= Margin + SideIn + 1e-3f; inward += Step)
            {
                Vector3 p = room.Point(along, (left ? 1f : -1f) * (reach - inward), 0f);
                float? z = foot(p.x, p.y);
                if (z != null && Climbs(rooms, i, p.x, p.y, z.Value))
                    return new KwGearRiser(new Vector3(p.x, p.y, z.Value), room.Top - KwGearTrain.Below, room.SideWay(left), 10f + along + inward);
            }
            return null;
        }

        /// <summary>
        /// Whether the shaft can climb at a place from a foot to under cabin i's roof: its foot's bevel box inside the hull
        /// (under the deck) or under the roof below, clear all the way up (through the floors between, which its roof
        /// covers), its head's bevel box clear, nothing standing in its way.
        /// </summary>
        public static bool Climbs(KwGearRooms rooms, int i, float x, float y, float foot)
        {
            KwGearRoom room = rooms.Chain[i];
            float head = room.Top - KwGearTrain.Below;
            if (i > 0 ? !rooms.Chain[i - 1].Covers(x, y) : !rooms.Inboard(new Vector3(x, y, foot)))
                return false;
            for (float z = foot + Margin; z < head; z += 0.25f)
            {
                if (!rooms.Clear(new Vector3(x, y, z), Margin, true))
                    return false;
            }
            return rooms.Clear(new Vector3(x, y, head), KwGearCeiling.Box) && !rooms.Blocked(new Vector3(x, y, foot), new Vector3(x, y, head));
        }

        // The places found so far with one more, best first, no more than kept.
        private static void Keep(List<KwGearRiser> found, KwGearRiser one)
        {
            found.Add(one);
            found.Sort((a, b) => a.Score.CompareTo(b.Score));
            if (found.Count > Kept)
                found.RemoveAt(found.Count - 1);
        }

        /// <summary>The brackets holding a climb in cabin i to its wall: evenly from over its floor to under its head's box, each reaching the wall.</summary>
        public static void Brackets(KwGearRooms rooms, int i, KwGearRiser riser, List<KwGearBracket> into)
        {
            KwGearRoom room = rooms.Chain[i];
            float low = room.Floor + 0.3f, high = riser.HeadZ - KwRudderGearRoute.Lead - 0.03f;
            if (high < low)
                return;
            int gaps = high - low < 0.4f ? 0 : Mathf.Max(1, Mathf.CeilToInt((high - low) / Spacing));
            for (int k = 0; k <= gaps; k++)
            {
                float z = gaps == 0 ? (low + high) / 2f : Mathf.Lerp(low, high, k / (float)gaps);
                foreach (float shift in new[] { 0f, 0.15f, -0.15f, 0.3f, -0.3f })
                {
                    Vector3 at = new Vector3(riser.Foot.x, riser.Foot.y, z + shift);
                    Vector2? reach = at.z >= low - 0.15f && at.z <= high ? Reaching(rooms, room, at, riser.Wall) : null;
                    if (reach == null)
                        continue;
                    into.Add(new KwGearBracket(at, reach.Value));
                    break;
                }
            }
        }

        // A bracket's reach from a point on the shaft: square onto the nearest face on the side of the wall it is held to (the
        // shortest reach within a quarter turn either way of it, the way nearest the wall's on a tie, so a pointed stern's
        // slanting planking or a post is met square and its plate lies flat); null with none in reach whose plate lies flat.
        private static Vector2? Reaching(KwGearRooms rooms, KwGearRoom room, Vector3 at, Vector3 wall)
        {
            (Vector3 Way, float D)? best = null;
            for (int k = 0; k <= 2 * Mathf.RoundToInt(90f / Turn); k++)
            {
                float a = (k % 2 == 0 ? k / 2 : -(k + 1) / 2) * Turn * Mathf.Deg2Rad;
                Vector3 way = new Vector3(wall.x * Mathf.Cos(a) - wall.y * Mathf.Sin(a), wall.x * Mathf.Sin(a) + wall.y * Mathf.Cos(a), 0f);
                float? d = ToWall(rooms, room, at, way);
                if (d != null && (best == null || d.Value < best.Value.D))
                    best = (way, d.Value);
            }
            return best != null && Seated(room, at, best.Value.Way, best.Value.D) ? new Vector2(best.Value.Way.x, best.Value.Way.y) * best.Value.D : (Vector2?)null;
        }

        // Whether a bracket's plate at a reach along a way lies flat on the wall: its four corners just off the face all in the room.
        private static bool Seated(KwGearRoom room, Vector3 at, Vector3 way, float d)
        {
            Vector3 side = new Vector3(-way.y, way.x, 0f) * KwGearFittings.BracketPlate, up = new Vector3(0f, 0f, KwGearFittings.BracketPlate);
            Vector3 face = at + way * (d - 0.004f);
            foreach (Vector3 corner in new[] { face + side + up, face + side - up, face - side + up, face - side - up })
            {
                if (!room.Free(corner, 0f))
                    return false;
            }
            return true;
        }

        // How far from a point on the shaft the nearest wall is along a way (to a few millimetres); null with none within
        // reach, or with a doorway's or a window's room in the way.
        private static float? ToWall(KwGearRooms rooms, KwGearRoom room, Vector3 at, Vector3 way)
        {
            for (float d = KwGearTrain.Shaft + 0.02f; d <= Reach; d += 0.02f)
            {
                if (rooms.ByOpening(at + way * d, KwGearFittings.BracketPlate))
                    return null;
                if (room.Free(at + way * d, 0f))
                    continue;
                float inside = d - 0.02f;
                for (int i = 0; i < 4; i++)
                    (inside, d) = room.Free(at + way * ((inside + d) / 2f), 0f) ? ((inside + d) / 2f, d) : (inside, (inside + d) / 2f);
                return inside;
            }
            return null;
        }
    }
}
