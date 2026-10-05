using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Items;
using EliteCrafting.Rules;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// The item-local numbers of one item, precomputed from its state so the hot getters (GetDamage, GetWeight,
    /// GetArmor, GetMaxDurability) only read floats. Values are fractions (12% → 0.12). Per effect: the unconditional
    /// sum and the health-critical sum, each clamped to its channel cap on this item (item-local effects are never
    /// summed across items). Brands are kept per damage type.
    /// </summary>
    internal sealed class ItemLocalSums
    {
        private const int KindSpan = EffectKind.SummonHealth - EffectKind.ItemArmor + 1;

        private readonly float[] _normal = new float[KindSpan];
        private readonly float[] _critical = new float[KindSpan];

        /// <summary>Per damage type: the share of the item's own combat damage added again as that type.</summary>
        public readonly float[] Brand = new float[DamageSlots.Count];
        public readonly float[] BrandCritical = new float[DamageSlots.Count];
        public bool HasBrand;

        /// <summary>Per damage type: the flat damage <c>added_damage</c> adds to the item's hits (Combat3/AddedDamage).</summary>
        public readonly float[] Added = new float[DamageSlots.Count];
        public readonly float[] AddedCritical = new float[DamageSlots.Count];
        public bool HasAdded;

        /// <summary>The fraction for an item-local kind, with the health-critical part while the local player is critical.</summary>
        public float Get(EffectKind kind)
        {
            int i = kind - EffectKind.ItemArmor;
            return HealthCritical.Active ? _normal[i] + _critical[i] : _normal[i];
        }

        public float BrandShare(int type) => HealthCritical.Active ? Brand[type] + BrandCritical[type] : Brand[type];

        public float AddedFlat(int type) => HealthCritical.Active ? Added[type] + AddedCritical[type] : Added[type];

        // ---- building (on a cache miss: a new item object, a write, a rules reload; never per frame)

        private static float[] _channelSums = new float[0];
        private static readonly List<int> Touched = new List<int>();

        /// <summary>Null when the item has nothing item-local: the getters then return at once.</summary>
        public static ItemLocalSums? Build(ItemDrop.ItemData item, ItemState state)
        {
            ItemLocalSums sums = new ItemLocalSums();
            return sums.AddAffixes(state, ChannelPlan.Current) ? sums : null;
        }

        private bool AddAffixes(ItemState state, ChannelPlan plan)
        {
            SumChannels(state, plan);
            foreach (int c in Touched)
            {
                ChannelDef channel = plan.Channels[c];
                Add(plan.Kinds[c], channel, AggregateBuilder.Scale(channel, plan.Clamp(c, _channelSums[c])));
            }
            return Touched.Count > 0;
        }

        // Sums this item's active item-local affixes per channel (two on one channel sum, then clamp once).
        private static void SumChannels(ItemState state, ChannelPlan plan)
        {
            if (_channelSums.Length < plan.Count)
            {
                _channelSums = new float[plan.Count];
            }
            Touched.Clear();
            IReadOnlyList<EffectRoll> rolls = state.EffectRolls;
            for (int i = 0; i < rolls.Count; i++)
            {
                int c = rolls[i].Def.ChannelIndex;
                if (c >= 0 && c < plan.Count && EffectKinds.IsItemLocal(plan.Kinds[c]))
                {
                    if (!Touched.Contains(c))
                    {
                        Touched.Add(c);
                        _channelSums[c] = 0f;
                    }
                    _channelSums[c] += rolls[i].Roll.Value;
                }
            }
        }

        private void Add(EffectKind kind, ChannelDef channel, float amount)
        {
            bool critical = channel.Condition == AffixCondition.HealthCritical;
            if (kind == EffectKind.BrandDamage)
            {
                HasBrand |= Split(channel, amount, critical ? BrandCritical : Brand);
            }
            else if (kind == EffectKind.AddedDamage)
            {
                HasAdded |= Split(channel, amount, critical ? AddedCritical : Added);
            }
            else
            {
                (critical ? _critical : _normal)[kind - EffectKind.ItemArmor] += amount;
            }
        }

        // A param naming a group splits the amount evenly over its types (the total added stays the rolled value).
        private static bool Split(ChannelDef channel, float amount, float[] into)
        {
            DamageMask mask = channel.Sample.ParamDamage;
            int count = DamageSlots.CountIn(mask);
            for (int t = 0; t < DamageSlots.Count && count > 0; t++)
            {
                if ((mask & DamageSlots.Masks[t]) != 0)
                {
                    into[t] += amount / count;
                }
            }
            return count > 0;
        }
    }
}
