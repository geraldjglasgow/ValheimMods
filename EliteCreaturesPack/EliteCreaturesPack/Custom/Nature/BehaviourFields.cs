using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `behaviour:` on the shell's mind. Any mind (<see cref="BaseAI"/>): avoid fire <c>m_avoidFire</c>, fear fire
    /// <c>m_afraidOfFire</c>, avoid water <c>m_avoidWater</c>. A monster's mind only (<see cref="MonsterAI"/>; a human has
    /// one too): flee at health <c>m_fleeIfLowHealth</c>, flee when unreachable <c>m_fleeIfHurtWhenTargetCantBeReached</c>,
    /// hunt players <c>m_enableHuntPlayer</c>, attack buildings <c>m_attackPlayerObjects</c>, chase distance
    /// <c>m_maxChaseDistance</c>, time between attacks <c>m_minAttackInterval</c>, circle before charging
    /// <c>m_circulateWhileCharging</c> (and its flying twin), circle target every / for / distance
    /// <c>m_circleTargetInterval</c>, <c>m_circleTargetDuration</c>, <c>m_circleTargetDistance</c>, starts asleep
    /// <c>m_sleeping</c> (a hit always wakes it), wakes on noise <c>m_noiseWakeup</c>, wake distance <c>m_wakeupRange</c>,
    /// and eating (<see cref="ApplyEating"/>). On an animal's mind (AnimalAI) the monster's switches are named and ignored.
    /// </summary>
    internal static class BehaviourFields
    {
        public static void Apply(CreatureBuild build)
        {
            BehaviourBlock? behaviour = build.Definition.Behaviour;
            if (behaviour == null)
            {
                return;
            }
            BaseAI ai = build.Shell.GetComponent<BaseAI>();
            if (ai == null)
            {
                build.Report.Warn("it has no mind of the game's (BaseAI), so its behaviour is ignored", "behaviour");
                return;
            }
            Assign.Set(ref ai.m_avoidFire, behaviour.AvoidFire);
            Assign.Set(ref ai.m_afraidOfFire, behaviour.FearFire);
            Assign.Set(ref ai.m_avoidWater, behaviour.AvoidWater);
            ApplyMonster(build, behaviour, ai);
        }

        private static void ApplyMonster(CreatureBuild build, BehaviourBlock behaviour, BaseAI ai)
        {
            if (ai is MonsterAI monster)
            {
                ApplyFight(behaviour, monster);
                ApplyRest(behaviour, monster);
                ApplyEating(build, behaviour, monster);
            }
            else if (NeedsMonster(behaviour))
            {
                build.Report.Warn($"its base has an {ai.GetType().Name}, which takes only avoid fire, fear fire and avoid water; the rest of behaviour is ignored", "behaviour");
            }
        }

        private static void ApplyFight(BehaviourBlock behaviour, MonsterAI ai)
        {
            Assign.Set(ref ai.m_fleeIfLowHealth, behaviour.FleeAtHealth);
            Assign.Set(ref ai.m_fleeIfHurtWhenTargetCantBeReached, behaviour.FleeWhenUnreachable);
            Assign.Set(ref ai.m_enableHuntPlayer, behaviour.HuntPlayers);
            Assign.Set(ref ai.m_attackPlayerObjects, behaviour.AttackBuildings);
            Assign.Set(ref ai.m_maxChaseDistance, behaviour.ChaseDistance);
            Assign.Set(ref ai.m_minAttackInterval, behaviour.TimeBetweenAttacks);
            Assign.Set(ref ai.m_circulateWhileCharging, behaviour.CircleBeforeCharging);
            Assign.Set(ref ai.m_circulateWhileChargingFlying, behaviour.CircleBeforeCharging);
            Assign.Set(ref ai.m_circleTargetInterval, behaviour.CircleTargetEvery);
            Assign.Set(ref ai.m_circleTargetDuration, behaviour.CircleTargetFor);
            Assign.Set(ref ai.m_circleTargetDistance, behaviour.CircleTargetDistance);
        }

        private static void ApplyRest(BehaviourBlock behaviour, MonsterAI ai)
        {
            Assign.Set(ref ai.m_sleeping, behaviour.StartsAsleep);
            Assign.Set(ref ai.m_noiseWakeup, behaviour.WakesOnNoise);
            Assign.Set(ref ai.m_wakeupRange, behaviour.WakeDistance);
        }

        /// <summary>
        /// `eats` replaces <c>MonsterAI.m_consumeItems</c> (the game matches an item on the ground by its shared name),
        /// `eat search range` is <c>m_consumeSearchRange</c>, `eat heal` is <see cref="EatHeal"/> (the game's eating heals
        /// nothing; 0 takes it off). A tameable creature eats only while hungry, and eating is what tames it.
        /// </summary>
        private static void ApplyEating(CreatureBuild build, BehaviourBlock behaviour, MonsterAI ai)
        {
            if (behaviour.Eats != null)
            {
                ai.m_consumeItems = Foods(build, behaviour.Eats);
            }
            Assign.Set(ref ai.m_consumeSearchRange, behaviour.EatSearchRange);
            if (behaviour.EatHeal == null)
            {
                return;
            }
            if (behaviour.EatHeal.Value > 0f)
            {
                Assign.Ensure<EatHeal>(build.Shell).Heal = behaviour.EatHeal.Value;
            }
            else
            {
                Assign.Remove<EatHeal>(build.Shell);
            }
        }

        private static List<ItemDrop> Foods(CreatureBuild build, List<string> names)
        {
            List<ItemDrop> foods = new List<ItemDrop>(names.Count);
            for (int i = 0; i < names.Count; i++)
            {
                GameObject? item = build.Find.Item(names[i]);
                if (item == null)
                {
                    build.Report.Fail($"unknown item '{names[i]}'", $"behaviour.eats[{i}]");
                    continue;
                }
                foods.Add(item.GetComponent<ItemDrop>());
            }
            return foods;
        }

        private static bool NeedsMonster(BehaviourBlock b) =>
            b.FleeAtHealth != null || b.FleeWhenUnreachable != null || b.HuntPlayers != null || b.AttackBuildings != null
            || b.ChaseDistance != null || b.TimeBetweenAttacks != null || b.CircleBeforeCharging != null
            || b.CircleTargetEvery != null || b.CircleTargetFor != null || b.CircleTargetDistance != null
            || b.StartsAsleep != null || b.WakesOnNoise != null || b.WakeDistance != null
            || b.Eats != null || b.EatSearchRange != null || b.EatHeal != null;
    }
}
