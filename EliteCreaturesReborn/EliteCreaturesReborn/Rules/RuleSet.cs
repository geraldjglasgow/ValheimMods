using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// One loaded rule file: the two top-level switches and the resolved rules per biome. Every biome entry is already
    /// merged over <see cref="Defaults"/>, so a lookup is a single dictionary hit with a fall back to the defaults for
    /// an unknown or modded biome.
    /// </summary>
    public sealed class RuleSet
    {
        public bool LockToServer = true;
        public int MaxMutations = 1;
        public BiomeRules Defaults = new BiomeRules();

        /// <summary>Per-mutation on/off switch. An entry missing here (an old file, an unlisted mutation) means enabled.</summary>
        public readonly Dictionary<Mutation, bool> MutationEnabled = new Dictionary<Mutation, bool>();

        public bool IsEnabled(Mutation mutation) => !MutationEnabled.TryGetValue(mutation, out bool value) || value;

        /// <summary>The boss table. One for the whole world: boss stars do not follow a biome or the world's pressure.</summary>
        public BossRules Boss = new BossRules();

        /// <summary>How camps, dungeons and dungeon loot repopulate. One set of timers for the whole world.</summary>
        public RespawnRules Respawn = new RespawnRules();

        /// <summary>The world tier: which boss defeats raise it and how much each tier boosts stars and mutations.</summary>
        public TierRules Tiers = new TierRules();

        /// <summary>What a newborn of tamed parents inherits from them.</summary>
        public BreedingRules Breeding = new BreedingRules();

        /// <summary>The world-wide loot settings: mode, extra rolls, multipliers, the trophy switch.</summary>
        public LootRules Loot = new LootRules();

        /// <summary>Per-creature loot rules, keyed by prefab name. A creature without an entry follows the mode alone.</summary>
        public readonly Dictionary<string, CreatureLootRule> CreatureLoot =
            new Dictionary<string, CreatureLootRule>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, BiomeRules> Biomes =
            new Dictionary<string, BiomeRules>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _loggedUnlisted = new HashSet<string>();

        /// <summary>The rules that govern a creature in the given biome; the defaults for any biome without an entry.</summary>
        public BiomeRules For(Heightmap.Biome biome)
        {
            string name = biome.ToString();
            if (Biomes.TryGetValue(name, out BiomeRules rules))
            {
                return rules;
            }
            if (_loggedUnlisted.Add(name))
            {
                Log.Info($"biome '{name}' has no rules entry - using defaults, and the Meadows row for stars");
            }
            return Defaults;
        }
    }
}
