using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's ironwork (in its metal look), drawn in kit axes (<see cref="KwShipSpec"/>). On the carriage (the yaw
    /// part): the cap-squares, a strap over each trunnion with a flat on the bracket's head either side and a bolt strap
    /// down its outer face fore and aft; bolt heads on the brackets' outer faces where they sit on the axletrees; an
    /// eyebolt on each bracket's step beside the breech's ring that the breeching rope runs through, one low on each
    /// bracket's front for the side tackles and one on the rear transom for the train tackle; a washer and a linchpin on
    /// each axle arm outside its truck. On each truck (turning with it): three bolt heads round its outer face. On the gun (the pitch part's barrel,
    /// kicking back with it): a lug on each side of the breech with the ring the breeching is made fast to. The piece's
    /// own model: a ringbolt on a deck plate ahead of each bracket, out beyond its trucks.
    /// </summary>
    public static class KwShipIrons
    {
        private static readonly float[] Sides = { 1f, -1f };
        private static readonly Vector3 Fore = new Vector3(1f, 0f, 0f), Across = new Vector3(0f, 1f, 0f);

        /// <summary>An axle arm's radius, by the trucks' width.</summary>
        public static float Arm(KwShipSpec ship) => ship.Width * 0.42f;

        /// <summary>The carriage's ironwork, on the yaw part.</summary>
        public static void Fittings(MeshBuilder b, KwShipSpec ship)
        {
            CapSquares(b, ship);
            foreach (float side in Sides)
            {
                Bolts(b, ship, side);
                Eyebolt(b, ship.Eye(side), side, ship.Outer, Fore, 0.028f);
                Eyebolt(b, new Vector3(ship.BracketFront - 0.1f, side * (ship.Outer + 0.03f), 2f * ship.Front + 0.05f), side, ship.Outer, Fore, 0.024f);
                Linchpin(b, ship, ship.FrontAxle, ship.Front, side);
                Linchpin(b, ship, ship.RearAxle, ship.Rear, side);
            }
            float rear = ship.BracketRear, low = (ship.Bottom(rear) + ship.BedTop - 0.05f) / 2f;
            Solid.Log(b, new Vector3(rear + 0.03f, 0f, low), new Vector3(rear - 0.03f, 0f, low), 0.008f, 6);
            KwRound.Ring(b, new Vector3(rear - 0.05f, 0f, low), Across, 0.024f, 0.007f, 10, 4);
        }

        /// <summary>A lug on each side of the breech's underside ring, with the ring the breeching is made fast to (on the gun, from the trunnions).</summary>
        public static void Lugs(MeshBuilder b, KwShipSpec ship)
        {
            Vector2 under = ship.Under;
            foreach (float side in Sides)
            {
                float y0 = side * (under.y - 0.01f), y1 = side * (under.y + 0.03f);
                Solid.Box(b, new Vector3(under.x - 0.018f, Mathf.Min(y0, y1), -0.012f), new Vector3(under.x + 0.018f, Mathf.Max(y0, y1), 0.012f));
                KwRound.Ring(b, ship.Ring(side), Fore, 0.024f, 0.007f, 10, 4);
            }
        }

        /// <summary>The two ringbolts ahead of the brackets: a plate on the deck, a bolt up from it and a standing ring facing the gun.</summary>
        public static void Ringbolts(MeshBuilder b, KwShipSpec ship)
        {
            foreach (float side in Sides)
            {
                Vector3 ring = ship.Anchor(side);
                Solid.Box(b, new Vector3(ring.x - 0.06f, ring.y - 0.05f, 0f), new Vector3(ring.x + 0.06f, ring.y + 0.05f, 0.012f));
                KwRound.Ring(b, ring, Fore, 0.032f, 0.009f, 12, 4);
            }
        }

        /// <summary>A truck's three bolt heads round its outer face (left +1, right -1), about its hub, turning with it.</summary>
        public static void TruckBolts(MeshBuilder b, float radius, float width, float side)
        {
            float face = side * (width / 2f), out0 = face, out1 = face + side * 0.008f;
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 30f) * Mathf.Deg2Rad;
                Vector3 at = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (radius * 0.55f);
                Solid.Box(b, new Vector3(at.x - 0.016f, Mathf.Min(out0, out1), at.z - 0.016f), new Vector3(at.x + 0.016f, Mathf.Max(out0, out1), at.z + 0.016f));
            }
        }

        // Over each trunnion (bedded a little into the bracket's head): the strap, its flats on the head fore and aft, and a
        // bolt strap down the outer face at each end.
        private static void CapSquares(MeshBuilder b, KwShipSpec ship)
        {
            KwCheeks c = ship.Cheeks;
            Vector3 at = ship.Trunnions;
            KwCannonParts.CapSquares(b, at, (c.Inner + c.Outer) / 2f, c.Strap, c.Tube);
            foreach (float side in Sides)
            {
                float y0 = Mathf.Min(side * (c.Inner + 0.006f), side * (c.Outer - 0.006f)), y1 = Mathf.Max(side * (c.Inner + 0.006f), side * (c.Outer - 0.006f));
                float face = side * (c.Outer + 0.008f), inside = side * c.Outer;
                foreach (float end in Sides)
                {
                    float x = at.x + end * (c.Strap + 0.03f);
                    Solid.Box(b, new Vector3(x - 0.032f, y0, ship.HeadTop - 0.004f), new Vector3(x + 0.032f, y1, ship.HeadTop + c.Tube));
                    Solid.Box(b, new Vector3(x - 0.014f, Mathf.Min(face, inside), ship.HeadTop - 0.13f), new Vector3(x + 0.014f, Mathf.Max(face, inside), ship.HeadTop + 0.004f));
                }
            }
        }

        // Bolt heads on a bracket's outer face over each axletree (above its trucks), two to an axletree.
        private static void Bolts(MeshBuilder b, KwShipSpec ship, float side)
        {
            float face = side * (ship.Outer + 0.01f), inside = side * ship.Outer;
            foreach ((float x, float hub) in new[] { (ship.FrontAxle, ship.Front), (ship.RearAxle, ship.Rear) })
            {
                foreach (float d in Sides)
                {
                    float bx = x + d * ship.TreeHalf * 0.5f, z = 2f * hub + 0.045f;
                    Solid.Box(b, new Vector3(bx - 0.016f, Mathf.Min(face, inside), z - 0.016f), new Vector3(bx + 0.016f, Mathf.Max(face, inside), z + 0.016f));
                }
            }
        }

        // An eyebolt standing out of a bracket's outer face: its shank and its ring (so wide, its hole along the axis).
        private static void Eyebolt(MeshBuilder b, Vector3 ring, float side, float face, Vector3 axis, float radius)
        {
            Solid.Log(b, new Vector3(ring.x, side * (face - 0.02f), ring.z), new Vector3(ring.x, ring.y - side * radius, ring.z), 0.008f, 6);
            KwRound.Ring(b, ring, axis, radius, 0.007f, 10, 4);
        }

        // An axle arm's washer outside its truck and the linchpin through the arm beyond it.
        private static void Linchpin(MeshBuilder b, KwShipSpec ship, float x, float hub, float side)
        {
            float arm = Arm(ship), y0 = side * (ship.TruckOut + 0.002f), y1 = side * (ship.TruckOut + 0.012f), pin = side * (ship.TruckOut + 0.022f);
            Solid.Log(b, new Vector3(x, y0, hub), new Vector3(x, y1, hub), arm * 1.6f, 10);
            Solid.Box(b, new Vector3(x - 0.008f, pin - 0.006f, hub - arm * 1.7f), new Vector3(x + 0.008f, pin + 0.006f, hub + arm * 1.7f));
        }
    }
}
