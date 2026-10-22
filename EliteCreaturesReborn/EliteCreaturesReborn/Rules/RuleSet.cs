using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// One loaded rule file: the top-level switches and the resolved rules per biome. Every biome entry is already
    /// merged over <see cref="Defaults"/>, so a lookup is a single dictionary hit with a fall back to the defaults for
    /// an unknown or modded biome. A creature with mutation keys in its `creatures:` entry gets those laid over its
    /// biome's rules on first lookup, cached here, so the cache goes when a reload replaces the set.
    /// </summary>
    public sealed class RuleSet
    {
        /// <summary>The .cfg's Difficulty; Custom plays the file's own rows.</summary>
        public Difficulty Difficulty = Difficulty.Custom;

        /// <summary>The .cfg's Max mutations; 0 for no cap.</summary>
        public int MaxMutations = 1;
        public BiomeRules Defaults = new BiomeRules();

        /// <summary>The .cfg's Creature stars. False: creatures roll no stars and keep the game's level.</summary>
        public bool CreatureStars = true;

        /// <summary>Per-mutation on/off switch, the .cfg's `2 - Mutations`. An entry missing here takes the mutation's own
        /// default (<see cref="MutationCatalog.OnByDefault"/>).</summary>
        public readonly Dictionary<Mutation, bool> MutationEnabled = new Dictionary<Mutation, bool>();

        public bool IsEnabled(Mutation mutation) =>
            MutationEnabled.TryGetValue(mutation, out bool value) ? value : MutationCatalog.OnByDefault(mutation);

        /// <summary>The boss table. One for the whole world: boss stars do not follow a biome or the world's pressure.</summary>
        public BossRules Boss = new BossRules();

        /// <summary>How camps, dungeons and dungeon loot repopulate, the .cfg's `3 - Respawning`. One set of timers for the
        /// whole world.</summary>
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

        /// <summary>Per-creature mutation keys, keyed by prefab name; only entries that name one are listed.</summary>
        public readonly Dictionary<string, CreatureMutationRule> CreatureMutations =
            new Dictionary<string, CreatureMutationRule>(StringComparer.OrdinalIgnoreCase);

        public readonly Dictionary<string, BiomeRules> Biomes =
            new Dictionary<string, BiomeRules>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _loggedUnlisted = new HashSet<string>();

        // Keyed by the rules object itself, like the creature merges: one neutral copy per biome block or merge.
        private readonly Dictionary<BiomeRules, BiomeRules> _unstarred = new Dictionary<BiomeRules, BiomeRules>();

        /// <summary>
        /// True when a creature with no stars of this mod keeps the level the game (or another mod) gave it: this mod's
        /// stars are off for its kind - Creature stars false (the .cfg), or `stars: false` under `bosses:` for a boss. Such a
        /// creature is never put back to level 1, and the star power lines do not touch it; the game's level does.
        /// </summary>
        public bool KeepsLevel(int stars, bool isBoss) => stars == 0 && !(isBoss ? Boss.Enabled : CreatureStars);

        /// <summary>As <see cref="KeepsLevel(int, bool)"/>, and true too for a tame whose stars are the game's
        /// (<see cref="CreatureTraits.GameStars"/>).</summary>
        public bool KeepsLevel(CreatureTraits traits, bool isBoss) =>
            traits.Stars == 0 && (traits.GameStars || KeepsLevel(0, isBoss));

        /// <summary>The rules with neutral star power, for a creature that keeps its level: its mutations still apply.</summary>
        public BiomeRules Unstarred(BiomeRules rules)
        {
            if (!_unstarred.TryGetValue(rules, out BiomeRules neutral))
            {
                neutral = rules.Clone();
                neutral.Star = new StarPower();
                _unstarred[rules] = neutral;
            }
            return neutral;
        }

        /// <summary>
        /// The rules that govern one creature: its biome's, with its own `creatures:` entry's mutation keys on top. A
        /// creature with no such entry gets the biome's object itself; one with an entry, a merged copy made once per
        /// biome and shared by every creature of its kind there. Bosses never come here: they scale on the boss table.
        /// </summary>
        public BiomeRules For(Heightmap.Biome biome, string prefab)
        {
            BiomeRules rules = For(biome);
            return CreatureMutations.TryGetValue(prefab, out CreatureMutationRule own) ? own.Over(rules) : rules;
        }

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
