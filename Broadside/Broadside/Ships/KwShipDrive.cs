using HarmonyLib;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's physics step, in place of the game's for its own ships (<c>Ship.CustomFixedUpdate</c>): the
    /// same controls, sail, rudder, damage and edge of the world as the game's ships, and the same pushes from rudder and
    /// oars; the sails' push is each kind of sail's own by the apparent wind (<see cref="KwShipWind.Push"/>), with the water's lift from the ship's own hull (<see cref="KwShipHydro"/>) in place of the game's
    /// float, tuned for the karve, which would fight it. The game's drag on the ship's speed is kept as it is when its
    /// ships float. The game's ships run the game's step unchanged.
    /// </summary>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    public static class KwShipDrive
    {
        // The game's drag at the depth its ships usually float at (its own share of the drag, there).
        internal const float DragShare = 0.25f;

        // Turning costs speed: a share of the speed along the ship lost a second at full helm, and per radian a second of turn.
        internal const float HelmDrag = 0.3f, TurnDrag = 0.8f;

        // The share of the sail's sideways push the keel lets through (leeway).
        private const float Leeway = 0.25f;

        [HarmonyPrefix]
        private static bool Prefix(Ship __instance, float fixedDeltaTime)
        {
            KwShipHydro? hydro = __instance.GetComponent<KwShipHydro>();
            if (hydro == null)
                return true;
            KwShipBody.Step("Broadside ship physics", () => Step(__instance, hydro, fixedDeltaTime));
            return false;
        }

        private static void Step(Ship ship, KwShipHydro hydro, float dt)
        {
            bool controlled = ship.HaveControllingPlayer();
            ship.UpdateControlls(dt);
            ship.UpdateSail(dt, controlled);
            ship.UpdateRudder(dt, controlled);
            if (ship.m_nview != null && !ship.m_nview.IsOwner())
                return;
            ship.UpdateUpsideDmg(dt);
            ship.TakeAshlandsDamage(dt * KwShipPerks.ShareOf(ship));
            if (ship.m_players.Count == 0)
                (ship.m_speed, ship.m_rudderValue) = (Ship.Speed.Stop, 0f);
            if (!controlled && (ship.m_speed == Ship.Speed.Slow || ship.m_speed == Ship.Speed.Back))
                ship.m_speed = Ship.Speed.Stop;
            if (!hydro.Float() || !hydro.Afloat)
                return;
            Drag(ship, dt);
            Drive(ship, dt);
            ship.ApplyEdgeForce(dt);
        }

        // The game's drag on a floating ship (along and across it, its turning, a near stop with nobody aboard), and the
        // speed a turn costs: more the harder the helm is over and the faster the ship swings.
        private static void Drag(Ship ship, float dt)
        {
            Rigidbody body = ship.m_body;
            Vector3 v = body.linearVelocity, forward = ship.transform.forward, right = ship.transform.right;
            float along = Vector3.Dot(v, forward), across = Vector3.Dot(v, right);
            float turning = Mathf.Abs(ship.m_rudderValue) * HelmDrag + Mathf.Abs(Vector3.Dot(body.angularVelocity, ship.transform.up)) * TurnDrag;
            v -= forward * (along * Mathf.Clamp01(turning * dt));
            v -= forward * Mathf.Clamp(along * along * Mathf.Sign(along) * ship.m_dampingForward * DragShare, -1f, 1f);
            v -= right * Mathf.Clamp(across * across * Mathf.Sign(across) * ship.m_dampingSideway * DragShare, -1f, 1f);
            if (ship.m_players.Count == 0)
                (v.x, v.z) = (v.x * 0.1f, v.z * 0.1f);
            body.linearVelocity = v;
            body.angularVelocity -= body.angularVelocity * (ship.m_angularDamping * DragShare);
        }

        // The game's pushes: the sail's (Sail), the rudder's and the oars' at the stern; at anchor none, the ship riding
        // to its anchor instead (KwShipAnchor).
        private static void Drive(Ship ship, float dt)
        {
            if (KwShipAnchor.Holding(ship, dt))
                return;
            Rigidbody body = ship.m_body;
            Vector3 forward = ship.transform.forward, right = ship.transform.right;
            Sail(ship, body, forward, dt);
            Vector3 stern = ship.transform.position + forward * ship.m_stearForceOffset;
            float along = Vector3.Dot(body.linearVelocity, forward);
            body.AddForceAtPosition(body.mass * along * ship.m_stearVelForceFactor * -ship.m_rudderValue * dt * right, stern, ForceMode.Impulse);
            body.AddForceAtPosition(Row(ship, forward, right) * (body.mass * dt), stern, ForceMode.Impulse);
        }

        // The sails' push (each kind's by the apparent wind, KwShipWind) split: along the ship it drives, a little over the
        // centre of weight (m_sailForceOffset); across it the keel takes most of it (Leeway kept), at the centre of weight,
        // so a wind on the beam, or a fore-and-aft sail close-hauled, neither lays the ship over nor stops it. The game's
        // push leaned half into the wind's way, and applied whole at the masthead it tipped a tall ship over and stalled it.
        private static void Sail(Ship ship, Rigidbody body, Vector3 forward, float dt)
        {
            float sailSize = ship.m_speed == Ship.Speed.Full ? 1f : ship.m_speed == Ship.Speed.Half ? 0.5f : 0f;
            Vector3 sail = KwShipWind.Push(ship, sailSize, dt) * body.mass;
            Vector3 ahead = forward * Vector3.Dot(sail, forward);
            Vector3 across = Vector3.ProjectOnPlane(sail - ahead, ship.transform.up) * Leeway;
            body.AddForceAtPosition(ahead, body.worldCenterOfMass + ship.transform.up * ship.m_sailForceOffset, ForceMode.Impulse);
            body.AddForce(across, ForceMode.Impulse);
        }

        // Rowing: ahead slowly or astern, and turning while rowing, as the game's ships row.
        private static Vector3 Row(Ship ship, Vector3 forward, Vector3 right)
        {
            if (ship.m_speed != Ship.Speed.Slow && ship.m_speed != Ship.Speed.Back)
                return Vector3.zero;
            float way = ship.m_speed == Ship.Speed.Back ? -1f : 1f;
            return forward * (way * ship.m_backwardForce * (1f - Mathf.Abs(ship.m_rudderValue))) + right * (ship.m_stearForce * -ship.m_rudderValue * way);
        }
    }
}
