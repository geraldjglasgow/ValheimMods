using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// The Crypt Executioner's six attacks, items only it carries: copies of the Skeleton's sword (so its hit effects and
    /// the game's AI handling stay the game's), the sword's model taken off (the kit's greataxe is what shows), each
    /// setting its own clip's trigger. The game's AI picks among them by their ranges and intervals: the slam, the ground
    /// scrape and the spin within reach; the two throws only from further off (their least range); the rear strike only
    /// when its target is behind (an inverted angle check) and two or more foes are near (<see cref="HeadsmanRear"/>).
    /// The scrape does not hit: its shockwave does (<see cref="HeadsmanWave"/>). Damage follows the settings.
    /// </summary>
    public static class HeadsmanAttacks
    {
        public const string RearName = "$item_ecp_headsman_rear";

        private sealed class Shape
        {
            public HeadsmanMove Move = null!;
            public string Key = "";
            public Attack.AttackType Type;
            public float Range, Angle, AiRange, AiRangeMin, Interval, MaxAngle = 20f;
            public bool Behind;
        }

        private static readonly Shape[] Shapes =
        {
            new Shape { Move = HeadsmanMoves.Slam, Key = "slam", Type = Attack.AttackType.Horizontal, Range = 3.2f, Angle = 50f, AiRange = 3f, Interval = 4f },
            new Shape { Move = HeadsmanMoves.Scrape, Key = "scrape", Type = Attack.AttackType.None, AiRange = 4f, Interval = 8f, MaxAngle = 30f },
            new Shape { Move = HeadsmanMoves.Spin, Key = "spin", Type = Attack.AttackType.Horizontal, Range = 3.6f, Angle = 360f, AiRange = 3f, Interval = 10f, MaxAngle = 180f },
            new Shape { Move = HeadsmanMoves.Hurl, Key = "hurl", Type = Attack.AttackType.Projectile, AiRange = 22f, AiRangeMin = 7f, Interval = 14f, MaxAngle = 12f },
            new Shape { Move = HeadsmanMoves.SpinThrow, Key = "spinthrow", Type = Attack.AttackType.Projectile, AiRange = 22f, AiRangeMin = 7f, Interval = 14f, MaxAngle = 12f },
            new Shape { Move = HeadsmanMoves.Rear, Key = "rear", Type = Attack.AttackType.Horizontal, Range = 3.2f, Angle = 360f, AiRange = 3.2f, Interval = 8f, MaxAngle = 120f, Behind = true },
        };

        private static readonly List<GameObject> prefabs = new List<GameObject>();

        /// <summary>The six attacks; `hurl` and `disc` are the thrown axes of the overhead throw and the spin throw.</summary>
        public static GameObject[] Build(GameObject sword, GameObject hurl, GameObject disc)
        {
            prefabs.Clear();
            foreach (Shape shape in Shapes)
            {
                GameObject item = PrefabBench.Copy(sword, "ECP_Headsman_" + shape.Key);
                ItemDrop.ItemData.SharedData shared = Shared(item);
                shared.m_name = "$item_ecp_headsman_" + shape.Key;
                Unarmed(item, shared);
                Swing(shared.m_attack, shape, shape.Move == HeadsmanMoves.Hurl ? hurl : disc);
                Aim(shared, shape);
                Apply(shared);
                prefabs.Add(item);
            }
            return prefabs.ToArray();
        }

        /// <summary>No sword in the fist and no sword's whoosh: the kit's axe and the boss's own sounds are what show.</summary>
        private static void Unarmed(GameObject item, ItemDrop.ItemData.SharedData shared)
        {
            Transform? attach = item.transform.Find("attach");
            foreach (Transform child in attach != null ? attach.Cast<Transform>().ToArray() : new Transform[0])
            {
                Object.DestroyImmediate(child.gameObject);
            }
            shared.m_itemType = ItemDrop.ItemData.ItemType.TwoHandedWeapon;
            shared.m_triggerEffect = new EffectList();
            shared.m_attack.m_triggerEffect = new EffectList();
            shared.m_attack.m_trailStartEffect = new EffectList();
            shared.m_attack.m_startEffect = new EffectList();
        }

        private static void Swing(Attack attack, Shape shape, GameObject thrown)
        {
            (attack.m_attackType, attack.m_attackAnimation, attack.m_attackChainLevels, attack.m_attackRandomAnimations) = (shape.Type, shape.Move.Clip, 0, 0);
            (attack.m_attackRange, attack.m_attackAngle, attack.m_attackHeight, attack.m_attackRayWidth) = (shape.Range, shape.Angle, 1.2f, 0.5f);
            if (shape.Type != Attack.AttackType.Projectile)
            {
                return;
            }
            (attack.m_attackProjectile, attack.m_attackOriginJoint, attack.m_attackHeight, attack.m_attackRange) = (thrown, "RightHand", 0f, 0.3f);
            (attack.m_projectileVel, attack.m_projectileVelMin, attack.m_projectileAccuracy, attack.m_projectileAccuracyMin) = (18f, 18f, 2f, 2f);
            (attack.m_useCharacterFacing, attack.m_launchAngle, attack.m_projectiles, attack.m_projectileBursts) = (false, 0f, 1, 1);
        }

        private static void Aim(ItemDrop.ItemData.SharedData shared, Shape shape)
        {
            (shared.m_aiAttackRange, shared.m_aiAttackRangeMin, shared.m_aiAttackInterval, shared.m_aiAttackMaxAngle) = (shape.AiRange, shape.AiRangeMin, shape.Interval, shape.MaxAngle);
            (shared.m_aiPrioritized, shared.m_aiPrioritizedIfAngleCheckValid, shared.m_aiInvertAngleCheck) = (false, shape.Behind, shape.Behind);
        }

        /// <summary>The numbers the settings own, onto one copy of an attack's shared data.</summary>
        public static void Apply(ItemDrop.ItemData.SharedData shared)
        {
            float slash = shared.m_name switch
            {
                "$item_ecp_headsman_slam" => HeadsmanSettings.SlamDamage,
                "$item_ecp_headsman_spin" => HeadsmanSettings.SpinDamage,
                "$item_ecp_headsman_hurl" => HeadsmanSettings.ThrowDamage,
                "$item_ecp_headsman_spinthrow" => HeadsmanSettings.ThrowDamage,
                RearName => HeadsmanSettings.RearDamage,
                _ => 0f,
            };
            shared.m_damages = new HitData.DamageTypes { m_slash = slash };
            shared.m_attackForce = shared.m_name == "$item_ecp_headsman_slam" ? 90f : 60f;
        }

        /// <summary>After a settings change: the prefabs' attacks and every loaded Executioner's own copies of them.</summary>
        public static void ApplyToAll()
        {
            foreach (GameObject item in prefabs)
            {
                Apply(Shared(item));
            }
            foreach (Character character in Character.GetAllCharacters())
            {
                if (character is Humanoid humanoid && humanoid.GetComponent<HeadsmanRig>() != null)
                {
                    humanoid.GetInventory().GetAllItems().ForEach(item => Apply(item.m_shared));
                }
            }
        }

        private static ItemDrop.ItemData.SharedData Shared(GameObject item) => item.GetComponent<ItemDrop>().m_itemData.m_shared;
    }
}
