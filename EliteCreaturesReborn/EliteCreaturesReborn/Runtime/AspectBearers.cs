using System.Collections.Generic;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The creatures loaded on this machine that carry an aspect without being bosses: another mod's creatures given
    /// aspects through the API (<see cref="Traits.Registrations"/>), their twins, partners and Phantom copies. The attack
    /// patches that hand a blow or a throw to an aspect (Colossal, Brutal, Portalbound) see every character's attacks and
    /// pass over all but bosses at their first test; <see cref="Carries"/> keeps that test as cheap for everyone else, one
    /// count test while no such creature is loaded. Filled on every machine by <see cref="AspectInstaller"/> as it applies
    /// the traits loaded from the ZDO, and emptied as the controller is destroyed with its creature.
    /// </summary>
    public static class AspectBearers
    {
        private static readonly HashSet<Character> NotBosses = new HashSet<Character>();

        /// <summary>True for a boss, and for a creature listed here: the characters an aspect's attack hook can concern.</summary>
        public static bool Carries(Character character) =>
            character.IsBoss() || (NotBosses.Count != 0 && NotBosses.Contains(character));

        public static void Track(Character character)
        {
            if (!character.IsBoss())
            {
                NotBosses.Add(character);
            }
        }

        // `is not null` rather than Unity's ==: the creature may already read as destroyed here, and its entry must go.
        public static void Forget(Character character)
        {
            if (character is not null)
            {
                NotBosses.Remove(character);
            }
        }
    }
}
