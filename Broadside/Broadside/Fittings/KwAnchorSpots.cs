using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Every place within <see cref="Reach"/> of a windlass where its cable could go overboard (<see cref="KwAnchorSpot"/>),
    /// nearest first, from the ship's own parts (<see cref="KwAnchorShip"/>): every <see cref="Step"/> along each planking
    /// part's sheer on each side and snug either side of each railing post (a chock on its railing's cap between two posts,
    /// or on the planking's top edge), bow and stern included as far as the hull is <see cref="Narrowest"/> wide each side,
    /// and across a flat stern's transom; or, for a windlass in a cabin or with no chock clear, a hawse pipe through the
    /// hull's side (or a cabin's raised side) at the drum's height. Never at a rib, on a railing's post, where the hung
    /// anchor would cover an opening cut in that side (or an opened cover's swing), or where a cabin's raised side stands on
    /// the sheer. Beside a shield hook a chock counts a little more (<see cref="EmptyHook"/>, a shield hung there later
    /// would hang over the cable), and beside a hook with a shield hung on it much more (<see cref="HookCost"/>).
    /// </summary>
    public static class KwAnchorSpots
    {
        /// <summary>How far a fairlead may be from the drum; what a chock beside an empty shield hook, and beside a hook with
        /// a shield hung on it, counts for in metres of cable.</summary>
        public const float Reach = 12f, EmptyHook = 0.25f, HookCost = 1.5f;

        /// <summary>How far a chock's middle stays from a post's: its seat's end clear of the post's side.</summary>
        public const float PostGap = KwAnchorLeadShapes.SeatLong + KwRailing.Post + 0.01f;

        // The step along the sheer; the narrowest half width; how far from a shield hook, a rib, an opening along the side;
        // how far under a fairlead the hung anchor reaches, and the room kept from an opening's frame; the corners and the
        // middle of a transom left out; how high over the windlass's deck a hawse pipe goes, and how far under the sheer or a
        // cabin's roof it must be.
        private const float Step = 0.1f, Narrowest = 0.4f, HookGap = 0.55f, RibGap = 0.17f, OpeningGap = 0.45f;
        private const float Hangs = KwAnchorHang.Drop + KwAnchorShapes.Length - KwAnchorSpot.Rise, FrameRoom = 0.17f;
        private const float Corner = 0.35f, Middle = 0.6f, PipeHigh = 0.5f, PipeRoom = 0.22f;

        /// <summary>The chocks over the sheer and across flat sterns, nearest first.</summary>
        public static List<KwAnchorSpot> Chocks(KwAnchorShip ship)
        {
            List<KwAnchorSpot> spots = new List<KwAnchorSpot>();
            foreach (KwAnchorHull hull in ship.Hulls)
            {
                foreach (float side in new[] { 1f, -1f })
                    Sheer(ship, hull, side, spots);
                foreach (bool far in new[] { false, true })
                    Transom(hull, far, spots);
            }
            return Nearest(spots);
        }

        /// <summary>The hawse pipes through the sides at the drum's height (every second step), nearest first.</summary>
        public static List<KwAnchorSpot> Pipes(KwAnchorShip ship)
        {
            List<KwAnchorSpot> spots = new List<KwAnchorSpot>();
            foreach (KwAnchorHull hull in ship.Hulls)
            {
                float z = hull.Pose.Local(new Vector3(0f, 0f, PipeHigh)).z;
                foreach (float side in new[] { 1f, -1f })
                {
                    for (float x = hull.From + Step; x <= hull.To - Step; x += 2f * Step)
                    {
                        KwAnchorSpot? spot = Free(ship, hull, side, x, z) ? Pipe(ship, hull, side, x, z) : null;
                        if (spot != null)
                            spots.Add(spot);
                    }
                }
            }
            return Nearest(spots);
        }

        private static List<KwAnchorSpot> Nearest(List<KwAnchorSpot> spots)
        {
            spots.RemoveAll(s => s.Estimate > Reach);
            spots.Sort((a, b) => a.Least.CompareTo(b.Least));
            return spots;
        }

        // A chock along one side's sheer wherever one may stand: every step, and snug beside each post.
        private static void Sheer(KwAnchorShip ship, KwAnchorHull hull, float side, List<KwAnchorSpot> spots)
        {
            foreach (float x in Places(ship, hull, side))
            {
                if (ChockAt(ship, hull, side, x) is KwAnchorSpot spot)
                    spots.Add(spot);
            }
        }

        // The places along a side's sheer tried: every step, and just clear of each railing post standing on that side.
        private static List<float> Places(KwAnchorShip ship, KwAnchorHull hull, float side)
        {
            List<float> places = new List<float>();
            for (float x = hull.From + Step; x <= hull.To - Step; x += Step)
                places.Add(x);
            foreach (KwAnchorRail rail in ship.Rails)
            {
                foreach (Vector3 post in rail.Posts())
                {
                    Vector3 k = hull.Pose.Local(post);
                    foreach (float x in new[] { k.x - PostGap - 0.002f, k.x + PostGap + 0.002f })
                    {
                        if (Mathf.Sign(k.y) == side && x > hull.From + Step / 2f && x < hull.To - Step / 2f)
                            places.Add(x);
                    }
                }
            }
            return Apart(places);
        }

        // The places in order along the side, one of any two within a centimetre left out (two railings' end posts at a joint).
        private static List<float> Apart(List<float> places)
        {
            places.Sort();
            List<float> apart = new List<float>(places.Count);
            foreach (float x in places)
            {
                if (apart.Count == 0 || x - apart[apart.Count - 1] > 0.01f)
                    apart.Add(x);
            }
            return apart;
        }

        // A chock at x on one side's sheer, if one may stand there, with what the hooks beside it count for.
        private static KwAnchorSpot? ChockAt(KwAnchorShip ship, KwAnchorHull hull, float side, float x)
        {
            Vector2 e = hull.Sheer(x);
            if (e.x < Narrowest || !Free(ship, hull, side, x, e.y))
                return null;
            Vector3 sheer = hull.Pose.Out(new Vector3(x, side * e.x, e.y));
            if (ship.Cabins.Exists(c => c.Raises(sheer)))
                return null;
            KwAnchorRail? rail = Railing(ship, sheer, out float along, out float on);
            if (rail != null && rail.Near(along, PostGap))
                return null;
            KwAnchorSpot spot = Chock(hull, side, x, rail != null);
            spot.Penalty = rail != null ? rail.Hooked(along, on, HookGap, EmptyHook, HookCost) : 0f;
            return spot;
        }

        // A chock at x on one side: seated on the cap (railed) or the planking's top edge, square to the sheer, as high as
        // the sheer is under either lip (where the hull narrows the lips reach along the sheer, which may climb there).
        private static KwAnchorSpot Chock(KwAnchorHull hull, float side, float x, bool railed)
        {
            Vector2 e = hull.Sheer(x), n = new Vector2(-hull.Slope(x), side).normalized;
            float reach = (railed ? KwRailing.Half : KwPlanks.Thick) + KwAnchorSpot.Lip, top = e.y;
            foreach (float off in new[] { n.x * reach, -n.x * reach })
                top = Mathf.Max(top, hull.Sheer(Mathf.Clamp(x + off, hull.From, hull.To)).y);
            top += railed ? KwRailing.CapTop : 0f;
            Vector3 at = hull.Pose.Out(new Vector3(x, side * e.x, top)), outward = Level(hull.Pose.Way(new Vector3(n.x, n.y, 0f)));
            return railed ? new KwAnchorSpot(hull, false, true, at, outward, KwRailing.Half, KwRailing.Half)
                : new KwAnchorSpot(hull, false, false, at, outward, KwPlanks.Thick, KwPlanks.Lap);
        }

        // Chocks along a flat end's transom top, its corners and its middle (the post and the rudder) left out.
        private static void Transom(KwAnchorHull hull, bool far, List<KwAnchorSpot> spots)
        {
            if (!hull.Flat(far))
                return;
            Station end = hull.End(far);
            Vector2 e = end.Half[end.Half.Length - 1];
            Vector3 outward = Level(hull.Pose.Way(new Vector3(far ? 1f : -1f, 0f, 0f)));
            for (float y = -(e.x - Corner); y <= e.x - Corner; y += Step)
            {
                if (Mathf.Abs(y) >= Middle)
                    spots.Add(new KwAnchorSpot(hull, false, false, hull.Pose.Out(new Vector3(end.X, y, e.y)), outward, KwPlanks.Thick, KwPlanks.Lap));
            }
        }

        // A hawse pipe at x on one side at height z (the part's kit): through the planking under the sheer, or through a
        // cabin's raised side over it; none where neither has room for it. It spans the wall as the planking has it and as
        // a cabin standing there has it (a cabin's own frame may lie a hair off the planking's).
        private static KwAnchorSpot? Pipe(KwAnchorShip ship, KwAnchorHull hull, float side, float x, float z)
        {
            Vector2 e = hull.Sheer(x);
            Vector3 sheer = hull.Pose.Out(new Vector3(x, side * e.x, e.y));
            bool low = z < e.y - PipeRoom, raised = !low && ship.Cabins.Exists(c => c.Raises(sheer) && c.Height(sheer) + (z - e.y) < c.Top - PipeRoom);
            if (!low && !raised)
                return null;
            float inner = low ? hull.Inside(x, z) : e.x - KwPlanks.Thick, outer = low ? hull.Outside(x, z) : e.x + KwPlanks.Lap;
            Widen(ship, hull, hull.Pose.Out(new Vector3(x, side * (inner + outer) / 2f, z)), ref inner, ref outer);
            if (inner <= 0.2f)
                return null;
            float slope = low ? (hull.Outside(x + 0.05f, z) - hull.Outside(x - 0.05f, z)) / 0.1f : hull.Slope(x);
            Vector2 n = new Vector2(-slope, side).normalized;
            Vector3 at = hull.Pose.Out(new Vector3(x, side * (inner + outer) / 2f, z)), outward = Level(hull.Pose.Way(new Vector3(n.x, n.y, 0f)));
            return new KwAnchorSpot(hull, true, false, at, outward, (outer - inner) / 2f, (outer - inner) / 2f);
        }

        // The wall's faces (half widths in the part's kit) widened to a cabin's own faces where it stands at the hull there.
        private static void Widen(KwAnchorShip ship, KwAnchorHull hull, Vector3 middle, ref float inner, ref float outer)
        {
            foreach (KwAnchorCabin cabin in ship.Cabins)
            {
                if (cabin.Faces(middle) is (Vector3 a, Vector3 b))
                {
                    inner = Mathf.Min(inner, Mathf.Abs(hull.Pose.Local(a).y));
                    outer = Mathf.Max(outer, Mathf.Abs(hull.Pose.Local(b).y));
                }
            }
        }

        // Clear of the ribs, and of the openings cut in that side that a fairlead at x and height top would meet.
        private static bool Free(KwAnchorShip ship, KwAnchorHull hull, float side, float x, float top)
        {
            foreach (Vector3 rib in ship.Ribs)
            {
                if (Mathf.Abs(hull.Pose.Local(rib).x - x) < RibGap)
                    return false;
            }
            string wall = side > 0f ? KwOpening.Port : KwOpening.Starboard;
            return !hull.Openings.Exists(o => o.Wall == wall && Meets(o, x, top));
        }

        // Whether an opening (its frame, shutters opened out to its sides, a lid flapped up over it) comes within reach of a
        // fairlead at x and height top (the part's kit): along the side, and up to the anchor hung under it or the fairlead.
        private static bool Meets(KwOpening o, float x, float top)
        {
            float wide = o.Cover == KwOpening.Shutters ? o.Width / 2f : 0f, flap = o.Cover == KwOpening.Up || o.Cover == KwOpening.Lid ? o.Height : 0f;
            bool along = x > o.U0 - wide - OpeningGap && x < o.U1 + wide + OpeningGap;
            return along && o.Top + flap + FrameRoom > top - Hangs && o.Sill - FrameRoom < top + FrameRoom;
        }

        // The railing standing on the sheer at a point, its place along it and its side.
        private static KwAnchorRail? Railing(KwAnchorShip ship, Vector3 sheer, out float along, out float side)
        {
            (along, side) = (0f, 0f);
            foreach (KwAnchorRail rail in ship.Rails)
            {
                if (rail.Covers(sheer, out along, out side))
                    return rail;
            }
            return null;
        }

        private static Vector3 Level(Vector3 way) => new Vector3(way.x, way.y, 0f).normalized;
    }
}
