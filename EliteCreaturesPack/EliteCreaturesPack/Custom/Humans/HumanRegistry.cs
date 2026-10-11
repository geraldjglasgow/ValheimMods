using System.Collections.Generic;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// The humans loaded on this peer, each with its <see cref="HumanRanged"/>: the one place the human patches ask
    /// whether a character is one of them. Filled and emptied by HumanRanged's Awake and OnDestroy, so it never holds a
    /// destroyed creature. A lookup is a count test and, while any human is loaded, one dictionary lookup.
    /// </summary>
    internal static class HumanRegistry
    {
        private static readonly Dictionary<Character, HumanRanged> live = new Dictionary<Character, HumanRanged>();

        public static void Add(Character human, HumanRanged ranged) => live[human] = ranged;

        public static void Remove(Character human) => live.Remove(human);

        /// <summary>The character's <see cref="HumanRanged"/> if it is a human, otherwise null.</summary>
        public static HumanRanged? Find(Character character) =>
            live.Count != 0 && live.TryGetValue(character, out HumanRanged ranged) ? ranged : null;
    }
}
