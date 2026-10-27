using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A shot that goes on under the water (<see cref="KwShot.Dives"/>: the cannonballs), on the game that flies it (the
    /// shooter's: the game's projectile moves only there, and every other peer sees it through its ZDO, so nothing more is
    /// sent). The game would stop it at the water's surface; its prefab has that off, and here, where it crosses the
    /// surface, the shot's splash plays (its water hit effects, as the game plays them there) and its burst and hit effects
    /// are taken off (it bursts only above water). Under the water its speed is kept for the first
    /// <see cref="KwWaterFlight.Keep"/> of its path, then falls off with the path (<see cref="KwWaterFlight"/>), and a
    /// little buoyancy holds back the game's gravity; it is gone once slower than <see cref="KwWaterFlight.Slow"/> or
    /// deeper than <see cref="KwWaterFlight.Deep"/>. The game's projectile still moves it and still tests its path for
    /// hits, so what it strikes on its way takes the hit (a ship's hull below the waterline, a creature, a piece), by the
    /// speed it has left (<see cref="Share"/>, <see cref="KwShotHits"/>). It goes about a metre a physics step, so what it
    /// strikes in the very step it meets the water (within that metre of the surface) is struck as above water.
    /// </summary>
    public sealed class KwShotWater : MonoBehaviour
    {
        private Projectile? flight;
        private ZNetView? view;
        private Vector3 last;
        private float top, path, dragged, entered;

        /// <summary>Whether it is under the water now.</summary>
        public bool Under { get; private set; }

        /// <summary>The share of its hit it carries now: its energy against what it entered the water with (1 above water).</summary>
        public float Share => Under && entered > 0.01f && flight != null ? Mathf.Clamp01(flight.m_vel.sqrMagnitude / (entered * entered)) : 1f;

        private void Awake()
        {
            (flight, view) = (GetComponent<Projectile>(), GetComponent<ZNetView>());
            last = transform.position;
        }

        private void FixedUpdate()
        {
            if (flight == null || view == null || !view.IsValid() || !view.IsOwner() || flight.m_didHit)
                return;
            Vector3 at = transform.position;
            if (Under)
                Swim(last, at);
            else
                Enter(at);
            last = at;
        }

        // Crossing the surface: where it crossed, the splash, its burst and hit effects off, its path under water begun.
        private void Enter(Vector3 at)
        {
            float surface = Floating.GetLiquidLevel(at);
            if (at.y >= surface)
                return;
            float share = last.y > at.y ? Mathf.Clamp01((last.y - surface) / (last.y - at.y)) : 0f;
            Vector3 crossed = Vector3.Lerp(last, at, share);
            (Under, top, entered, path, dragged) = (true, surface, flight!.m_vel.magnitude, 0f, 0f);
            flight.m_hitWaterEffects.Create(crossed, Quaternion.identity);
            (flight.m_spawnOnHit, flight.m_hitEffects) = (null, new EffectList());
            Swim(crossed, at);
        }

        // Under water: the path it has gone; its speed dragged down through the middle of its coming step (it goes about
        // a metre a step, so its speed in the step, and at anything it strikes there, is the speed for where it is then),
        // buoyancy against gravity; gone once slow or deep.
        private void Swim(Vector3 from, Vector3 at)
        {
            path += Vector3.Distance(from, at);
            Projectile shot = flight!;
            float ahead = Mathf.Max(dragged, path + 0.5f * shot.m_vel.magnitude * Time.fixedDeltaTime);
            shot.m_vel = shot.m_vel * KwWaterFlight.Kept(dragged, ahead) + Vector3.up * (shot.m_gravity * KwWaterFlight.Buoyancy * Time.fixedDeltaTime);
            dragged = ahead;
            if (KwWaterFlight.Done(shot.m_vel.magnitude, top - at.y))
                ZNetScene.instance.Destroy(gameObject);
        }
    }

    /// <summary>
    /// A cannonball's flight under the water, plain numbers. It keeps its speed for the first <see cref="Keep"/> metre of
    /// its path (the hollow it tears open as it goes in), then the water's drag takes it off with the path: its speed
    /// times e^(−<see cref="Drag"/> × metres), down to a fifth over the next 3.5 m, as an iron ball a hand across slows
    /// (the water's drag on it against its weight: 3 × 1000 × 0.47 ÷ (8 × 7800 × 0.06 m), about 0.4 a metre). Iron
    /// floats back an eighth of its weight (<see cref="Buoyancy"/>), so gravity still sinks it, at about 3 m/s at most. It
    /// is gone once slower than <see cref="Slow"/> m/s or <see cref="Deep"/> m under where it went in.
    /// </summary>
    public static class KwWaterFlight
    {
        public const float Keep = 1f, Drag = 0.46f, Buoyancy = 0.13f, Slow = 2f, Deep = 3f;

        /// <summary>The share of its speed kept as its path under water goes from `before` to `after` metres.</summary>
        public static float Kept(float before, float after) => Mathf.Exp(-Drag * (Mathf.Max(Keep, after) - Mathf.Max(Keep, before)));

        /// <summary>Whether a ball going so fast, so deep under where it went in, is done.</summary>
        public static bool Done(float speed, float depth) => speed < Slow || depth > Deep;
    }
}
