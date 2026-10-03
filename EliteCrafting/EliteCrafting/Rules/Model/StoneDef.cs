using System;
using System.Collections.Generic;

namespace EliteCrafting.Rules
{
    /// <summary>One rune definition (economy-yaml.md section 4), merged and validated. Immutable. Players see "rune";
    /// the code keeps the word "stone".</summary>
    public sealed class StoneDef
    {
        public string Id { get; internal set; } = "";

        /// <summary><c>ECF_</c> + PascalCase id.</summary>
        public string Prefab { get; internal set; } = "";

        /// <summary>A <c>$key</c> (default <c>$ecf_stone_&lt;id&gt;</c>) or literal text.</summary>
        public string Name { get; internal set; } = "";

        /// <summary>The vanilla tooltip description: a <c>$key</c> (default <c>$ecf_stone_&lt;id&gt;_desc</c>) or literal text.</summary>
        public string Description { get; internal set; } = "";

        public StoneVerb Verb { get; internal set; }
        public IReadOnlyList<string> AppliesTo { get; internal set; } = Array.Empty<string>();

        /// <summary>Stones consumed per use by rarity; a rarity absent here costs 1 (see <see cref="CostFor"/>).</summary>
        public IReadOnlyDictionary<string, int> Cost { get; internal set; } = new Dictionary<string, int>();

        /// <summary>
        /// The weakest affix strength grade this stone rolls, or 0 for none. The YAML's <c>tier_floor</c> counts down
        /// (1 = the strongest tier); this is its grade (<see cref="Core.AffixTierNumbers"/>).
        /// </summary>
        public int TierFloor { get; internal set; }

        public bool Enabled { get; internal set; } = true;
        public bool Confirm { get; internal set; }
        public int Stack { get; internal set; } = 50;

        /// <summary>The item weight of one stone (YAML <c>item_weight</c>; plain <c>weight</c> is never an item weight, RC-6).</summary>
        public float ItemWeight { get; internal set; } = 0.2f;

        /// <summary><c>#RRGGBB</c> override of the default tint, or null.</summary>
        public string? Tint { get; internal set; }

        // corrupt
        public IReadOnlyList<CorruptWeight> Outcomes { get; internal set; } = Array.Empty<CorruptWeight>();
        public int Overflow { get; internal set; } = 1;

        public bool AppliesToRarity(string rarityId)
        {
            for (int i = 0; i < AppliesTo.Count; i++)
            {
                if (AppliesTo[i] == rarityId)
                {
                    return true;
                }
            }
            return false;
        }

        public int CostFor(string rarityId) => Cost.TryGetValue(rarityId, out int cost) ? cost : 1;
    }

    /// <summary>One row of a corrupt stone's outcome table.</summary>
    public sealed class CorruptWeight
    {
        public CorruptOutcome Outcome { get; internal set; }
        public float Weight { get; internal set; }
    }
}
