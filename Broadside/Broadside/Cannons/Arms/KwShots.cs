using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The weapons' projectile prefabs, built once from their shots when the network scene first wakes and registered
    /// every time it wakes, on every peer, so a projectile one peer launches is the same prefab on all (the game's own
    /// networked projectiles: the launching peer flies it, the others see it through its ZDO). Each is a copy of the
    /// shot's game projectile (its look, trail and hit effects) set to fly and hit as the shot says, spawning where it
    /// lands its burst: fragments (a shot of their own) or an area hit (a copy of a game area effect). A shot that dives
    /// (<see cref="KwShot.Dives"/>) is not stopped at the water's surface by the game; <see cref="KwShotWater"/> flies it on under.
    /// </summary>
    public static class KwShots
    {
        public const string Prefix = "BS_shot_";
        private static readonly Dictionary<string, GameObject> Shots = new Dictionary<string, GameObject>();
        private static readonly List<GameObject> All = new List<GameObject>();

        /// <summary>The shot's projectile prefab; null before the scene woke or when its game projectile is missing.</summary>
        public static GameObject? Prefab(KwShot shot) => Shots.TryGetValue(shot.Name, out GameObject made) ? made : null;

        /// <summary>Builds the shots not built yet, then registers every shot and burst prefab with this scene.</summary>
        public static void Install(ZNetScene scene, IEnumerable<KwShot> shots)
        {
            foreach (KwShot shot in shots)
                Make(scene, shot);
            foreach (GameObject prefab in All)
                NetPrefabs.Register(scene, prefab);
        }

        private static GameObject? Make(ZNetScene scene, KwShot shot)
        {
            if (Shots.TryGetValue(shot.Name, out GameObject made))
                return made;
            GameObject? game = scene.GetPrefab(shot.Projectile);
            if (game == null || game.GetComponent<Projectile>() == null)
            {
                Log.Warn($"Broadside cannons: shot {shot.Name} has no projectile: the game has no {shot.Projectile}.");
                return null;
            }
            GameObject copy = PrefabBench.Copy(game, shot.Prefab);
            Shots[shot.Name] = copy;
            All.Add(copy);
            Dress(scene, copy, shot);
            shot.Finish?.Invoke(copy);
            return copy;
        }

        // The copy made the shot's: its flight and hit, harder on structures, its splash, look, way under the water and burst.
        private static void Dress(ZNetScene scene, GameObject copy, KwShot shot)
        {
            Projectile projectile = copy.GetComponent<Projectile>();
            // fired from a muzzle: the game's hit test from the archer's body would run through the cannon and hit it
            projectile.m_doOwnerRaytest = false;
            Fly(projectile, shot);
            KwShotHits.Set(copy, shot.Structures);
            Splash(scene, projectile, shot);
            if (shot.Wears.Length > 0)
                KwShotLook.Wear(copy, shot.Wears);
            if (shot.Dives)
                copy.AddComponent<KwShotWater>();
            if (shot.Burst != null)
                Burst(scene, projectile, shot.Name, shot.Burst);
        }

        // The flight and the hit; a fragment, launched with no hit data of its own, hits with these.
        private static void Fly(Projectile p, KwShot shot)
        {
            (p.m_gravity, p.m_ttl, p.m_drag, p.m_aoe, p.m_hitMidFlight) = (shot.Gravity, shot.Life, 0f, 0f, false);
            (p.m_damage, p.m_attackForce, p.m_statusEffect, p.m_canHitWater) = (shot.Damage, shot.Push, shot.Status, shot.HitsWater && !shot.Dives);
            (p.m_hitOwner, p.m_hitFriendly, p.m_bounce, p.m_respawnItemOnHit, p.m_spawnOnTtl) = (false, false, false, false, false);
            p.m_spawnOnHit = null;
            p.m_randomSpawnOnHit = new List<GameObject>();
        }

        // The splash where it meets the sea: game effects the projectile's peer creates, the others see.
        private static void Splash(ZNetScene scene, Projectile p, KwShot shot)
        {
            List<EffectList.EffectData> effects = new List<EffectList.EffectData>();
            foreach (string name in shot.Splash)
            {
                GameObject? effect = scene.GetPrefab(name);
                if (effect != null)
                    effects.Add(new EffectList.EffectData { m_prefab = effect });
                else
                    Log.Warn($"Broadside cannons: shot {shot.Name} has no splash {name}: the game has no such effect.");
            }
            if (effects.Count > 0)
                p.m_hitWaterEffects = new EffectList { m_effectPrefabs = effects.ToArray() };
        }

        private static void Burst(ZNetScene scene, Projectile p, string name, KwBurst burst)
        {
            bool fragments = burst.Fragments != null;
            GameObject? spawn = fragments ? Make(scene, burst.Fragments!) : Area(scene, name, burst);
            if (spawn == null)
                return;
            (p.m_spawnOnHit, p.m_spawnOnHitChance, p.m_spawnCount) = (spawn, 1f, fragments ? Mathf.Max(1, burst.FragmentCount) : 1);
            (p.m_spawnOnTerrain, p.m_spawnOnWearNTear, p.m_spawnOnCharacters, p.m_staticHitOnly, p.m_groundHitOnly) = (true, true, true, false, false);
            (p.m_projectilesInheritHitData, p.m_onlySpawnedProjectilesDealDamage, p.m_divideDamageBetweenProjectiles) = (false, false, false);
            (p.m_spawnProjectileNewVelocity, p.m_spawnProjectileHemisphereDir, p.m_projectileReturn) = (fragments, true, false);
            (p.m_spawnProjectileMinVel, p.m_spawnProjectileMaxVel, p.m_spawnProjectileRandomDir) = (burst.FragmentSpeedMin, burst.FragmentSpeedMax, burst.FragmentScatter);
        }

        // A copy of a game area effect hitting with the burst's own numbers (the shot's hit data is not passed on).
        private static GameObject? Area(ZNetScene scene, string name, KwBurst burst)
        {
            GameObject? game = scene.GetPrefab(burst.Area);
            if (game == null || game.GetComponent<Aoe>() == null)
            {
                Log.Warn($"Broadside cannons: shot {name} bursts in nothing: the game has no area effect {burst.Area}.");
                return null;
            }
            GameObject copy = PrefabBench.Copy(game, KwBurst.Prefix + name);
            All.Add(copy);
            Aoe area = copy.GetComponent<Aoe>();
            (area.m_useAttackSettings, area.m_useCollider, area.m_radius, area.m_damage) = (false, null, burst.Radius, burst.Damage);
            (area.m_statusEffect, area.m_attackForce, area.m_hitFriendly, area.m_hitOwner) = (burst.Status, burst.Push, burst.HitsFriends, false);
            return copy;
        }
    }
}
