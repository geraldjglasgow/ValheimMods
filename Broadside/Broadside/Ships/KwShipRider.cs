using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hammer piece built on a launched ship (<see cref="KwShipRiderRules"/>), on every peer: its ZDO records its ship's
    /// own id (<see cref="ShipKey"/>, <see cref="KwShipRiders.IdKey"/>) and its pose in the ship's frame
    /// (<see cref="PosKey"/>, <see cref="RotKey"/>), written by the game that built it and again by its owner whenever it
    /// moves it about the ship (every other peer eases it after, <see cref="KwShipRiderMove"/>). Wherever the ship is in the
    /// scene, the piece is its child at that pose (<see cref="KwShipRiders.Take"/>): it follows the ship exactly, its
    /// colliders move with the hull and are part of the ship's body, which its weight is never added to. While it rides it
    /// needs no support (the ship holds it up), its own body, if it had one, is gone (a ship cannon's: the ship carries
    /// it) and the game's synced transform, if any, is off. Its owner keeps its ZDO's place with the ship (once a second,
    /// from the piece, or from the ship's ZDO when this game has no object for the ship), so it loads and unloads with
    /// the ship's sector. With its ship not loaded here it stays where it was (parked) until the ship comes back. When its
    /// ship is gone for good (destroyed, or nowhere near for a minute with the area loaded), its owner makes it an ordinary
    /// piece again where it is: the game's support rules then take it, as any piece's.
    /// </summary>
    public sealed class KwShipRider : MonoBehaviour
    {
        public const string ShipKey = "kw_rider", PosKey = "kw_rider_pos", RotKey = "kw_rider_rot";
        public static readonly int ShipHash = ShipKey.GetStableHashCode(), PosHash = PosKey.GetStableHashCode(), RotHash = RotKey.GetStableHashCode();
        private const float Look = 0.5f, Keep = 1f, Metres = 1f, Degrees = 5f, Hair = 0.0005f;

        private readonly KwShipRiderMove move = new KwShipRiderMove();
        private System.Action? moved;
        private ZNetView? view;
        private WearNTear? wear;
        private ZSyncTransform? sync;
        private KwShipRiders? on;
        private bool supportWear, synced, dressed, gone;
        private float next, missing;

        /// <summary>The ship's own id this piece rides.</summary>
        public long Ship { get; private set; }

        /// <summary>Its pose in the ship's frame.</summary>
        public Vector3 Local { get; private set; }
        public Quaternion Turn { get; private set; } = Quaternion.identity;

        /// <summary>The ship it is a child of now, or null while parked.</summary>
        public KwShipRiders? On => on;

        /// <summary>
        /// Marks a just placed piece's ZDO as riding this ship at its pose on the ship as drawn now (the ghost's, on the deck
        /// where it is drawn, <see cref="KwShipRiders.Shown"/>) and makes it ride; on the placing game only.
        /// </summary>
        public static void Place(GameObject piece, KwShipRiders ship)
        {
            ZDO? zdo = piece.GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null || ship.Id == 0L || piece.GetComponent<KwShipRider>() != null)
                return;
            Transform drawn = ship.transform;
            (Vector3 at, Quaternion turn) = KwShipRiderPose.Local(drawn.position, drawn.rotation, piece.transform.position, piece.transform.rotation);
            zdo.Set(PosHash, at);
            zdo.Set(RotHash, turn);
            zdo.Set(ShipHash, ship.Id);
            piece.AddComponent<KwShipRider>();
        }

        private void Awake() => Guard.Run("Broadside ship rider", Begin);

        // Aboard as soon as the game has made the object (not while it is being made), when its ship is here.
        private void Start() => KwShipBody.Step("Broadside ship rider", Board);

        // Its record read, its support, synced transform and own body held still until it is aboard.
        private void Begin()
        {
            (view, wear, sync) = (GetComponent<ZNetView>(), GetComponent<WearNTear>(), GetComponent<ZSyncTransform>());
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null || !Read(zdo))
            {
                Destroy(this);
                return;
            }
            if (wear != null)
                (supportWear, wear.m_noSupportWear) = (wear.m_noSupportWear, false);
            if (sync != null && sync.enabled)
                (synced, sync.enabled) = (true, false);
            if (TryGetComponent(out Rigidbody own))
                own.isKinematic = true;
            missing = Time.time;
        }

        private bool Read(ZDO zdo)
        {
            Ship = zdo.GetLong(ShipHash, 0L);
            Local = zdo.GetVec3(PosHash, Vector3.zero);
            Turn = KwShipRiderPose.Unit(zdo.GetQuaternion(RotHash, Quaternion.identity));
            return Ship != 0L;
        }

        private void OnDestroy()
        {
            if (on != null)
                on.Forget(this);
        }

        private void Update()
        {
            if (on != null && view != null && view.GetZDO() is ZDO live && move.Due(live))
                KwShipBody.Step("Broadside ship rider moved", moved ??= Moved);
            if (Time.time < next)
                return;
            next = Time.time + (on != null ? Keep : Look);
            KwShipBody.Step("Broadside ship rider", Tick);
        }

        // Its pose on the ship written again (moved about the deck by its owner): the record taken; on every other peer the
        // piece eased there from where it is drawn (the owner put it there itself).
        private void Moved()
        {
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null || on == null)
                return;
            if (move.Changed(zdo) && zdo.GetLong(ShipHash, 0L) == Ship)
                Repose(zdo);
            move.Ease(transform, Local, Turn);
        }

        private void Repose(ZDO zdo)
        {
            Vector3 at = zdo.GetVec3(PosHash, Local);
            Quaternion turn = KwShipRiderPose.Unit(zdo.GetQuaternion(RotHash, Turn));
            if (!KwShipRiderPose.Moved(at, turn, Local, Turn, Hair, Hair * 20f))
                return;
            (Local, Turn) = (at, turn);
            if (!zdo.IsOwner())
                move.Begin(transform);
        }

        private void Tick()
        {
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (zdo == null)
                return;
            if (zdo.GetLong(ShipHash, 0L) != Ship || on != null && on.Zdo == null)
                Changed(zdo);
            if (Ship == 0L)
                return;
            if (on == null)
                Board();
            if (zdo.IsOwner())
                Upkeep(zdo);
        }

        // Its record changed (made ordinary by its owner: released here too; another ship: aboard that), or its ship let go.
        private void Changed(ZDO zdo)
        {
            Leave(false);
            if (!Read(zdo))
                Release();
        }

        // Aboard its ship when it is in this game's scene.
        private void Board()
        {
            KwShipRiders? ship = KwShipRiders.Find(Ship);
            if (ship == null)
                return;
            (on, missing, gone) = (ship, Time.time, false);
            ship.Take(this, Local, Turn);
            Rigidbody? own = GetComponent<Rigidbody>();
            if (own != null)
                Unbody(own);
            if (KwShipBody.Drawn && !dressed)
                Dress();
        }

        // Its own body goes: the ship carries it, its colliders part of the ship's body (never simulated on their own again here).
        private static void Unbody(Rigidbody own)
        {
            (own.isKinematic, own.detectCollisions) = (true, false);
            Destroy(own);
        }

        // The ship's own copies of its piece materials (no world-laid noise streaking a moving piece), once.
        private void Dress()
        {
            dressed = true;
            KwShipMaterials.Apply(transform);
        }

        /// <summary>Let go where it is (parked) as its ship's object goes; when the ship was destroyed, its owner makes it ordinary.</summary>
        public void Leave(bool destroyed)
        {
            KwShipRiders? was = on;
            on = null;
            missing = Time.time;
            if (was != null)
                was.Drop(this);
            gone |= destroyed;
            if (destroyed && view != null && view.GetZDO() is ZDO zdo && zdo.IsOwner())
                Release();
        }

        // On the owner: the ZDO's place kept with the ship; a ship gone for good, or missing a minute, lets it go.
        private void Upkeep(ZDO zdo)
        {
            (Vector3 At, Quaternion Turn)? pose = on != null ? (transform.position, transform.rotation) : Followed();
            if (!pose.HasValue)
            {
                if (KwShipRiderRules.Released(gone || KwShipRiders.Gone(Ship), Time.time - missing, AreaReady()))
                    Release();
                return;
            }
            missing = Time.time;
            (Vector3 at, Quaternion turn) = pose.Value;
            if (KwShipRiderPose.Moved(at, turn, zdo.GetPosition(), zdo.GetRotation(), Metres, Degrees))
                Write(zdo, at, turn);
        }

        // Parked: where it would be on its ship as the ship's ZDO has it now (moved there too), or null when no such ship is near.
        private (Vector3 At, Quaternion Turn)? Followed()
        {
            ZDO? ship = KwShipRiderFind.Near(Ship, transform.position);
            if (ship == null)
                return null;
            (Vector3 at, Quaternion turn) = KwShipRiderPose.World(ship.GetPosition(), ship.GetRotation(), Local, Turn);
            transform.SetPositionAndRotation(at, turn);
            return (at, turn);
        }

        private static void Write(ZDO zdo, Vector3 at, Quaternion turn)
        {
            zdo.SetPosition(at);
            zdo.SetRotation(turn);
        }

        // The area round it loaded, so its ship's ZDO, were it near, would have come by now.
        private bool AreaReady() => ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(transform.position);

        /// <summary>An ordinary piece again: its record cleared (on its owner), its support and synced transform back.</summary>
        public void Release()
        {
            if (view != null && view.GetZDO() is ZDO zdo && zdo.IsOwner() && zdo.GetLong(ShipHash, 0L) != 0L)
                zdo.Set(ShipHash, 0L);
            if (on != null)
                Leave(false);
            if (wear != null)
                wear.m_noSupportWear = supportWear;
            if (sync != null && synced)
                sync.enabled = true;
            Ship = 0L;
            Destroy(this);
        }
    }
}
