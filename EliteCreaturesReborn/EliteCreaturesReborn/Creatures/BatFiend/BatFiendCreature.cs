using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Bat-fiend, <c>ECR_BatFiend</c>: a copy of the game's Wolf (a ground beast on four limbs), so it keeps the
    /// Wolf's character, senses, hit effects, footstep effects and AI, wearing the workshop's Bat-fiend
    /// (<see cref="BatFiendBody"/>) at <see cref="BatFiendNumbers.Scale"/>. One of Hel's creatures: Undead, 800 health,
    /// no armour and no resistance (only weak to spirit), it fights with its five attacks (<see cref="BatFiendAttacks"/>) and
    /// nothing of the Wolf's, never flees, cannot be tamed or bred, eats nothing, speaks with a deep bat's voice
    /// (<see cref="BatFiendVoice"/>) and leaves its own corpse (<see cref="BatFiendCorpse"/>). Spawnable only (`spawn`,
    /// `elite spawn`): in no spawn list, raid or arena wave yet.
    /// </summary>
    internal static class BatFiendCreature
    {
        /// <summary>What the game Wolf lacks that the Bat-fiend is built from, or null when it has everything.</summary>
        public static string? Missing(GameObject? wolf, GameObject? attack)
        {
            if (wolf == null)
            {
                return "is missing";
            }
            var lacks = new List<string>();
            if (wolf.GetComponent<Humanoid>() == null) lacks.Add("Humanoid");
            if (wolf.GetComponent<MonsterAI>() == null) lacks.Add("MonsterAI");
            if (wolf.GetComponent<CapsuleCollider>() == null) lacks.Add("CapsuleCollider");
            if (wolf.GetComponent<ZNetView>() == null) lacks.Add("ZNetView");
            if (wolf.GetComponent<ZSyncAnimation>() == null) lacks.Add("ZSyncAnimation");
            if (attack == null) lacks.Add("an attack item");
            return lacks.Count == 0 ? null : "lacks " + string.Join(", ", lacks);
        }

        public static GameObject Build(GameObject wolf, GameObject[] attacks, GameObject corpse, ZNetScene scene)
        {
            GameObject creature = PrefabBench.Copy(wolf, BatFiendPrefabs.Creature);
            creature.transform.localScale = Vector3.one * BatFiendNumbers.Scale;
            BatFiendBody.Wear(creature);
            var humanoid = creature.GetComponent<Humanoid>();
            var ai = creature.GetComponent<MonsterAI>();
            Arm(humanoid, attacks);
            Body(humanoid);
            Mind(ai);
            Untame(creature);
            Sync(creature.GetComponent<ZSyncAnimation>());
            BatFiendLoot.Fill(creature.GetComponent<CharacterDrop>(), scene);
            BatFiendVoice.Dress(humanoid, ai);
            Die(humanoid, corpse);
            creature.AddComponent<BatFiendLeap>();
            creature.AddComponent<BatFiendEffects>();
            return creature;
        }

        private static void Arm(Humanoid humanoid, GameObject[] attacks)
        {
            humanoid.m_defaultItems = attacks;
            humanoid.m_randomWeapon = new GameObject[0];
            humanoid.m_randomArmor = new GameObject[0];
            humanoid.m_randomShield = new GameObject[0];
            humanoid.m_randomSets = new Humanoid.ItemSet[0];
            humanoid.m_randomItems = new Humanoid.RandomItem[0];
        }

        /// <summary>Its name, health, pace and hide: Hel's, so Undead (friends with Hel's other dead, foes of the living).</summary>
        private static void Body(Humanoid humanoid)
        {
            (humanoid.m_name, humanoid.m_group, humanoid.m_faction, humanoid.m_boss) = ("$enemy_ecr_batfiend", "", Character.Faction.Undead, false);
            (humanoid.m_health, humanoid.m_deathAnimation) = (BatFiendNumbers.Health, false);
            (humanoid.m_walkSpeed, humanoid.m_speed, humanoid.m_runSpeed) = (BatFiendNumbers.WalkSpeed, BatFiendNumbers.WalkSpeed, BatFiendNumbers.RunSpeed);
            (humanoid.m_turnSpeed, humanoid.m_runTurnSpeed) = (BatFiendNumbers.TurnSpeed, BatFiendNumbers.RunTurnSpeed);
            humanoid.m_damageModifiers = new HitData.DamageModifiers { m_spirit = HitData.DamageModifier.Weak };
        }

        /// <summary>Wanders, sees far, fights to the death, fears no fire, eats nothing, paths as a horse-sized body.</summary>
        private static void Mind(MonsterAI ai)
        {
            (ai.m_fleeIfLowHealth, ai.m_fleeIfHurtWhenTargetCantBeReached, ai.m_fleeIfNotAlerted, ai.m_fleeInLava) = (0f, false, false, false);
            (ai.m_afraidOfFire, ai.m_avoidFire, ai.m_aggravatable, ai.m_enableHuntPlayer) = (false, false, false, false);
            ai.m_consumeItems = new List<ItemDrop>();
            (ai.m_circleTargetInterval, ai.m_circulateWhileCharging, ai.m_minAttackInterval) = (0f, false, BatFiendNumbers.MinAttackInterval);
            (ai.m_viewRange, ai.m_alertRange) = (BatFiendNumbers.ViewRange, BatFiendNumbers.AlertRange);
            ai.m_pathAgentType = Pathfinding.AgentType.HorseSize;
        }

        /// <summary>Nothing of the Wolf's taming, breeding or growing up.</summary>
        private static void Untame(GameObject creature)
        {
            foreach (System.Type type in new[] { typeof(Pet), typeof(Procreation), typeof(Growup), typeof(Tameable) })
            {
                Component? part = creature.GetComponent(type);
                if (part != null)
                {
                    Object.DestroyImmediate(part);
                }
            }
        }

        /// <summary>Other players' copies play the same locomotion: the blend tree's floats travel with the creature.</summary>
        private static void Sync(ZSyncAnimation? sync)
        {
            if (sync == null)
            {
                return;
            }
            foreach (string name in new[] { "forward_speed", "sideway_speed", "turn_speed" })
            {
                if (!sync.m_syncFloats.Contains(name))
                {
                    sync.m_syncFloats.Add(name);
                }
            }
        }

        /// <summary>The Wolf's death without its corpse and its cry: the Bat-fiend's death cry, then its own corpse.</summary>
        private static void Die(Humanoid humanoid, GameObject corpse)
        {
            BatFiendVoice.Swap(humanoid.m_deathEffects, BatFiendVoice.Of("death"));
            List<EffectList.EffectData> effects = humanoid.m_deathEffects.m_effectPrefabs
                .Where(effect => effect.m_prefab != null && effect.m_prefab.GetComponent<Ragdoll>() == null).ToList();
            effects.Add(BatFiendCorpse.Effect(corpse));
            humanoid.m_deathEffects.m_effectPrefabs = effects.ToArray();
        }
    }
}
