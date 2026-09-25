using System;
using System.Collections.Generic;
using EliteCrafting.Effects;
using EliteCrafting.Items;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The affix family (<c>EliteCrafting_affixes*.yml</c>) after merging and validation, with the lookups every
    /// reader needs precomputed at load: by id, the channel table, and the rollable pools per slot. Immutable.
    /// </summary>
    public sealed class AffixRules
    {
        public static readonly AffixRules Empty = new AffixRules();

        public float HealthCriticalThreshold { get; internal set; } = 30f;
        public float HealthCriticalMaxThreshold { get; internal set; } = 50f;

        /// <summary>The merged <c>caps:</c> map, channel key → cap.</summary>
        public IReadOnlyDictionary<string, float> Caps { get; internal set; } = new Dictionary<string, float>();

        /// <summary>Every affix, enabled or not, in file order.</summary>
        public IReadOnlyList<AffixDef> Affixes { get; internal set; } = Array.Empty<AffixDef>();

        public IReadOnlyDictionary<string, AffixDef> ById { get; internal set; } = new Dictionary<string, AffixDef>();

        /// <summary>One channel per distinct (effect, param, condition) of the enabled affixes; index = AffixDef.ChannelIndex.</summary>
        public IReadOnlyList<ChannelDef> Channels { get; internal set; } = Array.Empty<ChannelDef>();

        internal Dictionary<ItemSlot, AffixDef[]> RegularPools { get; set; } = new Dictionary<ItemSlot, AffixDef[]>();
        internal Dictionary<ItemSlot, AffixDef[]> MythicPools { get; set; } = new Dictionary<ItemSlot, AffixDef[]>();

        public AffixDef? Get(string? id) => id != null && ById.TryGetValue(id, out AffixDef def) ? def : null;

        /// <summary>
        /// Enabled affixes with weight above 0 that list the slot, from the regular or the Mythic-only pool. Tier,
        /// requires, exclusion and category filters are the roller's job.
        /// </summary>
        public IReadOnlyList<AffixDef> Pool(ItemSlot slot, bool mythicOnly)
        {
            Dictionary<ItemSlot, AffixDef[]> pools = mythicOnly ? MythicPools : RegularPools;
            return pools.TryGetValue(slot, out AffixDef[] pool) ? pool : Array.Empty<AffixDef>();
        }
    }

    /// <summary>
    /// A summing channel: every enabled affix with the same effect, param and condition feeds it. Channel indexes are
    /// assigned at load so the effects layer never looks anything up by name at runtime.
    /// </summary>
    public sealed class ChannelDef
    {
        public int Index { get; internal set; }

        /// <summary><c>effect</c>, <c>effect:param</c>, with <c>@health_critical</c> for the conditional channel.</summary>
        public string Key { get; internal set; } = "";

        public EffectDef Effect { get; internal set; } = null!;
        public string? Param { get; internal set; }
        public AffixCondition Condition { get; internal set; }

        /// <summary>Cap on the sum, or <see cref="float.PositiveInfinity"/> when uncapped.</summary>
        public float Cap { get; internal set; } = float.PositiveInfinity;

        /// <summary>An affix feeding this channel, to read its parsed param (skills, damage types).</summary>
        public AffixDef Sample { get; internal set; } = null!;
    }
}
