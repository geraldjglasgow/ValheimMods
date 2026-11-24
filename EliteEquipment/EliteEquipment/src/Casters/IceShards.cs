using BundlePrefabs;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Frostsilver Wand's attack in place of the bolt (user, 2026-11-02: a fan of sharp ice shards): a copy of the game's
    /// Staff of Frost shard (<c>staff_iceshard_projectile</c>, the bolt's base too), keeping what the bolt strips: its own
    /// look (a long thin ice sliver along its flight, cold flames, sparks and a light) and its own networked hit effect
    /// (<c>fx_iceshard_hit</c>). Read live 2026-11-02, the game's shard carries no damage of its own, no area, no status
    /// effect and nothing spawned on a hit; the copy is held to that (<see cref="Strip"/>), so only the attack's hit lands.
    /// The copy (<see cref="Prefab"/>, registered in the scene on every peer) flies straight (no gravity, the game's has 1)
    /// for <see cref="Seconds"/> s as the bolt does, <see cref="HitWidth"/> m wide, and hits as the game's projectiles hit:
    /// never its owner, and of other characters only the owner's enemies (other players only with PvP on, never a tame).
    /// The wand's attack fires it, its damage the wand's; how many and in what fan is <see cref="IceVolley"/>'s.
    /// </summary>
    public static class IceShards
    {
        public const string Prefab = "EE_WandIceShard";
        public const string GameShard = "staff_iceshard_projectile";
        private const float Gravity = 0f;
        private const float Seconds = 3f;
        // The game's shard's own width, the bolt's before its charge scales it.
        private const float HitWidth = 0.1f;

        public static GameObject Shard { get; private set; }

        /// <summary>The wand that fires the ice shards instead of the bolt: the Mountains'.</summary>
        public static bool Fires(GearPiece wand) => wand == CasterGear.Frostsilver;

        /// <summary>One of the shards (a copy of <see cref="Prefab"/> in the world).</summary>
        public static bool Is(Projectile projectile) => projectile != null && projectile.name.StartsWith(Prefab);

        public static void Build(GameObject game)
        {
            GameObject shard = PrefabBench.Copy(game, Prefab);
            Projectile projectile = shard.GetComponent<Projectile>();
            projectile.m_gravity = Gravity;
            projectile.m_ttl = Seconds;
            projectile.m_rayRadius = HitWidth;
            projectile.m_hitOwner = false;
            projectile.m_hitFriendly = false;
            Strip(projectile);
            Shard = shard;
        }

        /// <summary>Nothing hits but the attack's hit: no area, nothing spawned, no status effect of the shard's own.</summary>
        private static void Strip(Projectile projectile)
        {
            projectile.m_aoe = 0f;
            projectile.m_spawnOnHit = null;
            projectile.m_spawnOnTtl = false;
            projectile.m_randomSpawnOnHit.Clear();
            projectile.m_respawnItemOnHit = false;
            projectile.m_statusEffect = "";
            projectile.m_damage = new HitData.DamageTypes();
        }
    }
}
