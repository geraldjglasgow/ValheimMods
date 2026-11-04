using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's figures from its build (<see cref="KwShipStats"/>), its load's drag (<see cref="KwShipCargo"/>)
    /// and its rudders' damage (<see cref="KwRigDamageDrive.Steer"/>), kept here as shares of the karve's and multiplied
    /// onto the game's Ship fields where the step uses them (<see cref="KwShipDrive"/>, <see cref="KwShipWind"/>), never
    /// written into them. The fields keep the karve's values, so whatever another mod sets there carries over: ShipConfig's
    /// values for <c>KW_ship</c> from the start and when changed live, a talent or skill raising the sail force for a
    /// step (a player's report, 2026-11-04: ShipConfig's speed took only on the ship they stood on, after a live edit, and
    /// Path of Valheiman's sailing talents not at all). The ship's health is held the same way, as its build's times
    /// <c>KW_ship</c>'s against the karve's (<see cref="Hold"/>).
    /// </summary>
    public sealed class KwShipTrim : MonoBehaviour
    {
        private static ZNetScene? scene;
        private static WearNTear? prefabWear;
        private WearNTear? wear;

        /// <summary>The sails' push, the drag along the ship and the steering against the karve's; the helm's swing.</summary>
        public float Sail { get; set; } = 1f;
        public float Drag { get; set; } = 1f;
        public float Steer { get; set; } = 1f;
        public float Helm { get; set; } = 1f;

        /// <summary>The load's extra drag (1 unladen) and the share of the steering the rudders' damage leaves (1 whole).</summary>
        public float Cargo { get; set; } = 1f;
        public float Rig { get; set; } = 1f;

        /// <summary>The ship's full health by its build, and where its sails push from over its centre of weight.</summary>
        public float Health { get; set; } = KwShipStats.KarveHealth;
        public float SailOffset { get; set; } = 1f;

        public static KwShipTrim Of(Ship ship) => ship.GetComponent<KwShipTrim>() ?? ship.gameObject.AddComponent<KwShipTrim>();

        private void Awake() => wear = GetComponent<WearNTear>();

        public float SailForce(Ship ship) => ship.m_sailForceFactor * Sail;
        public float ForwardDrag(Ship ship) => ship.m_dampingForward * Drag * Cargo;
        public float SteerPush(Ship ship) => ship.m_stearVelForceFactor * Steer * Rig;
        public float RowTurn(Ship ship) => ship.m_stearForce * Steer * Rig;

        /// <summary>The game's rudder swing, at the helm's share of the ship's rudder speed for that call only.</summary>
        public void UpdateRudder(Ship ship, float dt, bool controlled)
        {
            float kept = ship.m_rudderSpeed;
            ship.m_rudderSpeed = kept * Helm * Rig;
            try
            {
                ship.UpdateRudder(dt, controlled);
            }
            finally
            {
                ship.m_rudderSpeed = kept;
            }
        }

        /// <summary>The full health its build gives times <c>KW_ship</c>'s against the karve's, put back when anything set another.</summary>
        public void Hold()
        {
            if (wear == null)
                return;
            float full = Health * HealthShare();
            if (!Mathf.Approximately(wear.m_health, full))
                wear.m_health = full;
        }

        // KW_ship's health against the karve's (1 unless something such as ShipConfig set the prefab's).
        private static float HealthShare()
        {
            if (scene != ZNetScene.instance || prefabWear == null)
            {
                scene = ZNetScene.instance;
                prefabWear = scene != null ? scene.GetPrefab(KwShipPrefab.Name)?.GetComponent<WearNTear>() : null;
            }
            return prefabWear != null ? Mathf.Max(0f, prefabWear.m_health) / KwShipStats.KarveHealth : 1f;
        }
    }
}
