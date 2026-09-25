using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Items;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>
    /// One roll's working state over a builder (rarity.md section 4): which ids and exclusion groups the item already
    /// holds, whether the category steer is still waiting for a draw it can apply to, and the two-stage draw itself -
    /// the affix by <c>weight</c>, then its tier by tier weight, then the value. Short-lived: made per roll, never shared.
    /// <para>
    /// Occupancy: every affix on the item holds its id (dormant ones too); its exclusion group is known when the id is
    /// still defined (enabled or not); an orphaned id has no known group and blocks nothing else. The category steer
    /// applies to the first draw for which the category leaves a candidate, and only to that one (sigils.md 3).
    /// </para>
    /// </summary>
    internal sealed class AffixDraw
    {
        private readonly RollContext _context;
        private readonly HashSet<string> _ids = new HashSet<string>();
        private readonly HashSet<string> _groups = new HashSet<string>();
        private readonly List<AffixDef> _candidates = new List<AffixDef>();
        private readonly List<AffixDef> _steered = new List<AffixDef>();
        private readonly List<AffixTierDef> _tiers = new List<AffixTierDef>();
        private readonly List<float> _weights = new List<float>();

        public AffixDraw(ItemStateBuilder builder, RollContext context)
        {
            Builder = builder;
            _context = context;
            SteerPending = context.Category != null;
            foreach (AffixRoll roll in builder.Affixes)
            {
                Occupy(roll.Id);
            }
        }

        public ItemStateBuilder Builder { get; }

        /// <summary>A category steer was asked for and no draw has applied it yet.</summary>
        public bool SteerPending { get; private set; }

        /// <summary>When set, only these ids are candidates (an essence's family draw); null = the whole pool.</summary>
        public HashSet<string>? Only { get; set; }

        /// <summary>Draws one affix and appends it. False (nothing changed) when the pool has no candidate.</summary>
        public bool TryAdd(bool mythicPool)
        {
            if (!TryDraw(mythicPool, out AffixRoll roll))
            {
                return false;
            }
            Builder.AddAffix(roll);
            Occupy(roll.Id);
            return true;
        }

        /// <summary>A Mythic-only draw that falls back to the regular pool when it has no candidate (RAR-5).</summary>
        public bool TryAddSpecial() => TryAdd(true) || TryAdd(false);

        /// <summary>Draws one affix without adding it (a swap puts it in place of the removed one).</summary>
        public bool TryDraw(bool mythicPool, out AffixRoll roll)
        {
            roll = default;
            List<AffixDef> pool = Candidates(mythicPool);
            AffixDef? def = PickAffix(pool);
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

        // The steered subset when the steer is still pending and the category has a candidate; else every candidate.
        private List<AffixDef> Candidates(bool mythicPool)
        {
            Gather(mythicPool);
            if (!SteerPending || _candidates.Count == 0)
            {
                return _candidates;
            }
            _steered.Clear();
            foreach (AffixDef def in _candidates)
            {
                if (def.Category == _context.Category)
                {
                    _steered.Add(def);
                }
            }
            if (_steered.Count == 0)
            {
                return _candidates;
            }
            SteerPending = false;
            return _steered;
        }

        private void Gather(bool mythicPool)
        {
            _candidates.Clear();
            IReadOnlyList<AffixDef> pool = _context.Rules.Affixes.Pool(_context.Slot.Slot, mythicPool);
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
            if (!def.Enabled || def.Weight <= 0f || _ids.Contains(def.Id) || def.Id == _context.ExcludeId
                || (Only != null && !Only.Contains(def.Id)))
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
