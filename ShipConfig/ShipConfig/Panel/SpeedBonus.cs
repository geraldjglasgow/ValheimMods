using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// How much faster than vanilla a ship is at top speed, and from what. The sail and the oars push with a steady
    /// force while the forward drag grows with the square of the speed (Ship.CustomFixedUpdate), so top speed grows
    /// with the square root of force / drag. Vanilla is the ship's defaults as ShipConfig found them (its config
    /// defaults); the force and drag in use are read from the ship itself, so another mod's changes count too.
    /// GrindstoneSkills raises the forces only inside the physics step, so its Sailing bonus comes from its API.
    /// </summary>
    public static class SpeedBonus
    {
        private const float LeastDrag = 0.0001f;

        /// <summary>The ship is rowed (slow ahead or astern) rather than sailed.</summary>
        public static bool Rowing(Ship ship)
        {
            Ship.Speed setting = ship.GetSpeedSetting();
            return setting == Ship.Speed.Slow || setting == Ship.Speed.Back;
        }

        /// <summary>The ship's own top speed factor against vanilla, from its force and drag; 1 for a ship without entries.</summary>
        public static float FromSettings(Ship ship, bool rowing)
        {
            if (!ShipConfiguration.TryGet(Utils.GetPrefabName(ship.gameObject), out ShipEntries entries))
                return 1f;
            float vanillaForce = (float)(rowing ? entries.PaddleForce.DefaultValue : entries.SailForce.DefaultValue);
            float vanillaDrag = (float)entries.ForwardDrag.DefaultValue;
            if (vanillaForce <= 0f || vanillaDrag <= 0f)
                return 1f;
            float force = Mathf.Max(0f, rowing ? ship.m_backwardForce : ship.m_sailForceFactor);
            float drag = Mathf.Max(LeastDrag, ship.m_dampingForward);
            return Mathf.Sqrt(force / vanillaForce * (vanillaDrag / drag));
        }

        /// <summary>The whole factor and a short account of it for the tooltip.</summary>
        public static float Total(Ship ship, out string account)
        {
            bool rowing = Rowing(ship);
            float settings = FromSettings(ship, rowing);
            account = $"Top speed against a vanilla ship of this kind, {(rowing ? "rowing" : "under sail")}.\n" +
                $"Ship settings: x{settings:0.00}";
            if (!GrindstoneLink.Present)
                return settings;
            float skill = GrindstoneLink.SpeedFactor(ship);
            account += $"\nHelmsman's Sailing skill: x{skill:0.00}";
            return settings * skill;
        }
    }
}
