using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// What a Devouring creature may eat. Two limits, both read from state every machine has, so the machine that runs a
    /// check - the devourer's owner choosing a target, the prey's owner resolving a bite - needs no one else:
    /// <list type="bullet">
    /// <item><b>Its size.</b> It eats only a creature whose current health is at most `max prey health` percent of its
    /// own current health (125 by default); bigger ones it neither hunts as prey nor devours. Both healths are read live, from
    /// each creature's ZDO, so a wounded devourer's reach shrinks and a hurt creature comes within it.</item>
    /// <item><b>Its appetite.</b> It eats one creature per star in its life, never fewer than `min meals` (1 by default,
    /// so an unstarred devourer still eats once), counted in its ZDO (<see cref="MealStore"/>). Once it has eaten
    /// them all it is sated: it devours nothing more and the game's own enmity rules it again.</item>
    /// </list>
    /// </summary>
    public static class DevourLimits
    {
        /// <summary>How many creatures it may devour in its life: one per star, never fewer than `min meals`.</summary>
        public static int Allowance(BiomeRules rules, CreatureTraits traits)
        {
            int floor = Mathf.Max(0, Mathf.RoundToInt(rules.PowerOf(Mutation.Devouring, Fields.MinMeals)));
            return Mathf.Max(traits.Stars, floor);
        }

        /// <summary>True once it has eaten its allowance and will devour nothing more.</summary>
        public static bool Sated(EliteController devourer) =>
            MealStore.Count(devourer.View.GetZDO()) >= Allowance(devourer.Rules, devourer.Traits);

        /// <summary>
        /// True when the prey is small enough: its current health at most `max prey health` percent of the devourer's
        /// own current health. `0` lifts the limit.
        /// </summary>
        public static bool FitsInMaw(EliteController devourer, Character prey)
        {
            float percent = devourer.Rules.PowerOf(Mutation.Devouring, Fields.MaxPreyHealth);
            return percent <= 0f || prey.GetHealth() <= percent / 100f * devourer.Creature.GetHealth();
        }

        /// <summary>Both limits at once: it may devour this prey right now, its cooldown aside.</summary>
        public static bool MayEat(EliteController devourer, Character prey) =>
            !Sated(devourer) && FitsInMaw(devourer, prey);
    }
}
