using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's anchor, on a ship whose blueprint has an anchor windlass (<see cref="KwAnchor"/>). Whether it is
    /// down and where it lies live in the ship's ZDO (<see cref="DownKey"/>, <see cref="AtKey"/>: the sea floor under the
    /// hawse where it was let go), so it holds after a logout, with nobody aboard and for every peer. E at a windlass asks the
    /// ship's owner (<see cref="Call"/>), who flips it. While it is down the owner's physics step rides the ship to it
    /// (<see cref="Holding"/>, called from <see cref="KwShipDrive"/> in place of the sail's, rudder's and oars' pushes, so
    /// sailing at anchor moves nothing; <see cref="KwMooring"/>), and the helmsman is told why the ship will not sail. The
    /// cable leaves the ship where its windlass's lead puts it (<see cref="Hawse"/>: the fairlead nearest the windlass,
    /// <see cref="KwAnchorPlan"/>, worked out from the blueprint on every peer), the anchor is let go under it and the ship
    /// rides to it there; with no lead found, at the bow's stem head, the top of the hull's foremost section.
    /// </summary>
    public sealed class KwShipAnchor : MonoBehaviour
    {
        public const string DownKey = "kw_anchor_down", AtKey = "kw_anchor_at";
        private const string Call = "KW_AnchorToggle";
        private const float WarnEvery = 6f, UnderHead = 0.15f, AheadOfStem = 0.1f, NoGround = 10f;
        private static readonly int DownHash = DownKey.GetStableHashCode(), AtHash = AtKey.GetStableHashCode();

        private ZNetView? view;
        private Ship? ship;
        private float warned = -99f;

        /// <summary>Where the cable leaves the ship, in its own frame: at its fairlead, or at the stem head, a little ahead of it.</summary>
        public Vector3 Hawse { get; private set; }

        public bool Down => view != null && view.IsValid() && view.GetZDO().GetBool(DownHash);

        /// <summary>Where the anchor lies on the sea floor (the ship's place when nothing is saved).</summary>
        public Vector3 At => view != null && view.IsValid() ? view.GetZDO().GetVec3(AtHash, transform.position) : transform.position;

        /// <summary>Sets the anchor up on its ship: the cable leaving it at a fairlead (the ship's frame) or, with none, at the stem head.</summary>
        public void Set(Ship game, KwShipHydro hydro, Vector3? fairlead)
        {
            ship = game;
            view = GetComponent<ZNetView>();
            Hawse = fairlead ?? Bow(hydro);
            if (view != null && view.GetZDO() != null)
                view.Register(Call, _ => Guard.Run("Broadside anchor", Flip));
        }

        // The top of the foremost hull section, a little under it and ahead of it.
        private static Vector3 Bow(KwShipHydro hydro)
        {
            if (hydro.Slices.Count == 0)
                return new Vector3(0f, hydro.Waterline + 1f, 1f);
            KwHullSlice last = hydro.Slices[hydro.Slices.Count - 1];
            float top = hydro.Waterline;
            foreach (Vector2 p in last.Shape)
                top = Mathf.Max(top, p.y);
            return new Vector3(0f, top - UnderHead, last.Z + last.Length / 2f + AheadOfStem);
        }

        /// <summary>Asks the ship's owner to drop the anchor, or to weigh it.</summary>
        public void Ask()
        {
            if (view != null && view.IsValid())
                view.InvokeRPC(Call);
        }

        // On the owner: dropped on the sea floor under where the cable leaves the ship, or weighed.
        private void Flip()
        {
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            ZDO zdo = view.GetZDO();
            bool down = Down;
            if (!down)
                zdo.Set(AtHash, Floor(transform.TransformPoint(Hawse)));
            zdo.Set(DownHash, !down);
        }

        // The ground under a point (the sea floor), or some way under the sea where no ground is found.
        private static Vector3 Floor(Vector3 at)
        {
            float sea = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : at.y;
            float ground = ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out float height) ? height : sea - NoGround;
            return new Vector3(at.x, Mathf.Min(ground, sea), at.z);
        }

        /// <summary>
        /// The owner's physics step at anchor: true while the ship rides to its anchor (the caller then leaves out the
        /// sail's, rudder's and oars' pushes); false without an anchor or with it weighed.
        /// </summary>
        public static bool Holding(Ship ship, float dt)
        {
            if (!ship.TryGetComponent(out KwShipAnchor anchor) || ship.m_body == null || !anchor.Down)
                return false;
            anchor.Hold(ship.m_body, dt);
            return true;
        }

        // The speed over the ground and the swing dragged away, the hawse pulled toward the anchor at the height of the
        // centre of weight (so the cable neither heels nor pitches the ship), the wind pushing.
        private void Hold(Rigidbody body, float dt)
        {
            Vector3 v = body.linearVelocity, w = body.angularVelocity;
            float keep = KwMooring.Kept(KwMooring.Drag, dt);
            body.linearVelocity = new Vector3(v.x * keep, v.y, v.z * keep);
            body.angularVelocity = new Vector3(w.x, w.y * KwMooring.Kept(KwMooring.Turn, dt), w.z);
            Vector3 hawse = transform.TransformPoint(Hawse);
            hawse.y = body.worldCenterOfMass.y;
            body.AddForceAtPosition(KwMooring.Pull(hawse, At) * body.mass, hawse, ForceMode.Force);
            EnvMan? env = EnvMan.instance;
            if (env != null)
                body.AddForce(KwMooring.Wind(env.GetWindDir(), env.GetWindIntensity()) * body.mass, ForceMode.Force);
        }

        // The helmsman's game: setting sail at anchor tells him why the ship stays.
        private void Update()
        {
            Player? me = Player.m_localPlayer;
            if (me == null || ship == null || me.GetControlledShip() != ship || Time.time - warned < WarnEvery)
                return;
            if (ship.m_speed == Ship.Speed.Stop || !Down)
                return;
            warned = Time.time;
            me.Message(MessageHud.MessageType.Center, "The anchor is down: weigh it at the windlass to sail");
        }
    }
}
