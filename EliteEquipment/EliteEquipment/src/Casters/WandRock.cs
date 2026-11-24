using BundlePrefabs;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Greywood Wand's attack in place of the bolt (user, 2026-11-02: "black forest left click spell wand should be like a
    /// rock"): a copy of the stone a Greydwarf throws (<c>Greydwarf_throw_projectile</c>, fired by the <c>Greydwarf_throw</c>
    /// attack among its default items; a scene prefab), keeping its look (a small stone tumbling as it flies) and its own
    /// networked hit effects (<c>sfx_greydwarf_stone_hit</c>, <c>vfx_arrowhit</c>). Read live 2026-11-02, the game's stone
    /// carries no damage, area, status effect or spawn of its own; the copy is held to that (<see cref="Strip"/>), so only
    /// the attack's hit lands. The copy (<see cref="Prefab"/>, registered in the scene on every peer) is thrown like a spell
    /// (user, 2026-11-02: "it should also be thrown like a spell and shoot straight ahead"): no gravity, straight at the
    /// crosshair (<see cref="RockShot"/>), for <see cref="Seconds"/> s, with a dark green trail drawn on every machine
    /// (<see cref="RockTrail"/>). It shares its size with every machine (charged rocks are bigger, pieces smaller) and hits
    /// as the game's projectiles hit: never its owner, and of other characters only the owner's enemies (other players only
    /// with PvP on, never a tame). A charged rock breaks apart (<see cref="RockBreak"/>, <see cref="RockBurst"/>: the
    /// game's <see cref="Crack"/> and, should the workshop's burst be missing, its <see cref="Chips"/>).
    /// </summary>
    public static class WandRock
    {
        public const string Prefab = "EE_WandRock";
        public const string GameStone = "Greydwarf_throw_projectile";
        public const string GameChips = "vfx_RockHit";
        public const string GameCrack = "sfx_rock_hit";
        /// <summary>The hit width of a rock the stone's size (the game's stone hits 0.5 m wide, made for a creature's aim).</summary>
        public const float HitWidth = 0.15f;
        private const float Gravity = 0f;
        private const float Seconds = 4f;
        // As the bolt and the ice shards give (the game's stone gives 2).
        private const float Adrenaline = 1f;

        public static GameObject Rock { get; private set; }

        /// <summary>The game's stone chips (<c>vfx_RockHit</c>), or null: drawn where a rock breaks when the workshop's burst is missing.</summary>
        public static GameObject Chips { get; private set; }

        /// <summary>The game's rock crack (<c>sfx_rock_hit</c>), or null: heard where a rock breaks.</summary>
        public static GameObject Crack { get; private set; }

        /// <summary>The wand that throws the rock instead of the bolt: the Black Forest's.</summary>
        public static bool Fires(GearPiece wand) => wand == CasterGear.Greywood;

        /// <summary>A rock or one of its pieces (a copy of <see cref="Prefab"/> in the world).</summary>
        public static bool Is(Projectile projectile) => projectile != null && projectile.name.StartsWith(Prefab);

        public static void Build(GameObject game, GameObject chips, GameObject crack)
        {
            GameObject rock = PrefabBench.Copy(game, Prefab);
            Projectile projectile = rock.GetComponent<Projectile>();
            projectile.m_gravity = Gravity;
            projectile.m_drag = 0f;
            projectile.m_ttl = Seconds;
            projectile.m_rayRadius = HitWidth;
            projectile.m_adrenaline = Adrenaline;
            projectile.m_hitOwner = false;
            projectile.m_hitFriendly = false;
            Strip(projectile);
            rock.GetComponent<ZNetView>().m_syncInitialScale = true;
            rock.AddComponent<RockTrail>();
            Chips = chips;
            Crack = crack;
            Rock = rock;
        }

        /// <summary>Nothing hits but the attack's hit: no area, nothing spawned, no status effect of the stone's own.</summary>
        private static void Strip(Projectile projectile)
        {
            projectile.m_aoe = 0f;
            projectile.m_spawnOnHit = null;
            projectile.m_spawnOnTtl = false;
            projectile.m_randomSpawnOnHit.Clear();
            projectile.m_respawnItemOnHit = false;
            projectile.m_statusEffect = "";
            projectile.m_damage = new HitData.DamageTypes();
            projectile.m_bounce = false;
        }
    }
}
