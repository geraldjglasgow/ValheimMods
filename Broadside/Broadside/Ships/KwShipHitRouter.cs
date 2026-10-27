using System;
using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A heavy shot through a launched ship (the user, 2026-10-26: "If the cannon is close enough to a ship, can the
    /// cannonball shoot through the ship: a hole in one side, through the ship, and a hole out the other side?"), on every
    /// peer's ship and worked out on its owner. The shooter's game sends the shot instead of the game's hit
    /// (<see cref="KwShipShot"/>: where it struck in the ship's frame, its flight, speed and range, its projectile); the
    /// owner traces its path through the ship (<see cref="KwShipHitTrace"/>: every collider it meets, nearest first, classed
    /// by kind) and hands each to its kind's taker (<see cref="Register"/>; the hull's, decks', cabins', railings', fittings',
    /// riders' and characters' are <see cref="KwShipHitTakers"/>'), each absorbing some of the energy the shot starts with
    /// (<see cref="KwShipHitEnergy"/>: its damage, speed and range). Where it stops its burst is made; if it leaves the ship
    /// with energy left it flies on as a new projectile (<see cref="KwShipHitAfter"/>). The ship's health falls once, by
    /// the takers' shares totalled, through the game's own damage path (<see cref="Health"/>). Every taker keeps its state
    /// in the ship's ZDO, which every peer draws from.
    /// </summary>
    public sealed class KwShipHitRouter : MonoBehaviour
    {
        public const string Hull = "hull", Deck = "deck", Cabin = "cabin", Rail = "rail", Mast = "mast", Yard = "yard",
            Sail = "sail", Cap = "cap", Bowsprit = "bowsprit", Rudder = "rudder", Fitting = "fitting", Rider = "rider",
            Character = "character";

        public const string ShotRpc = "KW_ShipShot";
        private static readonly Dictionary<string, Func<KwShipHitRouter, KwShipHit, float>> Takers = new Dictionary<string, Func<KwShipHitRouter, KwShipHit, float>>();
        private KwHullPartMap? map;
        private float health;

        static KwShipHitRouter() => KwShipHitTakers.Install();

        /// <summary>A kind's taker: run on the ship's owner, it returns the energy it absorbed. One a kind, the last kept.</summary>
        public static void Register(string kind, Func<KwShipHitRouter, KwShipHit, float> take) => Takers[kind] = take;

        /// <summary>A holder's colliders classed as a kind, with their piece's index in the blueprint and a part of it.</summary>
        public static void Tag(GameObject holder, string kind, int piece, int part = -1)
        {
            KwShipHitPart tag = holder.GetComponent<KwShipHitPart>() ?? holder.AddComponent<KwShipHitPart>();
            (tag.Kind, tag.Piece, tag.Part) = (kind, piece, part);
        }

        /// <summary>
        /// Whether a shot is being routed just now on this game: its takers' hits and the ship's health go the game's way
        /// then, never sent as a shot again, and the ship's wear makes no holes of its own (the takers made them).
        /// </summary>
        internal static bool Routing { get; private set; }

        /// <summary>The ship's blueprint in its own frame.</summary>
        public KwBlueprint? Blueprint { get; private set; }

        public ZNetView? View { get; private set; }

        /// <summary>Whether this peer owns the ship (its takers run here).</summary>
        public bool Owner => View != null && View.IsValid() && View.IsOwner();

        public WearNTear? Wear { get; private set; }
        public Rigidbody? Body { get; private set; }

        /// <summary>The ship's holes (made by the hull, deck and cabin takers).</summary>
        public KwHullHoles? Holes { get; private set; }

        /// <summary>The hull's box in the ship's frame, with room over it for its cabins.</summary>
        public Bounds Bounds { get; private set; }

        /// <summary>The ship's length, beam and depth together: as far as a shot could go inside it.</summary>
        public float Size { get; private set; }

        /// <summary>For the shot being routed: the share of its damage a body takes (a ball hits structures harder).</summary>
        internal float Plain { get; private set; } = 1f;

        /// <summary>Where the hull's parts lie (<see cref="KwHullPartMap"/>), made the first time it is needed.</summary>
        internal KwHullPartMap Map => map ??= KwHullPartMap.Of(Blueprint!);

        /// <summary>
        /// The drawn hull's face at a point of the world that a shot struck on a box facing a way (world): its normal there
        /// (the hull map's, <see cref="KwHullPartMap.Struck"/>), or that way when no part is near.
        /// </summary>
        public Vector3 Face(Vector3 point, Vector3 facing)
        {
            if (Body == null || Blueprint == null)
                return facing;
            Matrix4x4 pose = Matrix4x4.TRS(Body.position, Body.rotation, Vector3.one);
            Matrix4x4 into = pose.inverse;
            return Map.Struck(into.MultiplyPoint3x4(point), into.MultiplyVector(facing), out _, out _, out Vector3 normal) ? pose.MultiplyVector(normal) : facing;
        }

        /// <summary>On the owner, for the shot being routed: the ship loses this share of the shot's damage from its health.</summary>
        public void Health(KwShipHit hit, float share) => health += Mathf.Max(0f, share);

        public static KwShipHitRouter Add(Transform ship, KwBlueprint blueprint, KwHullHoles holes, KwBpHull hull)
        {
            KwShipHitRouter router = ship.gameObject.AddComponent<KwShipHitRouter>();
            router.Bounds = new Bounds(new Vector3(0f, hull.Keel + hull.Depth / 2f + 1.5f, (hull.Fore + hull.Aft) / 2f), new Vector3(hull.Beam + 0.4f, hull.Depth + 3f, hull.Length + 0.4f));
            router.Size = hull.Length + hull.Beam + hull.Depth;
            (router.Blueprint, router.Holes, router.View) = (blueprint, holes, ship.GetComponent<ZNetView>());
            (router.Wear, router.Body) = (ship.GetComponent<WearNTear>(), ship.GetComponent<Rigidbody>());
            if (router.View != null && router.View.IsValid())
                router.View.Register<ZPackage>(ShotRpc, (sender, package) => Guard.Run("Broadside ship shot", () => router.Arrived(package)));
            return router;
        }

        private void OnDestroy()
        {
            if (View != null && View.IsValid())
                View.Unregister(ShotRpc);
        }

        // A shot sent to this ship: routed on its owner, passed on once to the owner when it came here as ownership moved.
        private void Arrived(ZPackage package)
        {
            KwShipShotData? shot = KwShipShotData.Read(package);
            if (shot == null || View == null || !View.IsValid())
                return;
            if (Owner)
                Route(shot);
            else if (shot.Hops == 0)
                View.InvokeRPC(ShotRpc, shot.Write(1));
        }

        /// <summary>On the owner: a shot's path through the ship, its takers, its end and the ship's health.</summary>
        internal void Route(KwShipShotData shot)
        {
            if (Wear == null || Body == null || Blueprint == null)
                return;
            Matrix4x4 pose = Matrix4x4.TRS(Body.position, Body.rotation, Vector3.one);
            Vector3 at = pose.MultiplyPoint3x4(shot.Point), way = pose.MultiplyVector(shot.Way).normalized;
            float damage = KwShipHitEnergy.Structure(shot.Hit, Wear.m_damages);
            float energy = KwShipHitEnergy.Start(damage, shot.Speed, shot.Range);
            List<KwShipHitMeet> path = KwShipHitTrace.Through(this, at, way, shot.Hit.m_attacker);
            (health, Plain, Routing) = (0f, shot.Plain, true);
            try
            {
                (Vector3 stop, float left, bool inside) = KwShipHitWalk.Walk(path, at, way, damage, energy, shot.Hit, Take);
                health += path.Count == 0 ? 1f : 0f;
                KwShipHitAfter.End(this, shot, stop, way, inside ? 0f : left, energy);
                TakeHealth(shot.Hit, path.Count > 0 ? path[0].Point : at, way);
            }
            finally
            {
                Routing = false;
            }
        }

        // A thing's taker (the kind's default with none); a throw logged and the default taken.
        private float Take(KwShipHit hit)
        {
            if (!Takers.TryGetValue(hit.Kind, out Func<KwShipHitRouter, KwShipHit, float> take))
                return KwShipHitEnergy.Default(hit.Kind, hit.Energy);
            try
            {
                return take(this, hit);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: a shot's {hit.Kind} taker failed: {e.Message}");
                return KwShipHitEnergy.Default(hit.Kind, hit.Energy);
            }
        }

        // The takers' shares of the shot's damage off the ship's health at once, the game's way (its resistances, its
        // wreck at none left), at the shot's entry; the wear's own hole rule left out (the takers made the holes).
        private void TakeHealth(HitData shot, Vector3 at, Vector3 way)
        {
            if (health <= 0f || Wear == null)
                return;
            HitData hit = shot.Clone();
            hit.m_damage.Modify(health);
            (hit.m_point, hit.m_dir) = (at, way);
            Wear.Damage(hit);
        }
    }

    /// <summary>
    /// A shot's walk along its path through a ship (<see cref="KwShipHitRouter"/>): each thing to its taker in turn, the
    /// energy it takes off what is left, until none is. Plain, so it runs offline as on the owner.
    /// </summary>
    public static class KwShipHitWalk
    {
        /// <summary>Where the shot stopped (or last struck), what it had left, and whether it stopped inside the ship.</summary>
        public static (Vector3 Stop, float Left, bool Inside) Walk(List<KwShipHitMeet> path, Vector3 at, Vector3 way, float damage, float energy, HitData shot,
            Func<KwShipHit, float> take)
        {
            float left = energy;
            Vector3 stop = at;
            for (int i = 0; i < path.Count; i++)
            {
                KwShipHit hit = new KwShipHit(path[i], way, damage, left, i == 0, shot.Clone());
                left -= Mathf.Clamp(take(hit), 0f, left);
                stop = path[i].Point;
                if (left <= 0.01f)
                    return (stop, 0f, true);
            }
            return (stop, left, false);
        }
    }
}
