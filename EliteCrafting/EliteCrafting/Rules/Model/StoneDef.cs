using System;
using System.Collections.Generic;
using EliteCrafting.Items;

namespace EliteCrafting.Rules
{
    /// <summary>One stone or sigil definition (economy-yaml.md section 4), merged and validated. Immutable.</summary>
    public sealed class StoneDef
    {
        public string Id { get; internal set; } = "";

        /// <summary><c>ECF_</c> + PascalCase id for built-in stones, <c>ECF_CustomNN</c> for owner-defined ones.</summary>
        public string Prefab { get; internal set; } = "";

        /// <summary>A <c>$key</c> (default <c>$ecf_stone_&lt;id&gt;</c>) or literal text.</summary>
        public string Name { get; internal set; } = "";

        /// <summary>The vanilla tooltip description: a <c>$key</c> (default <c>$ecf_stone_&lt;id&gt;_desc</c>) or literal text.</summary>
        public string Description { get; internal set; } = "";

        public StoneVerb Verb { get; internal set; }
        public StoneGrade Grade { get; internal set; }
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

        /// <summary>Slot filter; empty = any slot. Required for <c>quality</c>.</summary>
        public IReadOnlyList<ItemSlot> Slots { get; internal set; } = Array.Empty<ItemSlot>();

        // corrupt
        public IReadOnlyList<CorruptWeight> Outcomes { get; internal set; } = Array.Empty<CorruptWeight>();
        public int Overflow { get; internal set; } = 1;

        // gamble
        public IReadOnlyDictionary<string, float> GambleWeights { get; internal set; } = new Dictionary<string, float>();

        // duplicate
        public bool SealCopy { get; internal set; } = true;

        // lock
        public int MaxBound { get; internal set; } = 1;

        // quality
        public int Step { get; internal set; } = 1;
        public int Cap { get; internal set; } = 10;

        // sigil
        public SigilSteer Steer { get; internal set; }
        public AffixCategory? SteerCategory { get; internal set; }

        // imbue (essences.md section 11): the essence_families entry the guaranteed affix is drawn from; gem and catalyse
        // (sockets.md): the family the gem belongs to, the family a catalyst strengthens
        public string? Family { get; internal set; }

        /// <summary>The most sockets any item can have, from a drop or a stone (sockets.md section 2).</summary>
        public const int SocketLimit = 6;

        // socket (sockets.md section 3): the most sockets this stone makes on an item (a drop may carry more)
        public int MaxSockets { get; internal set; } = 2;

        // gem (sockets.md section 4): the affix id the gem gives, by item slot; a slot not listed refuses the gem
        public IReadOnlyDictionary<ItemSlot, string> GemAffixes { get; internal set; } = new Dictionary<ItemSlot, string>();

        public string? GemAffix(ItemSlot slot) => GemAffixes.TryGetValue(slot, out string id) ? id : null;

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

        public bool AcceptsSlot(ItemSlot slot)
        {
            if (Slots.Count == 0)
            {
                return true;
            }
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i] == slot)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>One row of a corrupt stone's outcome table.</summary>
    public sealed class CorruptWeight
    {
        public CorruptOutcome Outcome { get; internal set; }
        public float Weight { get; internal set; }
    }
}
