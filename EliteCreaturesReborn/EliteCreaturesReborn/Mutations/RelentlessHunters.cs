using System.Collections.Generic;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Every loaded Relentless creature that has a monster AI, keyed by that AI. The Relentless patches sit on game AI
    /// methods that run for every monster many times a second, so each answers "not one of ours" with a single
    /// dictionary lookup, and with none at all while no Relentless creature is loaded.
    /// </summary>
    public static class RelentlessHunters
    {
        private static readonly Dictionary<BaseAI, RelentlessBehaviour> Hunters = new Dictionary<BaseAI, RelentlessBehaviour>();

        public static void Add(BaseAI ai, RelentlessBehaviour hunter) => Hunters[ai] = hunter;

        public static void Remove(BaseAI ai) => Hunters.Remove(ai);

        public static bool TryGet(BaseAI ai, out RelentlessBehaviour hunter)
        {
            if (Hunters.Count == 0)
            {
                hunter = null!;
                return false;
            }
            return Hunters.TryGetValue(ai, out hunter);
        }

        public static bool Has(BaseAI ai) => Hunters.Count > 0 && Hunters.ContainsKey(ai);

        /// <summary>True on the owner while this creature holds a quarry within its chase distance.</summary>
        public static bool IsHunting(BaseAI ai) => TryGet(ai, out RelentlessBehaviour hunter) && hunter.Hunting;
    }
}
