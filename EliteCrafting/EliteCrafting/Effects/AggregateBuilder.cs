using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// The rebuild: walk the local player's counted items (<see cref="ItemEffects.CollectLocal"/>), sum every active
    /// affix into its channel, clamp each channel to its cap (on the sum, never per item), and fold the player-global
    /// channels into two value sets: <see cref="Normal"/> (unconditional channels) and <see cref="Critical"/>
    /// (unconditional plus health-critical, each conditional channel capped on its own). Runs on events only, at most
    /// once per frame (<see cref="EffectRuntime"/>); reuses every buffer, so it allocates nothing in steady state.
    /// Local player only: an empty result when there is no local player or effects are off.
    /// </summary>
    internal static class AggregateBuilder
    {
        public static readonly AggregateValues Normal = new AggregateValues();
        public static readonly AggregateValues Critical = new AggregateValues();

        private static readonly List<ActiveAffix> Affixes = new List<ActiveAffix>();
        private static readonly List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();

        /// <summary>Raw channel sums of the last rebuild (index = channel), for <c>ecraft stats</c>.</summary>
        internal static float[] Sums { get; private set; } = Array.Empty<float>();

        /// <summary>Per channel: the equipment positions that fed it (<see cref="EquipPositions"/> bits).</summary>
        internal static int[] Sources { get; private set; } = Array.Empty<int>();

        internal static ChannelPlan? LastPlan { get; private set; }
        internal static int ActiveAffixCount { get; private set; }
        internal static int MagicItemCount { get; private set; }
        internal static float RebuiltAt { get; private set; } = -1f;

        public static void Build(Player? player)
        {
            ChannelPlan plan = ChannelPlan.Current;
            HealthCritical.Refresh(ActiveRules.Current.Affixes);
            ItemEffects.CollectLocal(Affixes, Items);
            Indicators.ClearIcons();
            Sum(plan, player);
            Normal.Clear();
            Critical.Clear();
            Fold(plan);
            HealthCritical.AddThresholdBonus(ActiveRules.Current.Affixes, Normal[EffectKind.HcThreshold]);
            LastPlan = plan;
            ActiveAffixCount = Affixes.Count;
            MagicItemCount = CountMagic(Items);
            RebuiltAt = UnityEngine.Time.time;
        }

        private static void Sum(ChannelPlan plan, Player? player)
        {
            if (Sums.Length != plan.Count)
            {
                Sums = new float[plan.Count];
                Sources = new int[plan.Count];
            }
            Array.Clear(Sums, 0, Sums.Length);
            Array.Clear(Sources, 0, Sources.Length);
            foreach (ActiveAffix affix in Affixes)
            {
                int c = affix.Channel;
                if (c >= 0 && c < plan.Count)
                {
                    Sums[c] += affix.Roll.Value;
                    Sources[c] |= EquipPositions.Of(player, affix.Item);
                    Indicators.Note(plan.Kinds[c], affix.Item);
                }
            }
        }

        private static void Fold(ChannelPlan plan)
        {
            for (int c = 0; c < plan.Count; c++)
            {
                EffectKind kind = plan.Kinds[c];
                if (Sums[c] == 0f || kind == EffectKind.Unknown || EffectKinds.IsItemLocal(kind))
                {
                    continue;
                }
                ChannelDef channel = plan.Channels[c];
                float amount = Scale(channel, plan.Clamp(c, Sums[c]));
                Critical.Add(kind, channel, amount);
                if (channel.Condition == AffixCondition.None)
                {
                    Normal.Add(kind, channel, amount);
                }
            }
        }

        /// <summary>Percent values become fractions (12 → 0.12); flat and flag values stay as they are.</summary>
        internal static float Scale(ChannelDef channel, float value) =>
            channel.Sample.Value == AffixValueType.Percent ? value / 100f : value;

        private static int CountMagic(List<ItemDrop.ItemData> items)
        {
            int n = 0;
            foreach (ItemDrop.ItemData item in items)
            {
                if (EliteCrafting.Affixes.ItemState.Read(item).IsMagic)
                {
                    n++;
                }
            }
            return n;
        }
    }

    /// <summary>Where an equipped item sits, as bits, for the <c>ecraft stats</c> source column.</summary>
    internal static class EquipPositions
    {
        public const int Right = 1, Left = 2, Head = 4, Chest = 8, Legs = 16, Cape = 32, Utility = 64;

        private static readonly string[] Names = { "right", "left", "head", "chest", "legs", "cape", "utility" };

        public static int Of(Player? player, ItemDrop.ItemData item)
        {
            if (player == null)
            {
                return 0;
            }
            if (ReferenceEquals(item, player.m_rightItem)) return Right;
            if (ReferenceEquals(item, player.m_leftItem)) return Left;
            if (ReferenceEquals(item, player.m_helmetItem)) return Head;
            if (ReferenceEquals(item, player.m_chestItem)) return Chest;
            if (ReferenceEquals(item, player.m_legItem)) return Legs;
            if (ReferenceEquals(item, player.m_shoulderItem)) return Cape;
            return ReferenceEquals(item, player.m_utilityItem) ? Utility : 0;
        }

        public static string Describe(int mask)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < Names.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    parts.Add(Names[i]);
                }
            }
            return string.Join(", ", parts);
        }
    }
}
