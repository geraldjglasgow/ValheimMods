using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Tamed animals eat from chests (the user's request). The game's <c>MonsterAI.UpdateConsumeItem</c> runs on the
    /// creature's owner, the only machine that runs its AI: every <c>m_consumeSearchInterval</c> (10 s) a hungry creature
    /// looks for food on the ground within <c>m_consumeSearchRange</c> (5 m), walks there and eats it. When it found
    /// nothing, the postfix does the same with containers:
    /// <list type="bullet">
    /// <item>At the game's search moment (its timer has just been reset to 0) a hungry tamed creature picks the nearest
    /// usable container within Pet Chest Range holding food it eats (<see cref="PetFood"/>), if it has a path to the
    /// container's near side.</item>
    /// <item>It walks there (the postfix returns true, as the game does while it walks to food, so the AI does nothing
    /// else that frame), turns to the container and eats one item: taken out through <see cref="NearbyTake"/> (claim,
    /// remove, save), then the creature's <c>m_onConsumedItem</c> with the item's prefab, as for food on the ground
    /// (<c>Tameable</c> resets its feeding timer in the creature's ZDO there), the consume effect and the "consume"
    /// animation, which the game's synced animator shows everywhere.</item>
    /// <item>A container that empties or unloads, a creature that is fed or finds food on the ground, or a walk longer
    /// than <see cref="GiveUp"/> seconds drops the target.</item>
    /// </list>
    /// The access checks are those of the player on the machine that runs the animal, as for the other Homestead features;
    /// a dedicated server that runs one (no player near it) feeds nothing, and such an animal is not seen by anyone.
    /// </summary>
    public static class PetEating
    {
        /// <summary>Seconds a creature keeps walking to a container before it gives up until the next search.</summary>
        public const float GiveUp = 30f;

        /// <summary>Degrees within which the creature must face the container to eat, as for food on the ground.</summary>
        private const float EatAngle = 20f;

        /// <summary>Food-holding containers tried per search before the creature gives up on finding a path to one.</summary>
        private const int PathTries = 3;

        private static readonly ConditionalWeakTable<MonsterAI, Meal> Meals = new ConditionalWeakTable<MonsterAI, Meal>();

        private sealed class Meal
        {
            public Container Chest;
            public ItemDrop Food;
            public float Since;
        }

        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateConsumeItem))]
        public static class ConsumePatch
        {
            [HarmonyPostfix]
            public static void Postfix(MonsterAI __instance, Humanoid humanoid, float dt, ref bool __result)
            {
                if (__result)
                {
                    Meals.Remove(__instance);
                    return;
                }
                if (PetSettings.PetsEatFromChests.Value && Wants(__instance))
                    __result = Update(__instance, humanoid, dt);
            }
        }

        /// <summary>Cheap test before any work: a creature with food it eats, at the search moment or with a target.</summary>
        private static bool Wants(MonsterAI ai) =>
            ai.m_consumeItems != null && ai.m_consumeItems.Count > 0
            && (ai.m_consumeSearchTimer == 0f || Meals.TryGetValue(ai, out _));

        private static bool Update(MonsterAI ai, Humanoid humanoid, float dt)
        {
            if (!IsHungryPet(ai))
            {
                Meals.Remove(ai);
                return false;
            }
            if (ai.m_consumeSearchTimer == 0f)
                Choose(ai);
            return Meals.TryGetValue(ai, out Meal meal) && Approach(ai, humanoid, meal, dt);
        }

        private static bool IsHungryPet(MonsterAI ai)
        {
            Tameable tameable = ai.m_tamable;
            Character character = ai.m_character;
            return tameable != null && character != null && !character.IsDead() && character.IsTamed() && tameable.IsHungry();
        }

        /// <summary>The nearest container holding food the creature eats that it can walk to, tried up to <see cref="PathTries"/> times.</summary>
        private static void Choose(MonsterAI ai)
        {
            Meals.Remove(ai);
            int tries = 0;
            foreach (Container chest in ContainerScan.Nearby(ai.transform.position, PetSettings.PetChestRange.Value, ContainerUse.Reach))
            {
                ItemDrop food = PetFood.In(chest, ai);
                if (food == null)
                    continue;
                if (ai.HavePath(PetFood.Spot(ai, chest)))
                {
                    Meals.Add(ai, new Meal { Chest = chest, Food = food, Since = Time.time });
                    return;
                }
                if (++tries >= PathTries)
                    return;
            }
        }

        private static bool Approach(MonsterAI ai, Humanoid humanoid, Meal meal, float dt)
        {
            if (Time.time - meal.Since > GiveUp || !PetFood.StillHolds(meal.Chest, meal.Food))
            {
                Meals.Remove(ai);
                return false;
            }
            if (!ai.MoveTo(dt, PetFood.Spot(ai, meal.Chest), ai.m_consumeRange, run: false))
                return true;
            // MoveTo also reports arrival when it finds no path: only a creature really beside the container eats.
            if (!PetFood.InReach(ai, meal.Chest))
            {
                Meals.Remove(ai);
                return false;
            }
            Vector3 centre = meal.Chest.transform.position;
            ai.LookAt(centre);
            if (ai.IsLookingAt(centre, EatAngle))
                Eat(ai, humanoid, meal);
            return true;
        }

        private static void Eat(MonsterAI ai, Humanoid humanoid, Meal meal)
        {
            Meals.Remove(ai);
            if (NearbyTake.Take(new List<Container> { meal.Chest }, PetFood.Matches(meal.Food), 1) < 1)
                return;
            ai.m_onConsumedItem?.Invoke(meal.Food);
            if (humanoid != null)
                humanoid.m_consumeItemEffects.Create(ai.transform.position, Quaternion.identity);
            if (ai.m_animator != null)
                ai.m_animator.SetTrigger("consume");
            Plugin.Log.LogDebug($"OpenKeep: {Utils.GetPrefabName(ai.gameObject)} ate one {meal.Food.name} from {ContainerScan.PrefabName(meal.Chest)}");
        }
    }
}
