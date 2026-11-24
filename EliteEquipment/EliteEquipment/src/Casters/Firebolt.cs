using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Ashwood Wand's attack in place of the bolt (user, 2026-11-02: "need ashlands wand with firebolt as main lmb"): a
    /// copy of the Staff of Embers' fireball (<c>staff_fireball_projectile</c>, read live 2026-11-02: no damage of its own,
    /// a 3 m area, gravity 4, 20 s, 0.1 m wide, hit effect <c>fx_fireball_staff_explosion</c>, networked), keeping its look
    /// (a burning sphere, flames, a trail of flames and a light; its particles made to follow its scale, so a smaller or
    /// bigger firebolt is drawn so). The copy (<see cref="Prefab"/>, registered in the scene on every peer) flies straight
    /// (no gravity) for <see cref="Seconds"/> s as the bolt does and is single-target: no area, and the Surtling fireball's
    /// small burst where it lands (<c>vfx_FireballHit</c>, <c>sfx_imp_fireball_explode</c>, both networked) in place of the
    /// staff's explosion, which is kept as <see cref="Blast"/> for a fully charged firebolt (<see cref="FireboltShot"/>).
    /// It hits as the game's projectiles hit: never its owner, and of other characters only the owner's enemies (other
    /// players only with PvP on, never a tame); an area it is given skips buildings. Its size is shared with every machine.
    /// </summary>
    public static class Firebolt
    {
        public const string Prefab = "EE_WandFirebolt";
        public const string GameBolt = "staff_fireball_projectile";
        public const string GameSpark = "vfx_FireballHit";
        public const string GameBang = "sfx_imp_fireball_explode";
        /// <summary>The hit width of a firebolt the staff's size (the staff's own: 0.1 m).</summary>
        public const float HitWidth = 0.1f;
        private const float Gravity = 0f;
        private const float Seconds = 3f;
        // As the bolt, the ice shards and the rock give (the staff's fireball gives none).
        private const float Adrenaline = 1f;

        public static GameObject Bolt { get; private set; }

        /// <summary>The staff's own explosion, for a fully charged firebolt; empty when the game has none.</summary>
        public static EffectList Blast { get; private set; } = new EffectList();

        /// <summary>The wand that fires the firebolt instead of the bolt: the Ashlands'.</summary>
        public static bool Fires(GearPiece wand) => wand == CasterGear.Ashwood;

        /// <summary>A firebolt (a copy of <see cref="Prefab"/> in the world).</summary>
        public static bool Is(Projectile projectile) => projectile != null && projectile.name.StartsWith(Prefab);

        public static void Build(GameObject game, GameObject spark, GameObject bang)
        {
            GameObject bolt = PrefabBench.Copy(game, Prefab);
            Projectile projectile = bolt.GetComponent<Projectile>();
            projectile.m_gravity = Gravity;
            projectile.m_ttl = Seconds;
            projectile.m_rayRadius = HitWidth;
            projectile.m_adrenaline = Adrenaline;
            projectile.m_hitOwner = false;
            projectile.m_hitFriendly = false;
            projectile.m_aoeSkipWearNTear = true;
            Blast = projectile.m_hitEffects;
            projectile.m_hitEffects = Effects(spark, bang);
            Strip(projectile);
            ChargeOrb.ScaleWithParent(bolt);
            bolt.GetComponent<ZNetView>().m_syncInitialScale = true;
            Bolt = bolt;
        }

        /// <summary>Nothing hits but the attack's hit, on one target: no area, nothing spawned, no status effect of its own.</summary>
        private static void Strip(Projectile projectile)
        {
            projectile.m_aoe = 0f;
            projectile.m_hitMidFlight = false;
            projectile.m_spawnOnHit = null;
            projectile.m_spawnOnTtl = false;
            projectile.m_randomSpawnOnHit.Clear();
            projectile.m_respawnItemOnHit = false;
            projectile.m_statusEffect = "";
            projectile.m_damage = new HitData.DamageTypes();
            projectile.m_bounce = false;
        }

        private static EffectList Effects(params GameObject[] prefabs)
        {
            var effects = new List<EffectList.EffectData>();
            foreach (GameObject prefab in prefabs)
            {
                if (prefab != null)
                    effects.Add(new EffectList.EffectData { m_prefab = prefab });
            }
            return new EffectList { m_effectPrefabs = effects.ToArray() };
        }
    }
}
