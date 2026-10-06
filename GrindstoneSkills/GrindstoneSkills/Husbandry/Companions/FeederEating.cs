using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Hungry animals eat from feeders. The game's <c>MonsterAI.UpdateConsumeItem</c> runs on the creature's owner (the
    /// only machine that runs its AI): every m_consumeSearchInterval (10 s) it looks for food on the ground within
    /// m_consumeSearchRange (5 m), walks there and eats it. When it found nothing and Husbandry is on, a postfix does the
    /// same with feeders:
    /// <list type="bullet">
    /// <item>At the game's search moment (its timer has just been reset to 0), a hungry creature that is tamed or being
    /// tamed (<see cref="Herd.IsBeingTamed"/>) picks the nearest loaded feeder within Feeder Range holding food it eats
    /// (<see cref="Feeders.Nearest"/>), if it has a path to the feeder's near side.</item>
    /// <item>While it has one, the postfix walks it there and returns true, as the game does while it walks to food, so
    /// the AI does nothing else that frame; then it turns to the feeder and eats one item.</item>
    /// <item>Eating: the feeder's owner removes one of that item (<see cref="Feeders.TakeOne"/>); the creature's
    /// m_onConsumedItem runs with the eaten item's prefab, as for food on the ground (Tameable resets its feeding timer
    /// there, and Husbandry's own feeding hooks see it); the game's consume effect and "consume" animation play.</item>
    /// <item>A feeder that empties or unloads, a creature that stops being hungry or finds ground food, or a walk longer
    /// than <see cref="GiveUp"/> seconds drops the target.</item>
    /// </list>
    /// Two creatures eating a feeder's last item at the same moment may both be fed, since each owner reads the feeder's
    /// inventory from its ZDO copy (accepted).
    /// </summary>
    public static class FeederEating
    {
        /// <summary>Seconds a creature keeps walking to a feeder before it gives up until the next search.</summary>
        public const float GiveUp = 30f;

        /// <summary>Metres from a feeder's centre to its side, about the barrel's radius.</summary>
        private const float FeederRadius = 0.6f;

        /// <summary>Degrees within which the creature must face the feeder to eat, as for food on the ground.</summary>
        private const float EatAngle = 20f;

        private static readonly ConditionalWeakTable<MonsterAI, Meal> Meals = new ConditionalWeakTable<MonsterAI, Meal>();

        private sealed class Meal
        {
            public Container Feeder;
            public ItemDrop Food;
            public float Since;
        }

        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateConsumeItem))]
        private static class Consume
        {
            [HarmonyPostfix]
            private static void Postfix(MonsterAI __instance, Humanoid humanoid, float dt, ref bool __result)
            {
                if (__result)
                {
                    Meals.Remove(__instance);
                    return;
                }
                if (!HusbandrySkill.Active || !Wants(__instance))
                    return;
                // A static lambda with its arguments passed in: a capturing one would allocate on every call, 20 times a
                // second for every creature, before the gate above.
                __result = HookGuard.Run("feeder eating", static call => Update(call.ai, call.humanoid, call.dt),
                    (ai: __instance, humanoid, dt), false);
            }
        }

        /// <summary>Cheap test before any work: a creature with food it eats, at the search moment or with a target.</summary>
        private static bool Wants(MonsterAI ai) =>
            ai.m_consumeItems != null && ai.m_consumeItems.Count > 0
            && (ai.m_consumeSearchTimer == 0f || Meals.TryGetValue(ai, out _));

        private static bool Update(MonsterAI ai, Humanoid humanoid, float dt)
        {
            if (!IsHungryKeptAnimal(ai))
            {
                Meals.Remove(ai);
                return false;
            }
            if (ai.m_consumeSearchTimer == 0f)
                Choose(ai);
            return Meals.TryGetValue(ai, out Meal meal) && Approach(ai, humanoid, meal, dt);
        }

        private static bool IsHungryKeptAnimal(MonsterAI ai)
        {
            Tameable tameable = ai.m_tamable;
            Character character = ai.m_character;
            return tameable != null && character != null && !character.IsDead() && tameable.IsHungry()
                && (character.IsTamed() || Herd.IsBeingTamed(tameable));
        }

        private static void Choose(MonsterAI ai)
        {
            Meals.Remove(ai);
            Container feeder = Feeders.Nearest(ai, HusbandryCompanionSettings.FeederRange.Value, out ItemDrop food);
            if (feeder != null && ai.HavePath(Spot(ai, feeder)))
                Meals.Add(ai, new Meal { Feeder = feeder, Food = food, Since = Time.time });
        }

        /// <summary>The point beside the feeder on the creature's side, where a creature of its size stands to eat.</summary>
        private static Vector3 Spot(MonsterAI ai, Container feeder)
        {
            Vector3 centre = feeder.transform.position;
            Vector3 away = ai.transform.position - centre;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.forward;
            return centre + away.normalized * (FeederRadius + ai.m_character.GetRadius());
        }

        private static bool Approach(MonsterAI ai, Humanoid humanoid, Meal meal, float dt)
        {
            if (Time.time - meal.Since > GiveUp || !Feeders.Holds(meal.Feeder, meal.Food))
            {
                Meals.Remove(ai);
                return false;
            }
            Vector3 centre = meal.Feeder.transform.position;
            if (!ai.MoveTo(dt, Spot(ai, meal.Feeder), ai.m_consumeRange, run: false))
                return true;
            // MoveTo also reports arrival when it finds no path: only a creature really beside the feeder eats.
            if (!InReach(ai, centre))
            {
                Meals.Remove(ai);
                return false;
            }
            ai.LookAt(centre);
            if (ai.IsLookingAt(centre, EatAngle))
                Eat(ai, humanoid, meal);
            return true;
        }

        private static bool InReach(MonsterAI ai, Vector3 centre) =>
            Utils.DistanceXZ(ai.transform.position, centre)
            <= FeederRadius + ai.m_character.GetRadius() + ai.m_consumeRange + 0.5f;

        private static void Eat(MonsterAI ai, Humanoid humanoid, Meal meal)
        {
            Meals.Remove(ai);
            Feeders.TakeOne(meal.Feeder, meal.Food.m_itemData.m_shared.m_name);
            ai.m_onConsumedItem?.Invoke(meal.Food);
            if (humanoid != null)
                humanoid.m_consumeItemEffects.Create(ai.transform.position, Quaternion.identity);
            if (ai.m_animator != null)
                ai.m_animator.SetTrigger("consume");
        }
    }
}
