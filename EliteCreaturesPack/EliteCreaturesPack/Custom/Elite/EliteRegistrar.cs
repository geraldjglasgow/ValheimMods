using System;
using System.Collections.Generic;
using System.Linq;
using EliteCreaturesLink;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Elite
{
    /// <summary>
    /// Hands the checked elite lines to Elite Creatures Reborn (EliteCreaturesLink's <see cref="EliteTraits"/>), once per
    /// build and after it, when the creatures' prefabs are registered: ECR checks a portal attack against the registered
    /// prefab's items. First it clears every prefab it registered for the last world, so a definition removed or renamed
    /// since leaves nothing behind; then it registers each built creature's lines by its prefab name and logs every
    /// problem ECR answers (a mutation its limits refuse, ...) at the line's file and line. A creature left out after its
    /// own pass (it needs one that failed) is not registered. Registrations are code, not synced data: every peer makes the
    /// same ones from the same definitions, and ECR reads them on a creature's owner when it rolls.
    /// </summary>
    internal static class EliteRegistrar
    {
        /// <summary>A summon's stars when its entry gives none: the aspect's own (ECR reads any negative number so).</summary>
        private const int OwnStars = -1;

        private static readonly Dictionary<string, EliteLines> queued = new Dictionary<string, EliteLines>(StringComparer.Ordinal);

        /// <summary>The prefabs registered with ECR for the current world.</summary>
        private static readonly List<string> registered = new List<string>();

        /// <summary>A creature's checked lines, registered when the build is over.</summary>
        public static void Queue(string creature, EliteLines lines) => queued[creature] = lines;

        /// <summary>Drops what a creature of this name queued (its chain is being built again).</summary>
        public static void Forget(string creature) => queued.Remove(creature);

        /// <summary>The build is over and its prefabs registered: the last world's registrations go, this world's come.</summary>
        public static void Commit(IReadOnlyList<CustomCreature> built)
        {
            foreach (string prefab in registered)
            {
                EliteTraits.Clear(prefab);
            }
            registered.Clear();
            foreach (CustomCreature creature in built)
            {
                if (queued.TryGetValue(creature.Definition.Name, out EliteLines lines) && lines.Any)
                {
                    Register(creature.Definition.Name, lines);
                }
            }
            queued.Clear();
        }

        private static void Register(string prefab, EliteLines lines)
        {
            registered.Add(prefab);
            if (lines.Mutations != null)
            {
                Answer(lines.Mutations, EliteTraits.SetMutations(prefab, lines.Mutations.Values.ToArray()));
            }
            if (lines.Aspects != null)
            {
                Answer(lines.Aspects, EliteTraits.SetAspects(prefab, lines.Aspects.Values.ToArray()));
            }
            if (lines.PortalAttacks != null)
            {
                Answer(lines.PortalAttacks, EliteTraits.SetPortalAttacks(prefab, lines.PortalAttacks.Values.ToArray()));
            }
            if (lines.Summons != null)
            {
                RegisterSummons(prefab, lines.Summons);
            }
        }

        private static void RegisterSummons(string prefab, EliteLine<SummonEntry> line)
        {
            string[] creatures = new string[line.Values.Count];
            int[] stars = new int[line.Values.Count];
            for (int i = 0; i < creatures.Length; i++)
            {
                creatures[i] = line.Values[i].Creature;
                stars[i] = line.Values[i].Stars ?? OwnStars;
            }
            Answer(line, EliteTraits.SetSummons(prefab, creatures, stars));
        }

        /// <summary>ECR's answer to one registration: each problem a warning at the line (the creature keeps the rest).</summary>
        private static void Answer<T>(EliteLine<T> line, string[] problems)
        {
            foreach (string problem in problems)
            {
                line.Warn("Elite Creatures Reborn: " + problem);
            }
        }
    }
}
