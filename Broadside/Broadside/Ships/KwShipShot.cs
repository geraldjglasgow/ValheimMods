using HarmonyLib;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A heavy shot striking a launched ship, on the game that flies it (the shooter's: the game's projectiles hit only
    /// there, <c>Projectile.OnHit</c>). While a heavy projectile's hit on a launched ship is made, the game's hit on the
    /// ship's wear (<c>WearNTear.Damage</c>, through <see cref="KwHullDamagePatch"/>) is sent to the ship's owner as a shot
    /// instead (<see cref="KwShipShotData"/>, <see cref="KwShipHitRouter.ShotRpc"/>): where it struck and its flight in the
    /// ship's own frame (as this game sees the ship's body, so the owner finds the same place on its ship whatever the
    /// lag), its speed at impact, the range it flew, its projectile and how much of its damage a body would take. The
    /// projectile's own burst is held back for the owner to make where the shot stops (<see cref="KwShipHitAfter"/>).
    /// Runs first among the hit's patches, so its damage is read before any mod raises it against structures.
    /// </summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    public static class KwShipShot
    {
        private sealed class Flying
        {
            public Projectile Projectile = null!;
            public KwShipHitRouter Ship = null!;
            public GameObject? Burst;
            public float Plain;
            public bool Sent;
        }

        private static Flying? now;

        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void Hitting(Projectile __instance, Collider collider, bool water) => Begin(__instance, collider, water);

        [HarmonyFinalizer]
        private static void Hit(Projectile __instance) => End(__instance);

        private static void Begin(Projectile projectile, Collider? collider, bool water)
        {
            now = null;
            if (water || collider == null || !KwShipHitEnergy.Heavy(projectile.m_damage))
                return;
            GameObject? struck = Projectile.FindHitObject(collider);
            KwShipHitRouter? ship = struck != null ? struck.GetComponent<KwShipHitRouter>() : null;
            if (ship == null || ship.Blueprint == null || ship.Body == null)
                return;
            now = new Flying { Projectile = projectile, Ship = ship, Burst = projectile.m_spawnOnHit, Plain = projectile.m_damage.GetTotalDamage() };
            projectile.m_spawnOnHit = null;
        }

        private static void End(Projectile projectile)
        {
            if (now == null || now.Projectile != projectile)
                return;
            projectile.m_spawnOnHit = now.Burst;
            now = null;
        }

        /// <summary>
        /// The game's hit on a ship's wear, about to be sent: when it is the heavy shot being made, sent to the ship's owner
        /// as a shot instead (true: the game's hit is not sent).
        /// </summary>
        public static bool Send(WearNTear wear, HitData hit)
        {
            Flying? shot = now;
            if (shot == null || shot.Sent || KwShipHitRouter.Routing || wear.gameObject != shot.Ship.gameObject || shot.Ship.View == null || !shot.Ship.View.IsValid())
                return false;
            shot.Sent = true;
            Rigidbody body = shot.Ship.Body!;
            Matrix4x4 into = Matrix4x4.TRS(body.position, body.rotation, Vector3.one).inverse;
            Projectile p = shot.Projectile;
            KwShipShotData data = new KwShipShotData
            {
                Hit = hit, Point = into.MultiplyPoint3x4(hit.m_point), Way = into.MultiplyVector(p.m_vel.sqrMagnitude > 1e-4f ? p.m_vel : hit.m_dir).normalized,
                Speed = p.m_vel.magnitude, Range = Vector3.Distance(p.m_startPoint, hit.m_point), Prefab = Prefab(p),
                Plain = Mathf.Clamp01(shot.Plain / Mathf.Max(0.01f, hit.GetTotalDamage())),
            };
            shot.Ship.View.InvokeRPC(KwShipHitRouter.ShotRpc, data.Write(0));
            return true;
        }

        private static int Prefab(Projectile p)
        {
            ZDO? zdo = p.m_nview != null && p.m_nview.IsValid() ? p.m_nview.GetZDO() : null;
            return zdo != null ? zdo.GetPrefab() : 0;
        }
    }

    /// <summary>
    /// A heavy shot as the shooter's game sends it to a launched ship's owner (<see cref="KwShipShot"/>): the game's hit as
    /// it was made (its damage raised against structures, before the ship's resistances), where it struck and its flight
    /// in the ship's frame, its speed at impact (m/s), the range it flew (m), its projectile prefab's hash, the share of the
    /// hit's damage a body takes (a ball hits structures harder than people), and how many times it was passed on.
    /// </summary>
    public sealed class KwShipShotData
    {
        private const int Version = 1;

        public HitData Hit = new HitData();
        public Vector3 Point, Way;
        public float Speed, Range, Plain = 1f;
        public int Prefab, Hops;

        public ZPackage Write(int hops)
        {
            ZPackage package = new ZPackage();
            package.Write(Version);
            Hit.Serialize(ref package);
            package.Write(Point);
            package.Write(Way);
            package.Write(Speed);
            package.Write(Range);
            package.Write(Plain);
            package.Write(Prefab);
            package.Write(hops);
            return package;
        }

        /// <summary>A shot from its package; null when it is not one this version reads.</summary>
        public static KwShipShotData? Read(ZPackage package)
        {
            if (package.ReadInt() != Version)
                return null;
            KwShipShotData shot = new KwShipShotData();
            shot.Hit.Deserialize(ref package);
            (shot.Point, shot.Way) = (package.ReadVector3(), package.ReadVector3());
            (shot.Speed, shot.Range, shot.Plain) = (package.ReadSingle(), package.ReadSingle(), package.ReadSingle());
            (shot.Prefab, shot.Hops) = (package.ReadInt(), package.ReadInt());
            return shot;
        }
    }
}
