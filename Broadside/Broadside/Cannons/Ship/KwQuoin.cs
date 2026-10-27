using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship cannon's quoin, on every peer that draws (<see cref="KwShipRecoil"/>) and once on the bench: the wedge on the
    /// stool bed under the breech, slid along it so its sloping top bears on the breech's underside wherever the gun is
    /// tilted, pushed in as the gun is depressed and drawn out as it is elevated, within its travel
    /// (<see cref="KwShipSpec.QuoinOut"/> to <see cref="KwShipSpec.QuoinIn"/>). The underside is a few points riding on the
    /// barrel (<see cref="KwShipSpec.Bears"/>, from the ring it bears on back to the breech's back edge), so it follows the
    /// barrel's kick too; the wedge goes as far in as it can without its top rising above any of them that lie over it. All
    /// on the carriage (the yaw part), so it rolls back with it at a shot.
    /// </summary>
    public sealed class KwQuoin
    {
        public const string Name = "quoin", Bears = "quoin_bears_";

        private readonly KwShipSpec ship;
        private readonly Transform node, carriage;
        private readonly Transform[] bears;
        private float placed = float.NaN;

        private KwQuoin(KwShipSpec ship, Transform node, Transform carriage, Transform[] bears) =>
            (this.ship, this.node, this.carriage, this.bears) = (ship, node, carriage, bears);

        /// <summary>A cannon's quoin and the points it bears on, found by name under it; null when one is missing.</summary>
        public static KwQuoin? Of(Transform root, KwShipSpec ship)
        {
            Transform? yaw = root.Find(KwArm.YawName), node = yaw != null ? yaw.Find(Name) : null;
            Transform? gun = KwShipRig.Gun(root);
            if (yaw == null || node == null || gun == null)
                return null;
            Transform[] bears = new Transform[ship.Bears.Length];
            for (int i = 0; i < bears.Length; i++)
            {
                Transform? point = gun.Find(Bears + i);
                if (point == null)
                    return null;
                bears[i] = point;
            }
            return new KwQuoin(ship, node, yaw, bears);
        }

        /// <summary>Slides the quoin under the breech as it is now; moved only when it moves.</summary>
        public void Place()
        {
            float front = Front();
            if (Mathf.Abs(front - placed) < 0.0005f)
                return;
            placed = front;
            node.localPosition = KwMesh.ToUnity(new Vector3(front, 0f, ship.BedTop));
        }

        // The front end as far in as the first point allows, then no further than any other point over the wedge allows.
        private float Front()
        {
            float first = Bound(Point(bears[0])), front = first;
            for (int i = 1; i < bears.Length; i++)
            {
                Vector3 p = Point(bears[i]);
                if (p.x >= first - ship.QuoinLength && p.x <= first + 0.01f)
                    front = Mathf.Min(front, Bound(p));
            }
            return Mathf.Clamp(front, ship.QuoinOut - 0.12f, ship.QuoinIn);
        }

        // How far forward the front end may come before the top rises above a point: the top climbs the slope from the
        // thin end's height at the front end, so at the point it stands (front - x) * slope above that.
        private float Bound(Vector3 p) => p.x + (p.z - ship.BedTop - KwShipSpec.Tip) / ship.Slope;

        private Vector3 Point(Transform t) => KwMesh.ToKit(carriage.InverseTransformPoint(t.position));
    }
}
