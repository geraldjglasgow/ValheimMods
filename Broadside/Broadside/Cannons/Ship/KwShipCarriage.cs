using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's truck carriage, drawn in kit axes (<see cref="KwShipSpec"/>). The whole carriage is the yaw part
    /// (from its pivot at the origin, under the carriage's middle), since a truck carriage is levered round on its trucks
    /// rather than turned on a plate, and the yaw part is what rolls back at a shot (<see cref="KwShipRecoil"/>). In the
    /// carriage's timber: two long low brackets, each a long head at the trunnions (its front corner taken off) and its
    /// steps down to the rear, cut into lengths at every step and along an arch cut up under it between the axletrees it
    /// sits on; a front transom between them under the chase, a rear transom under the stool bed; the front and rear
    /// axletrees across under the brackets, with round arms out through the trucks; the stool bed between the brackets
    /// behind the trunnions, on a block on the rear axletree. Its metal is <see cref="KwShipIrons"/>'s. Drawn apart, each
    /// about its own point, as parts of their own: the four trucks (solid wooden discs about their hubs, turned as the
    /// carriage rolls, <see cref="KwWheels"/>) and the quoin (a wedge with a handle at its thick end, about its thin front
    /// end's foot, slid along the bed under the breech, <see cref="KwQuoin"/>).
    /// </summary>
    public static class KwShipCarriage
    {
        private static readonly float[] Sides = { 1f, -1f };

        /// <summary>A ship cannon's carriage parts (its gun is the cannon shape's): the timber and the metal, both on the yaw part.</summary>
        public static void Add(KwArmDef arm, KwShipSpec ship, KwCheeks cheeks)
        {
            ship.Cheeks = cheeks;
            arm.Part(KwJoint.Yaw, ship.Wood, b => Timber(b, ship)).Part(KwJoint.Yaw, ship.Metal, b => KwShipIrons.Fittings(b, ship));
        }

        /// <summary>The breeching ring on each side of the breech, on the gun itself (so it kicks back with the barrel); after the gun's shape has made its barrel.</summary>
        public static void Breeching(KwArmDef arm, KwShipSpec ship) =>
            KwArmMotionBuild.Find(arm, "gun")?.Part(ship.Metal, b => KwShipIrons.Lugs(b, ship));

        /// <summary>The piece's model: the two ringbolts on their deck plates ahead of the brackets, which the breeching is made fast to.</summary>
        public static void Model(MeshBuilder b, KwShipSpec ship) => KwShipIrons.Ringbolts(b, ship);

        /// <summary>A truck about its hub (its axle along y): a solid wooden disc.</summary>
        public static void Truck(MeshBuilder b, float radius, float width) =>
            Solid.Log(b, new Vector3(0f, -width / 2f, 0f), new Vector3(0f, width / 2f, 0f), radius, 14);

        /// <summary>The quoin about its thin front end's foot (x 0, on the bed): a wedge rising to the rear, a round handle out of its thick end.</summary>
        public static void Quoin(MeshBuilder b, KwShipSpec ship)
        {
            float l = ship.QuoinLength, q = ship.Inner - 0.04f, thick = KwShipSpec.Tip + ship.Slope * l;
            Solid.Hexa(b, new[]
            {
                new Vector3(-l, -q, 0f), new Vector3(0f, -q, 0f), new Vector3(0f, q, 0f), new Vector3(-l, q, 0f),
                new Vector3(-l, -q, thick), new Vector3(0f, -q, KwShipSpec.Tip), new Vector3(0f, q, KwShipSpec.Tip), new Vector3(-l, q, thick),
            });
            Solid.Log(b, new Vector3(-l + 0.01f, 0f, thick * 0.55f), new Vector3(-l - 0.07f, 0f, thick * 0.55f), 0.022f, 8);
        }

        private static void Timber(MeshBuilder b, KwShipSpec ship)
        {
            IReadOnlyList<float> cuts = ship.Cuts();
            foreach (float side in Sides)
            {
                for (int i = 0; i + 1 < cuts.Count; i++)
                    Length(b, ship, side, cuts[i], cuts[i + 1]);
            }
            Transoms(b, ship);
            Axletree(b, ship, ship.FrontAxle, ship.Front);
            Axletree(b, ship, ship.RearAxle, ship.Rear);
            Bed(b, ship);
        }

        // One length of a bracket on one side, from x0 to x1: its top at each end (a step's, or the head's corner) over
        // its underside there (on an axletree, or up in the arch).
        private static void Length(MeshBuilder b, KwShipSpec ship, float side, float x0, float x1)
        {
            float y0 = Mathf.Min(side * ship.Inner, side * ship.Outer), y1 = Mathf.Max(side * ship.Inner, side * ship.Outer);
            float b0 = ship.Bottom(x0), b1 = ship.Bottom(x1);
            (float rear, float front) = ship.TopOver(x0, x1);
            Solid.Hexa(b, new[]
            {
                new Vector3(x0, y0, b0), new Vector3(x1, y0, b1), new Vector3(x1, y1, b1), new Vector3(x0, y1, b0),
                new Vector3(x0, y0, rear), new Vector3(x1, y0, front), new Vector3(x1, y1, front), new Vector3(x0, y1, rear),
            });
        }

        // The front transom under the chase (clear of the barrel at full depression), the rear one under the stool bed.
        private static void Transoms(MeshBuilder b, KwShipSpec ship)
        {
            float front = ship.BracketFront - 0.02f, rear = ship.BracketRear, i = ship.Inner, deep = ship.Size * 0.28f;
            Solid.Box(b, new Vector3(front - deep, -i, ship.Bottom(front) + 0.01f), new Vector3(front, i, ship.RearTop - 0.02f));
            Solid.Box(b, new Vector3(rear, -i, ship.Bottom(rear) + 0.01f), new Vector3(rear + deep * 0.9f, i, ship.BedTop - 0.05f));
        }

        // An axletree across under both brackets at its axle, and its two round arms out through the trucks.
        private static void Axletree(MeshBuilder b, KwShipSpec ship, float x, float hub)
        {
            float half = ship.Outer + 0.005f, d = ship.TreeHalf;
            Solid.Box(b, new Vector3(x - d, -half, hub - ship.TreeBottom), new Vector3(x + d, half, hub + ship.TreeTop));
            foreach (float side in Sides)
                Solid.Log(b, new Vector3(x, side * ship.Outer, hub), new Vector3(x, side * (ship.TruckOut + 0.03f), hub), KwShipIrons.Arm(ship), 10);
        }

        // The stool bed between the brackets from just behind them to where the quoin is pushed right in, and the block
        // under it on the rear axletree (none when the bed lies on the axletree itself).
        private static void Bed(MeshBuilder b, KwShipSpec ship)
        {
            float top = ship.BedTop, i = ship.Inner - 0.004f, d = ship.TreeHalf - 0.01f, tree = ship.Rear + ship.TreeTop;
            Solid.Box(b, new Vector3(ship.BracketRear - 0.04f, -i, top - 0.05f), new Vector3(ship.QuoinIn + 0.04f, i, top));
            if (top - 0.05f > tree + 0.005f)
                Solid.Box(b, new Vector3(ship.RearAxle - d, -(i - 0.04f), tree), new Vector3(ship.RearAxle + d, i - 0.04f, top - 0.05f));
        }
    }
}
