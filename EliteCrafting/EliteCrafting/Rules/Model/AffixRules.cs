using System;
using System.Collections.Generic;
using EliteCrafting.Effects;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The affix family (<c>EliteCrafting_affixes*.yml</c>) after merging and validation, with the lookups every
    /// reader needs precomputed at load: by id, the channel table, and the rollable pools per item class. Immutable.
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

        internal Dictionary<string, PoolEntry[]> Pools { get; set; } = new Dictionary<string, PoolEntry[]>(StringComparer.Ordinal);

        public AffixDef? Get(string? id) => id != null && ById.TryGetValue(id, out AffixDef def) ? def : null;

        /// <summary>
        /// Enabled affixes with weight above 0 that list the item class in <c>classes.best</c> or <c>classes.allowed</c>,
        /// in file order, each with its fit. Level, tier, requires, limit and exclusion filters are the roller's job.
        /// </summary>
        public IReadOnlyList<PoolEntry> Pool(string? classId) =>
            classId != null && Pools.TryGetValue(classId, out PoolEntry[] pool) ? pool : Array.Empty<PoolEntry>();

        /// <summary>The class ids any affix names (pools exist for exactly these).</summary>
        public IEnumerable<string> PooledClasses => Pools.Keys;
    }

    /// <summary>One affix of a class's pool and how it fits the class.</summary>
    public readonly struct PoolEntry
    {
        public PoolEntry(AffixDef def, ClassFit fit)
        {
            Def = def;
            Fit = fit;
        }

        public AffixDef Def { get; }
        public ClassFit Fit { get; }
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
