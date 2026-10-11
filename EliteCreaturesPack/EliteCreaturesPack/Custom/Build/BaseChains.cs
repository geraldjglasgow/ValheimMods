using System;
using System.Collections.Generic;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Definitions;
using EliteCreaturesPack.Custom.Humans;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// Decides which definitions can be built and from what. A name that is already a prefab (of the game, a mod or this
    /// mod) is refused. Every switched-on creature gets its <see cref="CreatureChain"/> by following its base through the
    /// other custom definitions (switched-off ones included: they can still be a base) to the first base that is not
    /// custom; that root must be a creature prefab, or <c>Human</c>. A loop of bases (A on B, B on A) refuses every
    /// creature in it. Refusals are logged with the file and line.
    /// </summary>
    internal static class BaseChains
    {
        public static List<CreatureChain> Resolve(ZNetScene scene, IReadOnlyList<CreatureDefinition> definitions)
        {
            Dictionary<string, CreatureDefinition> customs = Accept(scene, definitions);
            List<CreatureChain> chains = new List<CreatureChain>();
            foreach (CreatureDefinition definition in definitions)
            {
                if (!definition.Enabled || !customs.TryGetValue(definition.Name, out CreatureDefinition accepted) || accepted != definition)
                {
                    continue;
                }
                CreatureChain? chain = Walk(definition, customs);
                if (chain != null && RootIsCreature(scene, chain))
                {
                    chains.Add(chain);
                }
            }
            return chains;
        }

        /// <summary>The definitions whose name is free, by name.</summary>
        private static Dictionary<string, CreatureDefinition> Accept(ZNetScene scene, IReadOnlyList<CreatureDefinition> definitions)
        {
            Dictionary<string, CreatureDefinition> accepted = new Dictionary<string, CreatureDefinition>(StringComparer.Ordinal);
            foreach (CreatureDefinition definition in definitions)
            {
                if (scene.HasPrefab(definition.Name.GetStableHashCode())
                    || string.Equals(definition.Name, HumanBody.Base, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Error($"{BuildReport.Describe(definition, "name")}: the name is already a prefab of the game or a mod; "
                        + "pick another. The creature is left out.");
                    continue;
                }
                accepted[definition.Name] = definition;
            }
            return accepted;
        }

        private static CreatureChain? Walk(CreatureDefinition creature, Dictionary<string, CreatureDefinition> customs)
        {
            List<CreatureDefinition> links = new List<CreatureDefinition> { creature };
            CreatureDefinition current = creature;
            while (customs.TryGetValue(current.Base, out CreatureDefinition next))
            {
                if (links.Contains(next))
                {
                    Log.Error($"{BuildReport.Describe(creature, "base")}: its bases go round in a loop ({Names(links)}, "
                        + $"{next.Name}). The creature is left out.");
                    return null;
                }
                links.Insert(0, next);
                current = next;
            }
            return new CreatureChain(creature, links, current.Base, current.BaseIsHuman);
        }

        private static bool RootIsCreature(ZNetScene scene, CreatureChain chain)
        {
            if (chain.Human)
            {
                return true;
            }
            GameObject? root = scene.GetPrefab(chain.RootBase);
            Character? character = root != null ? root.GetComponent<Character>() : null;
            if (character != null && !(character is Player))
            {
                return true;
            }
            string why = root == null ? $"its base '{chain.RootBase}' is not a prefab of the game or any mod"
                : character is Player ? "a person is made with base: Human, not the player's prefab"
                : $"its base '{chain.RootBase}' is not a creature";
            Log.Error($"{BuildReport.Describe(chain.Creature, "base")}: {why}. The creature is left out.");
            return false;
        }

        private static string Names(List<CreatureDefinition> links) => string.Join(" on ", links.ConvertAll(link => link.Name));
    }
}
