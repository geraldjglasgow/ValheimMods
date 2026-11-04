using System.Linq;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a crate the crane lifts is drawn (<see cref="KwCrate"/>), on every peer, from what is already replicated.
    /// Riding a launched ship (its rider record in its ZDO, <see cref="KwShipRider.ShipKey"/>): the rider places it, and this
    /// only makes the record and the rider agree here at once, so a crate set down on a deck by another game rides here
    /// too (a rider added) and one hooked off its ship by another game lets go of it here (its rider released) rather than
    /// a second later; the same for the game's cart (its link in its ZDO, <see cref="KwCargoCart.CartHash"/>, and
    /// <see cref="KwCartRider.Agree"/>). Hanging (<see cref="KwCrate.HangingFrom"/>): its lift point under the crane's hook, turned with the
    /// turret, in its rope sling (<see cref="KwCrateSling"/>). Free: at its ZDO's place, falling there under gravity when
    /// that is lower (set down from a hook), moved there at once otherwise, and riding the waves when afloat. While it hangs
    /// its boxes are triggers (<see cref="Hung"/>), so they push nothing. At rest, with
    /// nothing new in its ZDO, it does nothing. The lid is <see cref="KwCrateLid"/>'s. Sends nothing; a dedicated server
    /// only places the crate.
    /// </summary>
    public sealed class KwCrateLook : MonoBehaviour
    {
        private const float Gravity = 9.81f, Slide = 6f, Turn = 240f;

        private KwCrate? crate;
        private KwCrateSling? sling;
        private Collider[] solid = new Collider[0];
        private float fall;
        private uint seen;
        private bool screens, tried, still, broken, hung;

        private void Start() => Guard.Run("Broadside crate look", static l => l.Init(), this);

        private void Init()
        {
            crate = GetComponent<KwCrate>();
            screens = KwCargoModel.Screens;
            solid = GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).ToArray();
        }

        // After the cranes have set their hooks this frame; at rest with nothing new in the ZDO, nothing. A throw is
        // reported once and stops this crate's look.
        private void LateUpdate()
        {
            ZNetView? view = crate != null ? crate.View : null;
            if (broken || view == null || !view.IsValid() || still && view.GetZDO().DataRevision == seen)
                return;
            broken = true;
            Guard.Run("Broadside crate look", static (l, zdo) => l.Draw(zdo, Time.deltaTime), this, view.GetZDO());
            broken = false;
        }

        private void Draw(ZDO zdo, float dt)
        {
            seen = zdo.DataRevision;
            bool aboard = Aboard(zdo) | KwCartRider.Agree(gameObject, zdo);
            KwCrane? crane = aboard ? null : crate!.HangingFrom();
            Hung(crane != null);
            if (crane != null)
                Hang(crane);
            still = aboard || crane == null && Free(zdo, dt) && crate!.Crane.IsNone();
            Sling(crane);
        }

        // The rider record and the rider agree on this peer: set down on a deck by the crate's owner, it rides here too;
        // hooked off its ship, it lets go here now (its rider would within a second). Whether it rides.
        private bool Aboard(ZDO zdo)
        {
            bool record = zdo.GetLong(KwShipRider.ShipHash, 0L) != 0L;
            KwShipRider? rider = TryGetComponent(out KwShipRider found) ? found : null;
            if (record && rider == null)
                gameObject.AddComponent<KwShipRider>();
            else if (!record && rider != null && rider.Ship != 0L)
                rider.Release();
            return record;
        }

        // Hanging, its boxes push nothing: moved with the hook, swung into a hull or held on a deck it held a ship's body
        // fast where it hung (a player's report, 2026-11-04). Set down or riding, solid again.
        private void Hung(bool now)
        {
            if (now == hung)
                return;
            hung = now;
            foreach (Collider box in solid)
            {
                if (box != null)
                    box.isTrigger = now;
            }
        }

        private void Hang(KwCrane crane)
        {
            Quaternion turn = Quaternion.Euler(0f, crane.Rig!.Turret.eulerAngles.y + crate!.HangYaw, 0f);
            transform.SetPositionAndRotation(crane.Rig.Hook!.position - turn * crate.LiftLocal, turn);
            fall = 0f;
        }

        // To the ZDO's place: down under gravity, sideways and up quickly; afloat, on the waves where it floats. Whether
        // it is at rest there (not afloat, nothing left to move).
        private bool Free(ZDO zdo, float dt)
        {
            Vector3 at = zdo.GetPosition(), now = transform.position;
            float? waves = crate!.Afloat ? KwCargoCast.Water(at, 1f) : null;
            if (waves.HasValue)
                at.y = waves.Value - KwCrateRest.Draft;
            fall = now.y > at.y + 0.01f ? fall + Gravity * dt : 0f;
            float y = fall > 0f ? Mathf.Max(at.y, now.y - fall * dt) : at.y;
            Vector3 across = Vector3.MoveTowards(new Vector3(now.x, 0f, now.z), new Vector3(at.x, 0f, at.z), Slide * dt);
            Vector3 to = new Vector3(across.x, y, across.z);
            Quaternion turn = Quaternion.RotateTowards(transform.rotation, zdo.GetRotation(), Turn * dt);
            bool moves = (to - now).sqrMagnitude > 1e-8f || Quaternion.Angle(turn, transform.rotation) > 0.01f;
            if (moves)
                transform.SetPositionAndRotation(to, turn);
            return !moves && !waves.HasValue;
        }

        // The rope sling while it hangs from a crane, on a screen: made the first time, hidden when it does not.
        private void Sling(KwCrane? crane)
        {
            if (crane == null)
            {
                sling?.Hide();
                return;
            }
            if (sling == null && screens && !tried)
                (tried, sling) = (true, KwCrateSling.Make(crate!));
            sling?.Show(crate!.HangYaw);
        }
    }
}
