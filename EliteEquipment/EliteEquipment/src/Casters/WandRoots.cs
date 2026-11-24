using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Rootbound Wand's attack in place of the bolt (user, 2026-11-02: "the root wand attack should be the elder like
    /// root attack, but a tap should only have 1 root come out. full charge should have like 5 come out"): the Elder's own
    /// root attack, copied. The Elder (<c>gd_king</c>) fires <c>spawn_roots</c> from its <c>gd_king_rootspawn</c> attack, a
    /// <c>SpawnAbility</c> that is not networked and raises <c>TentaRoot</c>s (networked creatures that rise, whip and fade)
    /// around its target; the wand's primary attack fires a copy of it (<see cref="Cast"/>) the same way, and the copy raises
    /// copies of the root (<see cref="Root"/>, registered in the scene on every peer): the root's model, animator and clips
    /// with the creature taken out (no AI, health, collider or body, not saved), a <see cref="WandRoot"/> that plays it, the
    /// clip's <c>Hit</c> caught by <see cref="RootHit"/>, and an inert <c>Aoe</c> ahead of it, set off when the whip lands.
    /// How many, where and with what damage is <see cref="RootCast"/>'s.
    /// </summary>
    public static class WandRoots
    {
        public const string RootPrefab = "EE_WandRoot";
        public const string CastPrefab = "EE_WandRoots";
        public const string RisePrefab = "EE_fx_wandroot_spawn";
        public const string GameRoot = "TentaRoot";
        public const string GameElder = "gd_king";
        private const string GameAttack = "gd_king_rootspawn";
        // The strike: a ball this wide, ahead of the root and up from its foot, where the whip comes down.
        private const float StrikeRadius = 2f;
        private static readonly Vector3 StrikeAt = new Vector3(0f, 0.8f, 1f);
        // The area never goes off on its own timer (the root sets it off, WandRoot.Strike).
        private const float Never = 1e6f;
        // The ground is found from this far above the aimed point (the Elder's 1000 m would raise a root on a roof).
        private const int GroundFrom = 2;

        public static GameObject Root { get; private set; }
        public static GameObject Cast { get; private set; }

        /// <summary>The root's rising effect without the game's camera shake (a networked effect, registered on every peer).</summary>
        public static GameObject Rise { get; private set; }

        /// <summary>The root's fade as it goes: the game's root's death effect (networked, made on the owner only).</summary>
        public static EffectList Fade { get; private set; } = new EffectList();

        /// <summary>The wand that raises roots instead of firing the bolt: the Swamp's.</summary>
        public static bool Raises(GearPiece wand) => wand == CasterGear.Rootbound;

        public static void Build(GameObject root, GameObject elder)
        {
            Character body = root.GetComponent<Character>();
            Fade = body != null ? body.m_deathEffects : new EffectList();
            Root = BuildRoot(root);
            Cast = BuildCast(Spawner(elder), Root);
        }

        /// <summary>The Elder's root spawner: the projectile of its root attack, one of its default items.</summary>
        private static GameObject Spawner(GameObject elder)
        {
            foreach (GameObject item in elder.GetComponent<Humanoid>().m_defaultItems)
            {
                if (item != null && item.name == GameAttack)
                    return item.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_attackProjectile
                        ?? throw new InvalidOperationException($"{GameAttack} fires nothing");
            }
            throw new InvalidOperationException($"the Elder has no {GameAttack}");
        }

        private static GameObject BuildRoot(GameObject game)
        {
            GameObject root = PrefabBench.Copy(game, RootPrefab);
            Strip(root);
            root.GetComponent<ZNetView>().m_persistent = false;
            root.AddComponent<WandRoot>();
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null)
                animator.gameObject.AddComponent<RootHit>();
            AddStrike(root);
            return root;
        }

        /// <summary>Everything that made it a creature goes: every script but its network view, its colliders, its body.</summary>
        private static void Strip(GameObject root)
        {
            foreach (MonoBehaviour part in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(part is ZNetView))
                    Object.DestroyImmediate(part);
            }
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body != null)
                Object.DestroyImmediate(body);
        }

        /// <summary>The strike area: the caster's enemies only (no owner, no other player, no friend, no building).</summary>
        private static void AddStrike(GameObject root)
        {
            var at = new GameObject("strike");
            at.transform.SetParent(root.transform, false);
            at.transform.localPosition = StrikeAt;
            Aoe aoe = at.AddComponent<Aoe>();
            aoe.m_hitOwner = false;
            aoe.m_hitSame = false;
            aoe.m_hitFriendly = false;
            aoe.m_hitEnemy = true;
            aoe.m_hitCharacters = true;
            aoe.m_hitProps = false;
            aoe.m_hitTerrain = false;
            aoe.m_radius = StrikeRadius;
            aoe.m_ttl = 0f;
            aoe.m_hitInterval = 0f;
            aoe.m_activationDelay = Never;
        }

        /// <summary>The Elder's rising effect shakes the screen of everyone within 50 m (a CamShaker): ours is a copy without it.</summary>
        private static void Quieten(SpawnAbility ability)
        {
            EffectList.EffectData[] effects = ability.m_spawnEffects?.m_effectPrefabs;
            if (effects == null || effects.Length == 0 || effects[0].m_prefab == null)
                return;
            Rise = PrefabBench.Copy(effects[0].m_prefab, RisePrefab);
            foreach (CamShaker shaker in Rise.GetComponentsInChildren<CamShaker>(true))
                Object.DestroyImmediate(shaker);
            effects[0] = new EffectList.EffectData { m_prefab = Rise, m_enabled = true, m_attach = effects[0].m_attach,
                m_inheritParentRotation = effects[0].m_inheritParentRotation, m_inheritParentScale = effects[0].m_inheritParentScale,
                m_randomRotation = effects[0].m_randomRotation, m_scale = effects[0].m_scale, m_variant = effects[0].m_variant };
        }

        private static GameObject BuildCast(GameObject game, GameObject root)
        {
            GameObject cast = PrefabBench.Copy(game, CastPrefab);
            SpawnAbility ability = cast.GetComponent<SpawnAbility>();
            ability.m_spawnPrefab = new[] { root };
            ability.m_spawnOnAwake = false;
            ability.m_targetType = SpawnAbility.TargetType.Position;
            ability.m_spawnAtTarget = true;
            ability.m_circleSpawn = false;
            ability.m_maxSpawned = 0;
            ability.m_getSolidHeightMargin = GroundFrom;
            ability.m_alertSpawnedCreature = false;
            ability.m_randomYRotation = false;
            ability.m_commandOnSpawn = false;
            ability.m_wakeUpAnimation = false;
            ability.m_levelUpSettings = new List<SpawnAbility.LevelUpSettings>();
            ability.m_copySkill = Skills.SkillType.None;
            ability.m_aoePrefab = null;
            Quieten(ability);
            cast.AddComponent<RootCast>();
            return cast;
        }
    }
}
