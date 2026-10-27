using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the local player aims on a launched ship, in the ship's own frame (the blueprint's: every piece's place is in
    /// it), for the tools at sea (<see cref="KwSeaTools"/>) and the hammer's remove (<see cref="KwSeaRemove"/>): the camera's
    /// ray, the first of the ship's colliders it meets (the placement mask, or the remove mask) with its name and tag, and
    /// the nearest of its rig's hit colliders (the "hitbox" layer, which the building rays pass) before that. The colliders
    /// stand where the ship's body is, so the frame is the body's pose (as <see cref="KwShipRails"/> takes it); ghosts are
    /// drawn under the ship's visual holder and ride it.
    /// </summary>
    public sealed class KwSeaAim
    {
        private const float Reach = 50f;
        private static int rigMask = -1;

        public KwSeaShip Ship { get; private set; } = null!;

        /// <summary>The camera's ray in the ship's frame; Way is a unit vector.</summary>
        public Vector3 From { get; private set; }
        public Vector3 Way { get; private set; }

        /// <summary>Whether the ray met one of the ship's own colliders (else only its rig's).</summary>
        public bool Hit { get; private set; }
        public Vector3 Point { get; private set; }
        public Vector3 Normal { get; private set; }

        /// <summary>From the camera to the point (or to the rig's point), metres.</summary>
        public float Distance { get; private set; }
        public Collider? Collider { get; private set; }

        /// <summary>The collider's name: "hull", "deck", "cabin_roof", "rail", ...; "" for none.</summary>
        public string Name { get; private set; } = "";

        /// <summary>The collider's tag for shots (a railing's stretch: kind, piece, part), if it has one.</summary>
        public KwShipHitPart? Tag { get; private set; }

        /// <summary>The nearest of the rig's hit colliders the ray meets before the point, and where (the ship's frame).</summary>
        public KwShipHitPart? Rig { get; private set; }
        public Vector3 RigPoint { get; private set; }

        /// <summary>The camera's eye in the world.</summary>
        public Vector3 Eye { get; private set; }

        /// <summary>The aim with the placement mask; null when the ray meets no launched ship within 50 m.</summary>
        public static KwSeaAim? Of(Player player) => Of(player, player.m_placeRayMask);

        /// <summary>The aim with a mask of its own (the hammer's remove mask).</summary>
        public static KwSeaAim? Of(Player player, int mask)
        {
            if (GameCamera.instance == null || player == null)
                return null;
            Transform eye = GameCamera.instance.transform;
            Ray ray = new Ray(eye.position, eye.forward);
            bool solid = Physics.Raycast(ray, out RaycastHit hit, Reach, mask) && hit.collider != null;
            KwSeaShip? ship = solid ? hit.collider!.GetComponentInParent<KwSeaShip>() : null;
            (KwShipHitPart? rig, RaycastHit rigHit) = Rigged(ray, solid ? hit.distance : Reach);
            KwSeaShip? rigged = rig != null ? rig.GetComponentInParent<KwSeaShip>() : null;
            if (ship == null && rigged == null)
                return null;
            ship ??= rigged!;
            KwSeaAim aim = new KwSeaAim { Ship = ship, Eye = ray.origin, From = ship.Local(ray.origin), Way = ship.LocalWay(ray.direction).normalized };
            if (solid && hit.collider!.GetComponentInParent<KwSeaShip>() == ship)
                aim.Take(hit);
            if (rigged == ship)
                (aim.Rig, aim.RigPoint, aim.Distance) = (rig, ship.Local(rigHit.point), aim.Hit ? hit.distance : rigHit.distance);
            return aim;
        }

        // The solid hit, in the ship's frame.
        private void Take(RaycastHit hit)
        {
            (Hit, Point, Normal, Distance, Collider) = (true, Ship.Local(hit.point), Ship.LocalWay(hit.normal).normalized, hit.distance, hit.collider);
            Name = hit.collider.gameObject.name;
            Tag = hit.collider.GetComponentInParent<KwShipHitPart>();
        }

        // The nearest rig hit collider the ray meets before a distance.
        private static (KwShipHitPart?, RaycastHit) Rigged(Ray ray, float most)
        {
            if (rigMask < 0)
                rigMask = LayerMask.GetMask("hitbox");
            if (rigMask == 0 || !Physics.Raycast(ray, out RaycastHit hit, most, rigMask, QueryTriggerInteraction.Collide) || hit.collider == null)
                return (null, default);
            return (hit.collider.GetComponentInParent<KwShipHitPart>(), hit);
        }

        /// <summary>A point of the ship's frame in the world (as its body stands).</summary>
        public Vector3 World(Vector3 local) => Ship.World(local);

        /// <summary>A point of the world in the ship's frame.</summary>
        public Vector3 Local(Vector3 world) => Ship.Local(world);

        /// <summary>A turn in the ship's frame as a turn in the world (as it is drawn).</summary>
        public Quaternion WorldTurn(Quaternion local) => Ship.transform.rotation * local;

        /// <summary>The point on the ray a distance along it, in the ship's frame.</summary>
        public Vector3 Along(float distance) => From + Way * distance;

        /// <summary>Whether a point of the ship's frame is within the player's build reach of their eye.</summary>
        public bool Within(Player player, Vector3 local) => Vector3.Distance(player.m_eye.position, World(local)) <= player.m_maxPlaceDistance;
    }
}
