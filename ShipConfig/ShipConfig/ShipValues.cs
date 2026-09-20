using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// Writes the effective values into the ship prefab (so new and newly loaded ships get them) and into every
    /// loaded ship of that kind, found through the game's own list of live ships. Ship reads its fields every
    /// physics tick, so a change takes effect at once.
    /// </summary>
    public static class ShipValues
    {
        /// <summary>Applies every known ship, used when a global multiplier changes and at discovery.</summary>
        public static void ApplyAll()
        {
            foreach (ShipEntries entries in ShipConfiguration.Ships.Values)
                Apply(entries);
        }

        /// <summary>Applies one ship by prefab name, used by the per-ship change handlers.</summary>
        public static void Apply(string name)
        {
            if (ShipConfiguration.TryGet(name, out ShipEntries entries))
                Apply(entries);
        }

        private static void Apply(ShipEntries entries)
        {
            ApplyToPrefab(entries);
            foreach (IMonoUpdater updater in Ship.Instances)
            {
                Ship ship = updater as Ship;
                if (ship != null && Utils.GetPrefabName(ship.gameObject) == entries.Name)
                    ApplyToInstance(ship, ship.GetComponent<WearNTear>(), entries);
            }
        }

        /// <summary>Applies to the scene prefab of the ship, including its build cost.</summary>
        public static void ApplyToPrefab(ShipEntries entries)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(entries.Name) : null;
            Ship ship = prefab != null ? prefab.GetComponent<Ship>() : null;
            WearNTear wearNTear = prefab != null ? prefab.GetComponent<WearNTear>() : null;
            if (ship == null || wearNTear == null)
                return;

            WriteFields(ship, wearNTear, entries);
            ShipHealthUpdater.ApplyToPrefab(wearNTear, entries.EffectiveHealth);
            ShipBuildCost.Apply(prefab.GetComponent<Piece>(), entries);
        }

        /// <summary>Applies to one loaded ship.</summary>
        public static void ApplyToInstance(Ship ship, WearNTear wearNTear, ShipEntries entries)
        {
            if (ship == null || wearNTear == null)
                return;

            WriteFields(ship, wearNTear, entries);
            ShipHealthUpdater.ApplyToInstance(wearNTear, entries.EffectiveHealth);
        }

        /// <summary>The plain field writes; WeatherWear and AshlandsOceanDamage are inverted on the game side.</summary>
        private static void WriteFields(Ship ship, WearNTear wearNTear, ShipEntries entries)
        {
            ship.m_sailForceFactor = entries.EffectiveSailForce;
            ship.m_sailForceOffset = entries.EffectiveSailForceOffset;
            ship.m_backwardForce = entries.EffectivePaddleForce;
            ship.m_rudderSpeed = entries.EffectiveRudderSpeed;
            ship.m_stearVelForceFactor = entries.EffectiveTurnForceSailing;
            ship.m_stearForce = entries.EffectiveTurnForcePaddling;
            ship.m_dampingForward = entries.EffectiveForwardDrag;
            ship.m_dampingSideway = entries.EffectiveSidewaysDrag;
            ship.m_angularDamping = entries.EffectiveAngularDamping;
            ship.m_waterImpactDamage = entries.EffectiveWaterImpactDamage;
            ship.m_upsideDownDmg = entries.EffectiveUpsideDownDamage;
            ship.m_ashlandsReady = !entries.AshlandsOceanDamage.Value;
            wearNTear.m_noRoofWear = entries.WeatherWear.Value;
        }
    }
}
