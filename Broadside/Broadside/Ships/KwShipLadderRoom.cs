using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The boarding ladder's checks (<see cref="KwShipLadderFind"/>), on the ship as it sees it
    /// (<see cref="KwShipLadderShip"/>, kit axes: x forward, y to port, z up): whether a ladder hanging at a place is in the
    /// way of something (an opening it would cover, the anchor's cable, a rudder, a fitting, a railing's shield hook or a
    /// post where it hooks over), and where a person stands at a point (the floor under it with room over it, inside no
    /// cabin but one the climb may go into).
    /// </summary>
    public static class KwShipLadderRoom
    {
        /// <summary>The ladder's half width, its rails and the hooks over the top.</summary>
        public const float Half = 0.3f;

        // An opening's margin beside the ladder; how far along the ladder keeps from where the anchor's cable leaves the ship
        // and from a rudder's post; from a shield hook's middle; a post may stand between its hooks (within PostIn of its
        // middle), else no nearer than PostOut. A person's radius and the heights over the floor their room is looked at.
        private const float HoleGap = 0.15f, AnchorGap = 1.4f, RudderGap = 1.6f, ShieldGap = 0.48f, PostIn = 0.12f, PostOut = 0.32f;
        private const float Body = 0.22f, Stride = 0.25f;
        private static readonly float[] Heights = { 0.35f, 0.95f, 1.55f };

        /// <summary>
        /// What a ladder at x along on a side, from its foot to its top, its line out at out from the centre line, would be
        /// in the way of (null: nothing): an opening it would cover, the anchor's cable, a rudder, a fitting outside the hull.
        /// </summary>
        public static string? Blocked(KwShipLadderShip ship, float x, float side, float foot, float top, float outward)
        {
            foreach (KwLadderHole hole in ship.Holes)
            {
                if (hole.Overlaps(x, Half + HoleGap, side, foot, top - 0.05f))
                    return "an opening";
            }
            foreach (Vector3 c in ship.Cable)
            {
                if (Mathf.Abs(c.x - x) < AnchorGap && (Mathf.Abs(c.y) < 0.3f || Mathf.Sign(c.y) == side))
                    return "the anchor's cable";
            }
            if (ship.Posts.Exists(post => Mathf.Abs(post.x - x) < RudderGap))
                return "a rudder";
            return Fitting(ship, x, side, foot, top, outward);
        }

        // A fitting where the ladder hangs (a keel's post, the bowsprit, a rudder, a name board): its rails' line sampled.
        private static string? Fitting(KwShipLadderShip ship, float x, float side, float foot, float top, float outward)
        {
            for (float z = foot; z <= top + 0.15f; z += 0.3f)
            {
                foreach (float along in new[] { -Half, 0f, Half })
                {
                    string? what = KwAnchorShip.Blocker(new Vector3(x + along, side * (outward + 0.05f), z), 0.1f, true, ship.Fixtures);
                    if (what != null)
                        return what;
                }
            }
            return null;
        }

        /// <summary>What on a hull railing stands where the ladder hooks over the sheer at a point (null: nothing): a shield hook near it, a post under a hook.</summary>
        public static string? Railing(KwShipLadderShip ship, Vector3 sheer)
        {
            foreach (KwAnchorRail rail in ship.Rig.Hull.Rails)
            {
                if (!rail.Covers(sheer, out float x, out float side))
                    continue;
                if (rail.Hooked(x, side, ShieldGap, 1f, 1f) > 0f)
                    return "a shield hook";
                if (rail.Near(x, PostOut) && !rail.Near(x, PostIn))
                    return "a railing post";
            }
            return null;
        }

        /// <summary>
        /// The floor a person stands on at a point (kit x, y): the highest deck under it no higher than a limit (a hold floor
        /// too when holds, a cabin's roof when roofs), with room over it up to a person's height: no fitting, wall, deck,
        /// railing or cabin in it, but a cabin the climb may go into (into) as long as it is in its room. Null with none.
        /// </summary>
        public static float? Stand(KwShipLadderShip ship, Vector3 at, float limit, System.Predicate<KwRigCabin>? into, bool holds, bool roofs)
        {
            float? floor = Floor(ship, at, limit, holds, roofs);
            if (floor == null || !Firm(ship, at, floor.Value, holds, roofs))
                return null;
            foreach (float up in Heights)
            {
                if (!Free(ship, new Vector3(at.x, at.y, floor.Value + up), into))
                    return null;
            }
            return floor;
        }

        private static float? Floor(KwShipLadderShip ship, Vector3 at, float limit, bool holds, bool roofs)
        {
            float? best = null;
            foreach ((KwAnchorDeck deck, bool hold) in ship.Walked)
            {
                if (holds || !hold)
                    best = Higher(best, deck.Top(at.x, at.y), limit);
            }
            foreach (KwRigCabin cabin in ship.Rig.Cabins)
            {
                if (roofs)
                    best = Higher(best, cabin.RoofTop(at), limit);
            }
            return best;
        }

        // The same floor a stride round the spot every way (not at a floor's edge or a hatch's opening).
        private static bool Firm(KwShipLadderShip ship, Vector3 at, float floor, bool holds, bool roofs)
        {
            foreach (Vector3 step in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down }) // kit x along, y across
            {
                float? there = Floor(ship, at + step * Stride, floor + 0.1f, holds, roofs);
                if (there == null || Mathf.Abs(there.Value - floor) > 0.06f)
                    return false;
            }
            return true;
        }

        private static float? Higher(float? best, float? top, float limit) =>
            top.HasValue && top.Value <= limit && (best == null || top.Value > best.Value) ? top : best;

        // Nothing at a point of a person's room: a fitting, the hull's side, a railing, a deck over it, a cabin (but in the
        // room of one let in).
        private static bool Free(KwShipLadderShip ship, Vector3 p, System.Predicate<KwRigCabin>? into)
        {
            KwAnchorShip hull = ship.Rig.Hull;
            if (KwAnchorShip.Blocker(p, Body, false, ship.Fixtures) != null || hull.Hulls.Exists(h => h.Blocks(p, 0.15f, false)))
                return false;
            if (hull.Rails.Exists(r => r.Blocks(p, Body, false)) || ship.Rig.RoofRails.Exists(r => r.Blocks(p, Body, false)))
                return false;
            if (ship.Walked.Exists(w => w.Deck.Blocks(p, 0.05f, false)))
                return false;
            List<KwRigCabin> cabins = ship.Rig.Cabins;
            for (int i = 0; i < cabins.Count; i++)
            {
                if (cabins[i].Blocks(p, 0f, false) && !(into != null && into(cabins[i]) && ship.Rooms[i].Inside(p, 0.15f)))
                    return false;
            }
            return true;
        }
    }
}
