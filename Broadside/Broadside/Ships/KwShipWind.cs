using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's sails against the wind, on every peer: its sails by kind (<see cref="KwSailMix"/>, set by
    /// <see cref="KwShipStats.Apply"/>) and its apparent wind (<see cref="KwApparentWind"/>). On the ship's owner the
    /// physics step takes its sail push from here in place of the game's (<see cref="Push"/>): each kind's drive ahead and
    /// push to leeward at the apparent wind's angle (<see cref="KwSailPolar"/>), times the ship's sail force and the wind's
    /// strength, eased over a second as the game eases its own. Every screen swings the fore-and-aft sails to the same
    /// wind (<see cref="KwBoomSwing"/>, <see cref="KwJibSwing"/>) from the ship's speed as it is seen there (its place a
    /// frame apart, smoothed over a second), since the wind is the game's own everywhere: nothing is sent.
    /// </summary>
    public sealed class KwShipWind : MonoBehaviour
    {
        private const float Smoothing = 1f, Fastest = 40f;

        private Vector3 last, velocity;
        private bool started;

        /// <summary>The ship's sails by kind.</summary>
        public KwSailMix Mix { get; set; } = KwSailMix.None;

        /// <summary>The ship's speed as this peer sees it, level and smoothed.</summary>
        public Vector3 Velocity => velocity;

        /// <summary>The apparent wind on a sail heading that way, as this peer sees the ship move.</summary>
        public KwApparentWind Now(Vector3 forward) => Air(velocity, forward);

        /// <summary>The apparent wind for a speed and a heading, from the game's wind (none before there is one).</summary>
        public static KwApparentWind Air(Vector3 speed, Vector3 forward)
        {
            EnvMan? env = EnvMan.instance;
            return new KwApparentWind(env != null ? env.GetWindDir() : Vector3.zero, env != null ? env.GetWindIntensity() : 0f, speed, forward);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 at = transform.position;
            if (started && dt > 0f)
            {
                Vector3 moved = Vector3.ClampMagnitude((at - last) / dt, Fastest);
                velocity = Vector3.Lerp(velocity, new Vector3(moved.x, 0f, moved.z), Mathf.Clamp01(dt / Smoothing));
            }
            (last, started) = (at, true);
        }

        /// <summary>
        /// The owner's sail push this step at a sail size (1 full, 0.5 half), from its body's own speed, eased as the game
        /// eases its own (<c>Ship.GetSailForce</c>); the game's own push while there is no wind yet or the sails are not sorted by kind.
        /// </summary>
        public static Vector3 Push(Ship ship, float sailSize, float dt)
        {
            KwShipWind? wind = ship.GetComponent<KwShipWind>();
            if (wind == null || EnvMan.instance == null)
                return ship.GetSailForce(sailSize, dt);
            KwApparentWind air = Air(ship.m_body.linearVelocity, ship.transform.forward);
            bool half = sailSize < 0.99f;
            // the mix's push, less what shot-down, holed and drooping sails no longer give (KwRigDamageDrive)
            Vector3 push = KwRigDamageDrive.Push(ship, wind.Mix, air, half);
            Vector3 target = push * (air.Strength * ship.m_sailForceFactor * sailSize);
            ship.m_sailForce = Vector3.SmoothDamp(ship.m_sailForce, target, ref ship.m_windChangeVelocity, 1f, 99f, dt);
            return ship.m_sailForce;
        }
    }
}
