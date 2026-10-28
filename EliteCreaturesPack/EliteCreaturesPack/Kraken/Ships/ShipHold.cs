using UnityEngine;

namespace EliteCreaturesPack.Kraken
{
    /// <summary>
    /// A ship in the kraken's grip, on the machine that sails it (the ship's owner, where the game moves it): its sail
    /// comes down and stays down, and it is held where the kraken caught it, free to bob and roll on the waves but not to
    /// sail, drift or turn. The grip is read from the kraken's own ZDO (its phase and the ship's id), so it holds on
    /// whichever machine owns the ship and ends the moment the kraken lets go, leaves or dies. Blows on the ship rock it
    /// there, and a smash damages it through the game's own damage for ships. A ship its crew abandoned is dragged under
    /// in place of floating, and breaks there by the same damage.
    /// </summary>
    public static class ShipHold
    {
        private const float Pull = 0.5f;       // per second: drifting back to the anchor
        private const float MaxDrift = 1f;     // metres a second at most
        private const float Yaw = 0.85f;       // share of its turning kept each step
        private const float DragSpeed = 0.8f;  // metres a second it goes down while dragged under
        private const float DragPitch = 0.06f; // radians a second its bow dips while dragged under

        /// <summary>The loaded kraken holding this ship, or null.</summary>
        public static KrakenBrain? Holder(Ship ship)
        {
            ZDO? zdo = ship.m_nview != null && ship.m_nview.IsValid() ? ship.m_nview.GetZDO() : null;
            if (zdo == null)
            {
                return null;
            }
            foreach (KrakenBrain brain in KrakenBrain.Loaded)
            {
                if (brain != null && brain.Holds(zdo.m_uid))
                {
                    return brain;
                }
            }
            return null;
        }

        /// <summary>
        /// OWNER OF THE SHIP, as a hit reaches it: while a kraken holds it, the crew's own blows (blades, arrows, fire) are
        /// spared, so they can fight the kraken from the deck without breaking their ship. The kraken's smashes still land.
        /// </summary>
        public static bool Spares(WearNTear part, HitData hit)
        {
            Ship? ship = part.GetComponent<Ship>();
            return ship != null && hit.GetAttacker() is Player && Holder(ship) != null;
        }

        /// <summary>OWNER OF THE SHIP, before the game moves it: no sail while held.</summary>
        public static void Furl(Ship ship)
        {
            if (ship.m_nview != null && ship.m_nview.IsValid() && ship.m_nview.IsOwner() && Holder(ship) != null)
            {
                ship.m_speed = Ship.Speed.Stop;
            }
        }

        /// <summary>OWNER OF THE SHIP, after the game moved it: back towards the anchor, no turning.</summary>
        public static void Keep(Ship ship)
        {
            if (ship.m_nview == null || !ship.m_nview.IsValid() || !ship.m_nview.IsOwner())
            {
                return;
            }
            KrakenBrain? holder = Holder(ship);
            if (holder == null || ship.m_body == null)
            {
                return;
            }
            Vector3 pull = holder.State.Anchor - ship.transform.position;
            Vector3 drift = Vector3.ClampMagnitude(new Vector3(pull.x, 0f, pull.z) * Pull, MaxDrift);
            Rigidbody body = ship.m_body;
            body.linearVelocity = new Vector3(drift.x, body.linearVelocity.y, drift.z);
            Vector3 turning = body.angularVelocity;
            body.angularVelocity = new Vector3(turning.x, turning.y * Yaw, turning.z);
        }

        /// <summary>A blow landing on the ship at a point: it rocks, on the machine that sails it.</summary>
        public static void Rock(Ship ship, Vector3 at, float strength)
        {
            if (ship.m_nview != null && ship.m_nview.IsValid() && ship.m_nview.IsOwner() && ship.m_body != null)
            {
                ship.m_body.AddForceAtPosition(Vector3.down * (ship.m_body.mass * strength), at, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// OWNER OF THE SHIP, in place of the game's floating: a ship its crew abandoned goes down bow first while the
        /// kraken drags it under. False when it is not being dragged, and the game floats it as usual.
        /// </summary>
        public static bool Sinks(Ship ship)
        {
            if (ship.m_nview == null || !ship.m_nview.IsValid() || !ship.m_nview.IsOwner() || ship.m_body == null)
            {
                return false;
            }
            KrakenBrain? holder = Holder(ship);
            if (holder == null || holder.State.Phase != KrakenPhase.Drag)
            {
                return false;
            }
            ship.m_body.linearVelocity = Vector3.down * DragSpeed;
            ship.m_body.angularVelocity = ship.transform.right * DragPitch;
            return true;
        }

        /// <summary>KRAKEN'S OWNER: a smash's damage to the ship (the ship's owner applies it, as for any hit on a ship).</summary>
        public static void Damage(Ship ship, Character kraken, Vector3 at)
        {
            if (KrakenSettings.ShipDamage > 0f)
            {
                Hit(ship, kraken, at, new HitData.DamageTypes { m_blunt = KrakenSettings.ShipDamage });
            }
        }

        /// <summary>
        /// KRAKEN'S OWNER: the ship it dragged under breaks, by the game's own destruction for ships (its cargo and timbers
        /// float up in the game's crates): a blow of all the health it has left.
        /// </summary>
        public static void Wreck(Ship ship, Character kraken)
        {
            WearNTear? hull = ship.GetComponent<WearNTear>();
            ZDO? zdo = ship.m_nview != null && ship.m_nview.IsValid() ? ship.m_nview.GetZDO() : null;
            if (hull != null && zdo != null)
            {
                float left = zdo.GetFloat(ZDOVars.s_health, hull.m_health);
                Hit(ship, kraken, ship.transform.position, new HitData.DamageTypes { m_damage = left + 1f });
            }
        }

        private static void Hit(Ship ship, Character kraken, Vector3 at, HitData.DamageTypes damage)
        {
            WearNTear? hull = ship.GetComponent<WearNTear>();
            if (hull == null)
            {
                return;
            }
            var hit = new HitData { m_point = at, m_dir = Vector3.down, m_toolTier = 100, m_hitType = HitData.HitType.EnemyHit, m_damage = damage };
            hit.SetAttacker(kraken);
            hull.Damage(hit);
        }
    }
}
