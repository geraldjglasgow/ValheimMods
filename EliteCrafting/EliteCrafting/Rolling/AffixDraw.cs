using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Items;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// One roll's working state over a builder (rarity.md section 4): which ids and exclusion groups the item already
    /// holds, and the two-stage draw itself - the affix by <c>weight</c>, then its tier by tier weight, then the value.
    /// Short-lived: made per roll, never shared.
    /// <para>
    /// Occupancy: every affix on the item holds its id (dormant ones too); its exclusion group is known when the id is
    /// still defined (enabled or not); an orphaned id has no known group and blocks nothing else.
    /// </para>
    /// </summary>
    internal sealed class AffixDraw
    {
        private readonly RollContext _context;
        private readonly HashSet<string> _ids = new HashSet<string>();
        private readonly HashSet<string> _groups = new HashSet<string>();
        private readonly List<AffixDef> _candidates = new List<AffixDef>();
        private readonly List<AffixTierDef> _tiers = new List<AffixTierDef>();
        private readonly List<float> _weights = new List<float>();

        public AffixDraw(ItemStateBuilder builder, RollContext context)
        {
            Builder = builder;
            _context = context;
            foreach (AffixRoll roll in builder.Affixes)
            {
                Occupy(roll.Id);
            }
        }

        public ItemStateBuilder Builder { get; }

        /// <summary>Draws one affix and appends it. False (nothing changed) when the pool has no candidate.</summary>
        public bool TryAdd()
        {
            if (!TryDraw(out AffixRoll roll))
            {
                return false;
            }
            Builder.AddAffix(roll);
            Occupy(roll.Id);
            return true;
        }

        private bool TryDraw(out AffixRoll roll)
        {
            roll = default;
            Gather();
            AffixDef? def = PickAffix(_candidates);
            if (def == null)
            {
                return false;
            }
            TierWindow.Eligible(def, _context, _tiers);
            AffixTierDef tier = TierWindow.Pick(_tiers, _context, _weights);
            roll = new AffixRoll(def.Id, tier.Tier, ValueOf(def, tier));
            return true;
        }

        /// <summary>Marks an id (and its exclusion group) as held by the item.</summary>
        public void Occupy(string id)
        {
            _ids.Add(id);
            string? group = _context.Rules.Affixes.Get(id)?.ExclusionGroup;
            if (group != null)
            {
                _groups.Add(group);
            }
        }

        private float ValueOf(AffixDef def, AffixTierDef tier) =>
            def.Value == AffixValueType.Flag ? 1f : RollMath.RollValue(tier.Min, tier.Max, tier.Decimals, _context.Random);

        private void Gather()
        {
            _candidates.Clear();
            IReadOnlyList<AffixDef> pool = _context.Rules.Affixes.Pool(_context.Slot.Slot);
            for (int i = 0; i < pool.Count; i++)
            {
                if (IsCandidate(pool[i]))
                {
                    _candidates.Add(pool[i]);
                }
            }
        }

        private bool IsCandidate(AffixDef def)
        {
            if (!def.Enabled || def.Weight <= 0f || _ids.Contains(def.Id))
            {
                return false;
            }
            if (def.ExclusionGroup != null && _groups.Contains(def.ExclusionGroup))
            {
                return false;
            }
            if (!def.RollsOn(_context.Slot.Slot) || !ItemSlots.Satisfies(_context.Slot, def.Requires))
            {
                return false;
            }
            TierWindow.Eligible(def, _context, _tiers);
            return _tiers.Count > 0;
        }

        private AffixDef? PickAffix(List<AffixDef> pool)
        {
            _weights.Clear();
            foreach (AffixDef def in pool)
            {
                _weights.Add(def.Weight);
            }
            int index = RollMath.PickWeighted(_weights, _context.Random);
            return index < 0 ? null : pool[index];
        }
    }
}
