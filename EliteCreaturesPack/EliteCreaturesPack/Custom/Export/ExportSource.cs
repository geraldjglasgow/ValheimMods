using System;
using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// The creature being exported: its prefab and the game components its values are read from, and, for a custom
    /// creature, its chain of definitions and the originals of its parts. Most values are read from the prefab, so a
    /// custom creature's export shows the result of its build. What Elite Creatures Pack keeps outside the game's own
    /// fields (tints, overlay, texture, muted sounds, eating to heal, Elite Creatures Reborn's lines) is read from the
    /// chain instead: the last definition that sets it, as the build applies them.
    /// </summary>
    internal sealed class ExportSource
    {
        private static readonly IReadOnlyList<CreatureDefinition> NoChain = new CreatureDefinition[0];

        public ExportSource(GameObject prefab, CustomCreature? custom)
        {
            Prefab = prefab;
            Custom = custom;
            Character = prefab.GetComponent<Character>();
            Ai = prefab.GetComponent<BaseAI>();
        }

        public GameObject Prefab { get; }

        public string Name => Prefab.name;

        /// <summary>The custom creature it is, or null for a game or mod creature.</summary>
        public CustomCreature? Custom { get; }

        public Character Character { get; }

        /// <summary>Its mind, a MonsterAI or an AnimalAI; null for the few creatures without one.</summary>
        public BaseAI? Ai { get; }

        public MonsterAI? Monster => Ai as MonsterAI;

        public Humanoid? Humanoid => Character as Humanoid;

        /// <summary>A custom creature's definitions, base-most first; empty for any other creature.</summary>
        public IReadOnlyList<CreatureDefinition> Chain => Custom?.Chain ?? NoChain;

        /// <summary>The last value the chain sets, or null.</summary>
        public T? Last<T>(Func<CreatureDefinition, T?> read) where T : class
        {
            for (int i = Chain.Count - 1; i >= 0; i--)
            {
                T? value = read(Chain[i]);
                if (value != null)
                {
                    return value;
                }
            }
            return null;
        }

        /// <summary>The last value the chain sets, or null.</summary>
        public T? LastValue<T>(Func<CreatureDefinition, T?> read) where T : struct
        {
            for (int i = Chain.Count - 1; i >= 0; i--)
            {
                T? value = read(Chain[i]);
                if (value != null)
                {
                    return value;
                }
            }
            return null;
        }

        /// <summary>The last list the chain fills (an empty list sets nothing), or an empty one.</summary>
        public List<string> LastList(Func<CreatureDefinition, List<string>?> read) =>
            Last(definition => read(definition) is List<string> list && list.Count > 0 ? list : null) ?? new List<string>();

        /// <summary>Whether the prefab is one of those made for this custom creature (attack copies and the like).</summary>
        public bool IsPart(GameObject prefab) =>
            Custom != null && Custom.Parts.Any(part => part != null && (part == prefab || part.name == prefab.name));

        /// <summary>
        /// The name a definition gives a prefab the creature carries or fires: its own, or for one of a custom creature's
        /// parts (which exist only while the creature does) the prefab it was copied from; null for a part whose original
        /// is not on record.
        /// </summary>
        public string? OriginOf(GameObject prefab)
        {
            if (!IsPart(prefab))
            {
                return prefab.name;
            }
            return Custom!.PartOrigins.TryGetValue(prefab.name, out string origin) ? origin : null;
        }
    }
}
