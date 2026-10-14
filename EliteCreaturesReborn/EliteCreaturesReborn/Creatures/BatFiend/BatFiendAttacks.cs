using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Bat-fiend's five attacks, copies of the game Wolf's attack item so the game's handling of a creature's weapon
    /// and its blood on the target stay the game's. Each plays its clip (the item sets the animator trigger named after
    /// it) and hits with the game's own melee sweep from the creature's root when the clip's hit event fires
    /// (<c>OnAttackTrigger</c>, 0.43 to 0.70 s in), so the owner does the hits and the game routes the damage to each
    /// victim; the wing's fan is turned to its side (<see cref="BatFiendAim"/>). The AI picks among them by range, angle
    /// and interval. The pounce's leap and the bite's lunge are carried by <see cref="BatFiendLeap"/>; the bite heals it.
    /// Ranges are in the world, at <see cref="BatFiendNumbers.Scale"/>.
    /// </summary>
    internal static class BatFiendAttacks
    {
        private sealed class Shape
        {
            public string Key = "", Clip = "", Joint = "";
            public float AiRange, AiRangeMin, AiAngle, Interval, Turn = 0.2f;
            public float Range, Angle, Yaw, Height = 0.9f, Width = 0.4f;
            public float Slash, Pierce, Blunt, Frost, Push, Stagger = 1f, Heal;
            public bool Blockable = true, Deafens, Chills;
        }

        // Fans match what the body does at the hit: each striking part's reach, its sweep and its height, measured on
        // the workshop model over the frames round the hit (out/v003/reach, out/v004/reach_bite_v004.json, and the bones
        // read in game at the hit), times the creature's scale 1.2. A fan with a Joint is cast from that bone as the clip
        // has it at the hit, so it travels with the lunge and starts at the head or chest, not the body's middle; it
        // reaches 0.5 m past the teeth or claws, since the bodies meet before they cross. Pushes near the game's own
        // beasts' (Wolf 30, Bear 35-40, Fenring claw 60).
        private static readonly Shape[] Shapes =
        {
            // Leaps 2.9 m, then the claws reach 1.3 m ahead of the chest (to 1.8 m out to the right): from 2.4 to 4 m away.
            new Shape { Key = "pounce", Clip = "RakingPounce", Joint = "Chest", AiRange = 4f, AiRangeMin = 2.4f, AiAngle = 20f, Interval = 7f, Turn = 0.1f,
                Range = 1.7f, Angle = 100f, Height = -0.2f, Width = 0.25f, Slash = 55f, Pierce = 25f, Push = 30f, Stagger = 1.5f },
            // Lunges 1.44 m, then the teeth reach 0.56 m ahead of the head bone (1.5 m from the root).
            new Shape { Key = "bite", Clip = "BloodlettingBite", Joint = "Head", AiRange = 2.6f, AiAngle = 15f, Interval = 3f, Turn = 0.1f,
                Range = 1.1f, Angle = 30f, Height = -0.25f, Width = 0.25f, Pierce = 50f, Slash = 15f, Push = 10f, Heal = BatFiendNumbers.BiteHeal },
            // The left wing sweeps from behind-left across the front to 15 degrees right: its fan turned 35 degrees left.
            new Shape { Key = "wing", Clip = "WingGuillotine", AiRange = 2.4f, AiAngle = 15f, Interval = 6f,
                Range = 2.4f, Angle = 100f, Yaw = -35f, Height = 1.55f, Width = 0.25f, Slash = 80f, Push = 30f },
            // A gust thrown on ahead of the wings' downbeat, as far as its cold wind is seen to go: 5 s of frost
            // (BatFiendChill), no frost damage (the user, 2026-10-13: its cone half as wide as the first 60 degrees).
            new Shape { Key = "gravewind", Clip = "GravewindBlast", AiRange = 5.5f, AiRangeMin = 2f, AiAngle = 10f, Interval = 12f,
                Range = 6f, Angle = 30f, Height = 1f, Width = 0.5f, Blunt = 55f, Push = 60f, Chills = true },
            // A narrow beam of sound: staggers and deafens (BatFiendScreech), pushes nobody.
            new Shape { Key = "screech", Clip = "ShatteringScreech", AiRange = 7.5f, AiAngle = 8f, Interval = 16f,
                Range = 8f, Angle = 20f, Height = 1.1f, Width = 0.5f, Blunt = 25f, Push = 0f, Stagger = 6f, Blockable = false, Deafens = true },
        };

        /// <summary>The Wolf's attack item: the first of its default items, else of its random weapons, that plays an attack.</summary>
        public static GameObject? WolfAttack(GameObject? wolf)
        {
            Humanoid? humanoid = wolf != null ? wolf.GetComponent<Humanoid>() : null;
            if (humanoid == null)
            {
                return null;
            }
            IEnumerable<GameObject> items = (humanoid.m_defaultItems ?? new GameObject[0]).Concat(humanoid.m_randomWeapon ?? new GameObject[0]);
            return items.FirstOrDefault(item => item != null && item.GetComponent<ItemDrop>() is ItemDrop drop
                && drop.m_itemData.m_shared.m_attack != null && !string.IsNullOrEmpty(drop.m_itemData.m_shared.m_attack.m_attackAnimation));
        }

        /// <summary>The five attacks, copies of the Wolf's attack item named <c>ECR_BatFiend_&lt;key&gt;</c>.</summary>
        public static GameObject[] Build(GameObject wolfAttack)
        {
            var built = new List<GameObject>();
            foreach (Shape shape in Shapes)
            {
                GameObject item = PrefabBench.Copy(wolfAttack, BatFiendPrefabs.Creature + "_" + shape.Key);
                ItemDrop.ItemData.SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
                shared.m_name = "$item_ecr_batfiend_" + shape.Key;
                Bare(item, shared);
                Swing(shared.m_attack, shape);
                Aim(shared, shape);
                Hurt(shared, shape);
                BatFiendAim.Turn(shared.m_name, shape.Yaw);
                built.Add(item);
            }
            return built.ToArray();
        }

        /// <summary>No model in the mouth (nothing to attach), no swing sounds of the Wolf's: <see cref="BatFiendVoice"/> gives it its own.</summary>
        private static void Bare(GameObject item, ItemDrop.ItemData.SharedData shared)
        {
            foreach (string part in new[] { "attach", "attach_skin", "attach_back" })
            {
                Transform? model = item.transform.Find(part);
                if (model != null)
                {
                    Object.DestroyImmediate(model.gameObject);
                }
            }
            shared.m_itemType = ItemDrop.ItemData.ItemType.TwoHandedWeapon;
            (shared.m_triggerEffect, shared.m_startEffect, shared.m_trailStartEffect) = (new EffectList(), new EffectList(), new EffectList());
            (shared.m_attackStatusEffect, shared.m_secondaryAttack) = (null, new Attack());
        }

        private static void Swing(Attack attack, Shape shape)
        {
            (attack.m_attackType, attack.m_attackAnimation, attack.m_attackChainLevels, attack.m_attackRandomAnimations) = (Attack.AttackType.Horizontal, shape.Clip, 0, 0);
            (attack.m_attackOriginJoint, attack.m_attackStamina, attack.m_speedFactor, attack.m_speedFactorRotation) = (shape.Joint, 0f, 0f, shape.Turn);
            (attack.m_attackRange, attack.m_attackHeight, attack.m_attackOffset, attack.m_attackAngle) = (shape.Range, shape.Height, 0f, shape.Angle);
            (attack.m_attackRayWidth, attack.m_multiHit, attack.m_lowerDamagePerHit, attack.m_hitTerrain) = (shape.Width, true, false, false);
            (attack.m_staggerMultiplier, attack.m_forceMultiplier, attack.m_damageMultiplier, attack.m_attackHealthReturnHit) = (shape.Stagger, 1f, 1f, shape.Heal);
            (attack.m_attackProjectile, attack.m_spawnOnTrigger, attack.m_loopingAttack, attack.m_requiresReload) = (null, null, false, false);
            (attack.m_triggerEffect, attack.m_startEffect, attack.m_trailStartEffect) = (new EffectList(), new EffectList(), new EffectList());
        }

        private static void Aim(ItemDrop.ItemData.SharedData shared, Shape shape)
        {
            (shared.m_aiAttackRange, shared.m_aiAttackRangeMin, shared.m_aiAttackInterval, shared.m_aiAttackMaxAngle) = (shape.AiRange, shape.AiRangeMin, shape.Interval, shape.AiAngle);
            (shared.m_aiPrioritized, shared.m_aiPrioritizedIfAngleCheckValid, shared.m_aiInvertAngleCheck) = (false, false, false);
            (shared.m_aiTargetType, shared.m_aiWhenWalking, shared.m_aiWhenSwiming) = (ItemDrop.ItemData.AiTarget.Enemy, true, false);
            (shared.m_aiMaxHealthPercentage, shared.m_aiMinHealthPercentage, shared.m_aiInDungeonOnly, shared.m_aiInMistOnly) = (1f, 0f, false, false);
        }

        private static void Hurt(ItemDrop.ItemData.SharedData shared, Shape shape)
        {
            shared.m_damages = new HitData.DamageTypes { m_slash = shape.Slash, m_pierce = shape.Pierce, m_blunt = shape.Blunt, m_frost = shape.Frost };
            (shared.m_attackForce, shared.m_dodgeable, shared.m_blockable) = (shape.Push, true, shape.Blockable);
            shared.m_attackStatusEffect = shape.Deafens ? BatFiendScreech.Template() : shape.Chills ? BatFiendChill.Template() : null;
            shared.m_attackStatusEffectChance = 1f;
        }

        /// <summary>
        /// The attacks' sounds: a deep bat's cry as each begins; the screech's scream instead at its hit, every layer of it
        /// at once (the bat's scream and a wraith's shriek), so it is loud where it lands.
        /// </summary>
        public static void Voice(GameObject[] attacks, GameObject? cry, GameObject?[] scream)
        {
            foreach (GameObject item in attacks)
            {
                Attack attack = item.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack;
                GameObject?[] sounds = item.name.EndsWith("_screech") ? scream : new[] { cry };
                EffectList list = item.name.EndsWith("_screech") ? attack.m_triggerEffect : attack.m_startEffect;
                list.m_effectPrefabs = sounds.Where(s => s != null).Select(s => new EffectList.EffectData { m_prefab = s }).ToArray();
            }
        }
    }
}
