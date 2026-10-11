using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The one roll that gives a fresh creature its traits, boss or not, whatever spawned it - the wild, a raid, the game's
    /// spawn command, another mod. A boss draws its stars from the boss table and its aspects (<see cref="BossDraw"/>);
    /// every other creature its stars and mutations from its biome's rules and the world tier (<see cref="TraitRoller"/>).
    /// What another mod registered for the prefab (<see cref="Registrations"/>) goes on top: fixed mutations replace the
    /// random mutation roll - the stars stay as rolled, and a boss carries them too - and registered aspects are rolled
    /// for a creature that is not a boss as well, because the list is the prefab's own rotation: the boss gate does not
    /// apply to a registered prefab. Owner only, like every roll: the caller writes the result to the ZDO. The raids call
    /// it for their raiders and add their own stars after.
    /// </summary>
    internal static class CreatureRoll
    {
        public static CreatureTraits For(Character creature, Heightmap.Biome biome)
        {
            string prefab = Utils.GetPrefabName(creature.gameObject);
            if (creature.IsBoss())
            {
                // a registered boss's aspects come through its draw (AspectRoller reads the registration)
                return WithMutations(creature, prefab, BossDraw.Roll(prefab).ToTraits());
            }
            RuleSet rules = RuleState.Active;
            CreatureTraits traits = TraitRoller.Roll(rules.For(biome, prefab), rules, WorldTier.Current(), biome,
                MutationBars.Of(creature)); // a large body never rolls Gilded or Relentless, a Deathsquito never Cloaked
            return WithAspects(prefab, WithMutations(creature, prefab, traits));
        }

        /// <summary>A boss an altar summons: the stars and aspects locked at the offering, with its registered mutations.</summary>
        public static CreatureTraits FromDraw(Character creature, BossDraw draw) =>
            WithMutations(creature, Utils.GetPrefabName(creature.gameObject), draw.ToTraits());

        /// <summary>
        /// The registered mutations this creature may carry, packed like a trait mask; 0 for an unregistered prefab. For a
        /// creature whose traits are forced on it (a Summoner's adds), which still wears its kind's fixed mutations.
        /// </summary>
        public static int FixedMutations(Character creature) =>
            RegisteredMutations.For(creature, Utils.GetPrefabName(creature.gameObject));

        private static CreatureTraits WithMutations(Character creature, string prefab, CreatureTraits traits)
        {
            if (Registrations.MutationsOf(prefab) != 0)
            {
                traits.Mask = RegisteredMutations.For(creature, prefab); // in place of the random roll, ECR's limits kept
            }
            return traits;
        }

        private static CreatureTraits WithAspects(string prefab, CreatureTraits traits)
        {
            if (Registrations.AspectsOf(prefab) != null)
            {
                BossAspects aspects = AspectRoller.RollBoss(prefab, null);
                traits.Aspect = aspects.Headline;
                traits.ExtraAspects = aspects.Extras;
            }
            return traits;
        }
    }
}
