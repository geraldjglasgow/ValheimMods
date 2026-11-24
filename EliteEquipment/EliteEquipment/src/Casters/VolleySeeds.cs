using BundlePrefabs;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The seed of Elder's Volley (<see cref="VolleyCast"/>): a copy of the Elder's own ranged shot. The Elder
    /// (<c>gd_king</c>) has four attack items, <c>gd_king_rootspawn</c> (the roots the Rootbound Wand copies),
    /// <c>gd_king_scream</c>, <c>gd_king_shoot</c> and <c>gd_king_stomp</c>; its shot <c>gd_king_shoot</c> (the
    /// <c>shoot</c> clip; pierce 35, chop 20, pickaxe 20; 25 bursts 0.1 s apart, 30 m/s, 10 degrees of spread) fires
    /// <c>gdking_root_projectile</c>, a plain networked projectile (no gravity, no area, nothing spawned on a hit, 6 s, 0.5 m
    /// wide, staying 1 s where it strikes the ground or a wall; a spore cloud; the networked <c>vfx_gdking_projectile_hit</c>
    /// and <c>sfx_gdking_projectile_hit</c> where it lands), read live 2026-11-02. The copy (<see cref="Prefab"/>,
    /// registered in the scene on every peer, no game art shipped) keeps the look, the hit effects and the stay; it flies
    /// <see cref="Seconds"/> s at the Elder's <see cref="Speed"/>, hits as wide as the bolt (<see cref="HitWidth"/>), gives
    /// the bolt's adrenaline, and hits as the bolt does: never its owner, of other characters only the owner's enemies
    /// (other players only with PvP on), and even then no damage to a friend such as a tame (<c>m_noDamageFriendly</c>).
    /// </summary>
    public static class VolleySeeds
    {
        public const string Prefab = "EE_VolleySeed";
        public const string GameSeed = "gdking_root_projectile";

        /// <summary>The Elder's shot's speed (<c>gd_king_shoot</c>'s <c>m_projectileVel</c>), m/s.</summary>
        public const float Speed = 30f;

        private const float Gravity = 0f;
        private const float Seconds = 3f;
        // The bolt's width; the Elder's 0.5 m is a creature's generous aim at a player.
        private const float HitWidth = 0.1f;
        // Each seed that hits gives the bolt's adrenaline, not the Elder's 2.
        private const float Adrenaline = 1f;

        public static GameObject Seed { get; private set; }

        /// <summary>One of the seeds (a copy of <see cref="Prefab"/> in the world).</summary>
        public static bool Is(Projectile projectile) => projectile != null && projectile.name.StartsWith(Prefab);

        public static void Build(GameObject game)
        {
            GameObject seed = PrefabBench.Copy(game, Prefab);
            Projectile projectile = seed.GetComponent<Projectile>();
            projectile.m_gravity = Gravity;
            projectile.m_ttl = Seconds;
            projectile.m_rayRadius = HitWidth;
            projectile.m_adrenaline = Adrenaline;
            projectile.m_hitOwner = false;
            projectile.m_hitFriendly = false;
            projectile.m_noDamageFriendly = true;
            projectile.m_spawnOnHit = null;
            projectile.m_aoe = 0f;
            Seed = seed;
        }
    }
}
