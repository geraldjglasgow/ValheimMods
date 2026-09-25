using System;
using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// What the code does with a channel, one value per registered effect id. Resolved once per rules generation into
    /// <see cref="ChannelPlan"/>, so no rebuild and no hook ever compares effect ids as strings.
    /// </summary>
    internal enum EffectKind
    {
        Unknown,

        // aggregate status effect: native SE_Stats fields
        MoveSpeed,
        CarryCapacity,
        HealthRecovery,
        StaminaRecovery,
        EitrRecovery,
        RunStaminaCost,
        JumpStaminaCost,
        AttackStaminaCost,
        BlockStaminaCost,
        DodgeStaminaCost,
        HomeItemStaminaCost,
        FallDamageTaken,
        JumpHeight,
        ParryBonus,
        NoiseMade,
        Stealth,
        StaggerTaken,
        Swimmer,
        SlowFall,

        // aggregate status effect: our own overrides
        MoveSpeedSneak,
        SkillLevel,
        SkillGain,
        DamageDealt,
        NightDamage,
        MoveSpeedSprint,
        MoveSpeedEncumbered,
        MoveSpeedAfterDodge,
        MoveSpeedPaved,
        SurpriseBonus,
        StaggerPower,
        CoinDamage,
        ResistModifier,
        HcThreshold,
        HealthRecoveryFlat,

        // player-global hooks and field writes (summed with the aggregate, applied elsewhere)
        MaxHealth,
        MaxStamina,
        MaxEitr,
        PickupRadius,
        BuildRange,
        ExploreRadius,
        HealthForRegen,
        EitrForRegen,

        // player-global hooks, Phase 2: the local player's own client (Effects/*Hooks, *Patches)
        SlopePenalty,
        TerrainSlow,
        HeatResist,
        RestComfort,
        SkillLoss,
        FoodDuration,
        ForsakenCooldown,
        DamageTaken,
        RangedDamageTaken,
        AvoidHit,
        KnockbackTaken,
        Slayer,
        StaggeredTargetDamage,
        LowHealthOpener,
        ExploitStagger,
        OnHitSlow,
        ComboFinisher,
        DodgeFury,
        OnKillRestore,
        Leech,
        Thorns,
        CalmWard,
        DebuffDecay,
        FrostSlowTaken,
        StaggerRecovery,
        ParryRestore,
        AutoMead,
        MeadBurst,
        MeadCooldown,
        RainShield,
        IgnoreWet,
        ColdImmunity,
        FreezeImmunity,

        // player-global, published on the player's own ZDO for other peers (PlayerStats): the creature's, ship's,
        // pickable's, rock's owner or every client reads them
        StaggerDurationDealt,
        LightAura,
        DemistRadius,
        TamingSpeed,
        SailSpeed,
        YieldMining,
        YieldLumber,
        YieldPickable,

        // loot-kind: applied by Loot (killer-stat ZDO plumbing, Loot/FindPublisher + KillerStats), no-op here
        FindRarity,
        FindStones,
        FindTrophy,
        FindCoins,

        // item-local hooks (never summed across items); keep ItemArmor first and SummonHealth last
        ItemArmor,
        ItemBlock,
        ItemDeflection,
        ItemDurability,
        ItemLighten,
        BrandDamage,
        DrawStaminaCost,
        AttackEitrCost,
        ReloadSpeed,
        ItemUnbreakable,
        ItemZeroWeight,
        ItemNoMovePenalty,
        AttackHealthCost,
        AttackReach,
        AttackArc,
        ProjectileVelocity,
        DrawSpeed,
        DrawMovePenalty,
        Multishot,
        Twincast,
        AmmoSave,
        RuneEdge,
        BloodPrice,
        LoneBlade,
        BlockSteadfast,
        PerfectBlockWindow,
        SummonDamage,
        SummonHealth,
    }

    /// <summary>Effect id → <see cref="EffectKind"/>. Every id in <see cref="EffectCatalog"/> names a kind (checked at Awake).</summary>
    internal static class EffectKinds
    {
        /// <summary>The kind of an effect id: the id in PascalCase (<c>move_speed</c> → <see cref="EffectKind.MoveSpeed"/>).</summary>
        public static EffectKind Of(string id) => EnumIds<EffectKind>.TryParse(id, out EffectKind kind) ? kind : EffectKind.Unknown;

        /// <summary>Item-local kinds are applied by the item hooks and never summed across items.</summary>
        public static bool IsItemLocal(EffectKind kind) => kind >= EffectKind.ItemArmor;

        /// <summary>Plugin Awake: every registered effect must have a kind, or it would be registered but inert.</summary>
        public static void VerifyCatalog()
        {
            foreach (string id in EffectRegistry.AllIds)
            {
                if (Of(id) == EffectKind.Unknown)
                {
                    Core.Log.Error($"effect '{id}' is registered but has no implementation; affixes using it do nothing");
                }
            }
        }
    }

    /// <summary>
    /// The channel table of one rules generation, resolved to kinds: index = <see cref="ChannelDef.Index"/>. Rebuilt
    /// when the generation changes (on the next rebuild or item-cache miss, never per frame).
    /// </summary>
    internal sealed class ChannelPlan
    {
        private static ChannelPlan _current = new ChannelPlan(RuleSet.Empty);

        private ChannelPlan(RuleSet rules)
        {
            Generation = rules.Generation;
            Channels = rules.Affixes.Channels;
            Kinds = new EffectKind[Channels.Count];
            for (int i = 0; i < Kinds.Length; i++)
            {
                Kinds[i] = EffectKinds.Of(Channels[i].Effect.Id);
            }
        }

        public int Generation { get; }
        public IReadOnlyList<ChannelDef> Channels { get; }
        public EffectKind[] Kinds { get; }
        public int Count => Kinds.Length;

        /// <summary>The plan for the running rules.</summary>
        public static ChannelPlan Current
        {
            get
            {
                RuleSet rules = ActiveRules.Current;
                if (_current.Generation != rules.Generation)
                {
                    _current = new ChannelPlan(rules);
                }
                return _current;
            }
        }

        /// <summary>The channel's summed value clamped to its cap (caps are on the sum, never per item).</summary>
        public float Clamp(int channel, float sum) => Math.Min(sum, Channels[channel].Cap);
    }
}
