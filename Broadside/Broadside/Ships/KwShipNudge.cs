using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// H at the helm of a Broadside ship nudges it free when it is stuck (aground on a bar, wedged on rocks, laid over on
    /// a shore): the ship's owner lifts it a little, sets it upright, and pushes it toward the deepest water round it. At
    /// most once every <see cref="Every"/> seconds. The helmsman's game asks the owner (an RPC), who moves the body as every
    /// move of a ship is made, so every peer sees the same.
    /// </summary>
    public sealed class KwShipNudge : MonoBehaviour
    {
        private const string Rpc = "KwShipNudge";
        private const float Every = 1.5f, Lift = 3.5f, Push = 4.5f, Look = 8f, Upright = 0.5f;
        private ZNetView? view;
        private Ship? ship;
        private float last = -99f;

        private void Awake()
        {
            view = GetComponent<ZNetView>();
            ship = GetComponent<Ship>();
            if (view != null && view.GetZDO() != null)
                view.Register(Rpc, (long _) => Guard.Run("Broadside ship nudge", Nudge));
        }

        // The helmsman's game: H while he steers this ship (not while typing), at most once every Every seconds.
        private void Update()
        {
            Player? me = Player.m_localPlayer;
            if (me == null || ship == null || view == null || me.GetControlledShip() != ship)
                return;
            if (!Hotkey.Pressed(KeyCode.H) || !me.TakeInput() || Time.time - last < Every)
                return;
            last = Time.time;
            view.InvokeRPC(Rpc);
            me.Message(MessageHud.MessageType.Center, "You push the ship off");
        }

        // On the owner: up a little, toward the deepest water round it, and upright.
        private void Nudge()
        {
            if (view == null || !view.IsOwner() || ship == null || ship.m_body == null)
                return;
            Rigidbody body = ship.m_body;
            body.AddForce(Vector3.up * Lift + Deepest(transform.position) * Push, ForceMode.VelocityChange);
            body.angularVelocity = Vector3.zero;
            Vector3 ahead = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (ahead.sqrMagnitude > 0.01f)
                body.MoveRotation(Quaternion.Slerp(body.rotation, Quaternion.LookRotation(ahead.normalized), Upright));
        }

        // The level way, of eight round the ship, where the ground lies lowest some way off.
        private static Vector3 Deepest(Vector3 at)
        {
            Vector3 best = Vector3.zero;
            float lowest = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 way = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                float ground = ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(at + way * Look) : 0f;
                if (ground < lowest)
                    (lowest, best) = (ground, way);
            }
            return best;
        }
    }
}
