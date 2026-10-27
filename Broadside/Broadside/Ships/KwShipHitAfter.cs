using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The end of a heavy shot through a launched ship (<see cref="KwShipHitRouter"/>), on the ship's owner. Stopped inside
    /// it (in a wall, in a piece built aboard): its projectile's burst there (held back on the shooter's game,
    /// <see cref="KwShipShot"/>), made as the game makes it, a networked object every peer sees, hitting the ship and
    /// whoever stands round it. Out of the far side with energy left: the same projectile flies on from past the ship's
    /// outside, slower by the energy it lost (<see cref="KwShipHitEnergy.SpeedLeft"/>) and its damage by the same share,
    /// owned by this game, so it can strike another ship or splash into the sea, and bursts where it lands.
    /// </summary>
    public static class KwShipHitAfter
    {
        private const float Clear = 0.4f, Rest = 0.15f, LeastShare = 0.25f;

        public static void End(KwShipHitRouter ship, KwShipShotData shot, Vector3 at, Vector3 way, float left, float energy)
        {
            GameObject? prefab = ZNetScene.instance != null && shot.Prefab != 0 ? ZNetScene.instance.GetPrefab(shot.Prefab) : null;
            Projectile? kind = prefab != null ? prefab.GetComponent<Projectile>() : null;
            if (kind == null)
                return;
            Character? shooter = Shooter(shot.Hit.m_attacker);
            if (left <= 0f)
                Burst(kind, at - way * Rest, way, shot.Speed, shooter);
            else
                FlyOn(prefab!, Out(ship, at, way, shot.Speed), way, KwShipHitEnergy.SpeedLeft(shot.Speed, left, energy), Mathf.Max(LeastShare, left / energy), shooter);
        }

        // The shooter's character on this game, when it is loaded here.
        private static Character? Shooter(ZDOID attacker)
        {
            GameObject? found = attacker != ZDOID.None && ZNetScene.instance != null ? ZNetScene.instance.FindInstance(attacker) : null;
            return found != null ? found.GetComponent<Character>() : null;
        }

        // The projectile's burst at a point, as the game spawns it on a hit: its spawn so many times, each set up with the
        // shooter and its way (an area hit uses the shooter alone; fragments fly off with their own speed).
        private static void Burst(Projectile kind, Vector3 at, Vector3 way, float speed, Character? shooter)
        {
            if (kind.m_spawnOnHit == null || (kind.m_spawnOnHitChance < 1f && Random.value > kind.m_spawnOnHitChance))
                return;
            for (int i = 0; i < Mathf.Max(1, kind.m_spawnCount); i++)
            {
                GameObject made = Object.Instantiate(kind.m_spawnOnHit, at, Quaternion.LookRotation(way));
                Vector3 fly = Vector3.Lerp(way, Random.onUnitSphere, kind.m_spawnProjectileRandomDir).normalized;
                float fast = kind.m_spawnProjectileNewVelocity ? Random.Range(kind.m_spawnProjectileMinVel, kind.m_spawnProjectileMaxVel) : speed;
                made.GetComponentInChildren<IProjectile>()?.Setup(shooter, fly * fast, kind.m_hitNoise, null, null, null);
            }
            kind.m_spawnOnHitEffects.Create(at, Quaternion.identity);
        }

        // The projectile again, flying on from a point with a speed, its damage by a share.
        private static void FlyOn(GameObject prefab, Vector3 from, Vector3 way, float speed, float share, Character? shooter)
        {
            GameObject made = Object.Instantiate(prefab, from, Quaternion.LookRotation(way));
            Projectile? flight = made.GetComponent<Projectile>();
            if (flight == null)
                return;
            flight.Setup(shooter, way * speed, -1f, null, null, null);
            flight.m_damage.Modify(Mathf.Clamp01(share));
        }

        // Where the shot comes out past the ship's outside: past the last thing it struck and past the hull's box along
        // its way, clear of the hull by a little more than a physics step's flight (the projectile looks back half a step).
        private static Vector3 Out(KwShipHitRouter ship, Vector3 at, Vector3 way, float speed)
        {
            Matrix4x4 pose = Matrix4x4.TRS(ship.Body!.position, ship.Body.rotation, Vector3.one);
            Vector3 local = pose.inverse.MultiplyPoint3x4(at), dir = pose.inverse.MultiplyVector(way);
            float past = Leave(ship.Bounds, local, dir);
            return pose.MultiplyPoint3x4(local + dir * past) + way * (Clear + speed * Time.fixedDeltaTime);
        }

        // How far along a way from a point inside a box (the ship's frame) it leaves the box; 0 when already out.
        private static float Leave(Bounds box, Vector3 from, Vector3 way)
        {
            float far = float.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(way[i]) < 1e-5f)
                    continue;
                float wall = way[i] > 0f ? box.max[i] : box.min[i];
                far = Mathf.Min(far, (wall - from[i]) / way[i]);
            }
            return far == float.MaxValue || !box.Contains(from) ? 0f : Mathf.Max(0f, far);
        }
    }
}
