using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// A creature's definitions from its root down: when a creature's base is another custom creature, that one's
    /// definition comes first, then its own (and so on up the line). <see cref="RootBase"/> is the first base that is not a
    /// custom creature: the game or mod prefab its shell is copied from, or <c>Human</c>.
    /// </summary>
    internal sealed class CreatureChain
    {
        public CreatureChain(CreatureDefinition creature, List<CreatureDefinition> links, string rootBase, bool human)
        {
            Creature = creature;
            Links = links;
            RootBase = rootBase;
            Human = human;
        }

        /// <summary>The creature being built.</summary>
        public CreatureDefinition Creature { get; }

        /// <summary>The definitions applied to its shell, base-most first, <see cref="Creature"/> last.</summary>
        public List<CreatureDefinition> Links { get; }

        /// <summary>The game or mod prefab at the root, or <c>Human</c>.</summary>
        public string RootBase { get; }

        /// <summary>Whether the root is the player's body.</summary>
        public bool Human { get; }
    }
}
