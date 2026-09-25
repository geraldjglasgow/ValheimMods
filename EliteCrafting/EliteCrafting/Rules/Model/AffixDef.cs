using System;
using System.Collections.Generic;
using EliteCrafting.Effects;
using EliteCrafting.Items;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// One affix of the running configuration, fully merged and validated (configuration.md section 6). Built by the
    /// rules loader and never changed afterwards: treat every setter as loader-only.
    /// </summary>
    public sealed class AffixDef
    {
        public string Id { get; internal set; } = "";

        /// <summary>A <c>$key</c> (default <c>$ecf_affix_&lt;id&gt;</c>) or literal text shown in every language.</summary>
        public string Name { get; internal set; } = "";

        public string Effect { get; internal set; } = "";
        public EffectDef EffectDef { get; internal set; } = null!;

        /// <summary>The raw param text (<c>Swords</c>, <c>Run,Jump,Swim,Sneak</c>, <c>physical</c>), or null.</summary>
        public string? Param { get; internal set; }

        /// <summary>Parsed <c>skill</c> param; <c>Skills.SkillType.All</c> for <c>All</c>. Empty for other kinds.</summary>
        public IReadOnlyList<Skills.SkillType> ParamSkills { get; internal set; } = Array.Empty<Skills.SkillType>();

        /// <summary>Parsed <c>damage_type</c> / <c>element</c> param; None for other kinds.</summary>
        public DamageMask ParamDamage { get; internal set; }

        public AffixValueType Value { get; internal set; }
        public AffixUnit Unit { get; internal set; }
        public IReadOnlyList<ItemSlot> Slots { get; internal set; } = Array.Empty<ItemSlot>();
        public AffixRequirements Requires { get; internal set; } = AffixRequirements.Any;
        public AffixCategory Category { get; internal set; }
        public bool MythicOnly { get; internal set; }
        public AffixCondition Condition { get; internal set; }
        public string? ExclusionGroup { get; internal set; }
        public float Weight { get; internal set; } = 100f;
        public bool Enabled { get; internal set; } = true;
        public HookDifficulty Hook { get; internal set; }

        /// <summary>Sorted by tier, one row per tier.</summary>
        public IReadOnlyList<AffixTierDef> Tiers { get; internal set; } = Array.Empty<AffixTierDef>();

        /// <summary>Index into <see cref="AffixRules.Channels"/>; -1 when the affix is disabled.</summary>
        public int ChannelIndex { get; internal set; } = -1;

        public bool RollsOn(ItemSlot slot)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i] == slot)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The row for a tier, or null when the affix does not define it.</summary>
        public AffixTierDef? TierRow(int tier)
        {
            for (int i = 0; i < Tiers.Count; i++)
            {
                if (Tiers[i].Tier == tier)
                {
                    return Tiers[i];
                }
            }
            return null;
        }

        public int MinTier => Tiers.Count > 0 ? Tiers[0].Tier : 0;
        public int MaxTier => Tiers.Count > 0 ? Tiers[Tiers.Count - 1].Tier : 0;
    }

    /// <summary>One tier row: bounds, draw weight, and the decimals a rolled value is rounded to.</summary>
    public sealed class AffixTierDef
    {
        public int Tier { get; internal set; }

        /// <summary>0 for flags.</summary>
        public float Min { get; internal set; }

        /// <summary>0 for flags.</summary>
        public float Max { get; internal set; }

        public float Weight { get; internal set; } = 100f;

        /// <summary>The most decimals either bound is written with, at most 2.</summary>
        public int Decimals { get; internal set; }
    }

    /// <summary>The <c>requires</c> block: every present part must hold.</summary>
    public sealed class AffixRequirements
    {
        public static readonly AffixRequirements Any = new AffixRequirements();

        /// <summary>Any of these must govern the item. Empty = no skill requirement.</summary>
        public IReadOnlyList<Skills.SkillType> Skills { get; internal set; } = Array.Empty<Skills.SkillType>();

        public Hands Hands { get; internal set; }

        /// <summary>All of these.</summary>
        public ItemTraits Traits { get; internal set; }

        public bool IsAny => Skills.Count == 0 && Hands == Hands.None && Traits == ItemTraits.None;
    }
}
