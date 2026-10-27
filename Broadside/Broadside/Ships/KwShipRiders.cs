using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's riders: the hammer pieces built on it after it was launched (<see cref="KwShipRider"/>, each its
    /// own networked piece; the ship itself stays what its blueprint makes it). On every peer the ship keeps its riders as
    /// its children, so they follow it exactly, frame by frame, however it moves (interpolated or synced), and their
    /// colliders are part of its body: a player standing on one is carried as on the deck, and the hull's buoyancy never
    /// sees them, since the body's mass, centre of mass and turning (<see cref="KwShipHydro"/>, <see cref="KwShipCargo"/>)
    /// are put back as they were each time one comes or goes. A rider finds its ship by the ship's own id
    /// (<see cref="IdKey"/>, a random number the ship's owner writes once into its ZDO), since the game numbers its ZDOs
    /// afresh each time the world loads. Before the game lets go of the ship (unloaded, removed, broken, sunk) its riders
    /// are let go first (<see cref="KwShipRiderScene"/>), so they are never destroyed with it as its children.
    /// </summary>
    public sealed class KwShipRiders : MonoBehaviour
    {
        public const string IdKey = "kw_ship_uid";
        public static readonly int IdHash = IdKey.GetStableHashCode();
        private const float Again = 2f;

        private static readonly List<KwShipRiders> loaded = new List<KwShipRiders>();
        private static readonly HashSet<long> gone = new HashSet<long>();
        private readonly List<KwShipRider> riders = new List<KwShipRider>();
        private ZNetView? view;
        private Rigidbody? body;
        private float looked = -99f;

        /// <summary>Every launched ship this peer has in its scene now.</summary>
        public static IReadOnlyList<KwShipRiders> Loaded => loaded;

        /// <summary>The pieces riding this ship now, on this peer.</summary>
        public IReadOnlyList<KwShipRider> Riders => riders;

        /// <summary>The ship's ZDO; null once the game has let go of it (unloaded or destroyed, the object not yet gone).</summary>
        public ZDO? Zdo => view != null ? view.GetZDO() : null;

        /// <summary>The ship's own id (0 until its owner has written one).</summary>
        public long Id => Zdo?.GetLong(IdHash, 0L) ?? 0L;

        /// <summary>Whether a ship of this id was destroyed this session (its ZDO gone, not just unloaded).</summary>
        public static bool Gone(long id) => id != 0L && gone.Contains(id);

        /// <summary>The ship as the physics has it now (the pose a ray hits): its body's, else its transform's.</summary>
        public (Vector3 At, Quaternion Turn) Pose => body != null ? (body.position, body.rotation) : (transform.position, transform.rotation);

        /// <summary>
        /// A point and a normal a ray hit on the ship's colliders, where they are drawn: the body is interpolated (the
        /// game's ships' setting), so between physics steps the ship, its riders and the player aboard are drawn up to a
        /// step behind the colliders; a ghost standing where it is drawn on the deck is placed there.
        /// </summary>
        public (Vector3 At, Vector3 Way) Shown(Vector3 at, Vector3 way)
        {
            (Vector3 bodyAt, Quaternion bodyTurn) = Pose;
            Vector3 point = KwShipRiderPose.Carry(bodyAt, bodyTurn, transform.position, transform.rotation, at);
            return (point, KwShipRiderPose.CarryWay(bodyTurn, transform.rotation, way));
        }

        private void Awake()
        {
            (view, body) = (GetComponent<ZNetView>(), GetComponent<Rigidbody>());
            if (Zdo == null)
            {
                enabled = false;
                return;
            }
            loaded.Add(this);
            WearNTear? wear = GetComponent<WearNTear>();
            if (wear != null)
                wear.m_onDestroyed += () => KwShipBody.Step("Broadside ship riders wrecked", () => KwShipRiderBreak.Wreck(this));
        }

        private void OnDestroy() => loaded.Remove(this);

        // The owner gives the ship its id once; a ship launched before riders came, or whose owner is not yet known, is
        // looked at again every Again seconds until it has one.
        private void Update()
        {
            if (Time.time - looked < Again)
                return;
            looked = Time.time;
            ZDO? zdo = Zdo;
            if (zdo != null && zdo.IsOwner() && zdo.GetLong(IdHash, 0L) == 0L)
                zdo.Set(IdHash, NewId());
            enabled = Id == 0L;
        }

        private static long NewId()
        {
            long id = System.BitConverter.ToInt64(System.Guid.NewGuid().ToByteArray(), 0);
            return id != 0L ? id : 1L;
        }

        /// <summary>The loaded ship of this id, or null.</summary>
        public static KwShipRiders? Find(long id)
        {
            if (id == 0L)
                return null;
            foreach (KwShipRiders ship in loaded)
            {
                if (ship != null && ship.Zdo != null && ship.Id == id)
                    return ship;
            }
            return null;
        }

        /// <summary>A piece made this ship's child at its pose in the ship's frame, the body's mass as it was.</summary>
        public void Take(KwShipRider rider, Vector3 local, Quaternion turn)
        {
            KwBodyMass kept = KwBodyMass.Of(body);
            rider.transform.SetParent(transform, false);
            rider.transform.SetLocalPositionAndRotation(local, turn);
            kept.Put(body);
            if (!riders.Contains(rider))
                riders.Add(rider);
        }

        /// <summary>A piece let go where it is in the world, the body's mass as it was.</summary>
        public void Drop(KwShipRider rider)
        {
            riders.Remove(rider);
            if (rider == null || rider.transform.parent != transform)
                return;
            KwBodyMass kept = KwBodyMass.Of(body);
            rider.transform.SetParent(null, true);
            kept.Put(body);
        }

        /// <summary>A rider gone (destroyed, or no longer a rider), off the list.</summary>
        public void Forget(KwShipRider rider) => riders.Remove(rider);

        /// <summary>
        /// Every rider let go where it is, before the game destroys this ship's object: when its ZDO is destroyed too
        /// (<paramref name="destroyed"/>), the ship is remembered as gone, and each rider's owner makes it an ordinary piece.
        /// </summary>
        public void LetGo(bool destroyed)
        {
            long id = destroyed ? Id : 0L;
            if (id != 0L)
                gone.Add(id);
            foreach (KwShipRider rider in riders.ToArray())
            {
                if (rider != null)
                    rider.Leave(destroyed);
            }
            riders.Clear();
        }

        /// <summary>A ship of this id destroyed elsewhere (its ZDO gone, this peer had no object for it).</summary>
        public static void Destroyed(long id)
        {
            if (id != 0L)
                gone.Add(id);
        }
    }

    /// <summary>
    /// A body's mass, centre of mass and turning, read and put back (set by hand, they stay as they are when a child's
    /// colliders join the body; put back all the same, so no rider ever changes how the ship floats).
    /// </summary>
    public readonly struct KwBodyMass
    {
        private readonly bool known;
        private readonly float mass;
        private readonly Vector3 centre, tensor;
        private readonly Quaternion tensorTurn;

        private KwBodyMass(Rigidbody body) =>
            (known, mass, centre, tensor, tensorTurn) = (true, body.mass, body.centerOfMass, body.inertiaTensor, body.inertiaTensorRotation);

        public static KwBodyMass Of(Rigidbody? body) => body != null ? new KwBodyMass(body) : default;

        public void Put(Rigidbody? body)
        {
            if (!known || body == null)
                return;
            (body.mass, body.centerOfMass, body.inertiaTensor, body.inertiaTensorRotation) = (mass, centre, tensor, tensorTurn);
        }
    }
}
