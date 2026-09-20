using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// The vanilla values of one ship, read from its prefab, or from a live instance when the prefab is not in the
    /// scene. They become the defaults of the ship's config entries, so a fresh config file leaves the game unchanged.
    /// </summary>
    public sealed class ShipDefaults
    {
        public float Health;
        public float SailForce;
        public float SailForceOffset;
        public float PaddleForce;
        public float RudderSpeed;
        public float TurnForceSailing;
        public float TurnForcePaddling;
        public float ForwardDrag;
        public float SidewaysDrag;
        public float AngularDamping;
        public float WaterImpactDamage;
        public float UpsideDownDamage;
        public bool WeatherWear;
        public bool AshlandsOceanDamage;

        /// <summary>The vanilla material amounts of the Piece; null when the ship has no Piece.</summary>
        public int[] BuildCosts;

        /// <summary>
        /// Reads the defaults from a prefab or from a loaded instance. Both need a Ship and a WearNTear component,
        /// otherwise null. An instance's max health already carries the world level bonus, which is taken off again.
        /// </summary>
        public static ShipDefaults From(GameObject source, bool isInstance)
        {
            Ship ship = source != null ? source.GetComponent<Ship>() : null;
            WearNTear wearNTear = source != null ? source.GetComponent<WearNTear>() : null;
            if (ship == null || wearNTear == null)
                return null;

            ShipDefaults defaults = new ShipDefaults
            {
                Health = isInstance ? wearNTear.m_health / ShipHealthUpdater.WorldLevelFactor() : wearNTear.m_health,
                WeatherWear = wearNTear.m_noRoofWear,
                AshlandsOceanDamage = !ship.m_ashlandsReady,
                BuildCosts = ShipBuildCost.ReadOriginals(source.GetComponent<Piece>()),
            };
            defaults.ReadSailing(ship);
            return defaults;
        }

        private void ReadSailing(Ship ship)
        {
            SailForce = ship.m_sailForceFactor;
            SailForceOffset = ship.m_sailForceOffset;
            PaddleForce = ship.m_backwardForce;
            RudderSpeed = ship.m_rudderSpeed;
            TurnForceSailing = ship.m_stearVelForceFactor;
            TurnForcePaddling = ship.m_stearForce;
            ForwardDrag = ship.m_dampingForward;
            SidewaysDrag = ship.m_dampingSideway;
            AngularDamping = ship.m_angularDamping;
            WaterImpactDamage = ship.m_waterImpactDamage;
            UpsideDownDamage = ship.m_upsideDownDmg;
        }
    }
}
