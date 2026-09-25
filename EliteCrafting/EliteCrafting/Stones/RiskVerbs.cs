using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>lock</c> (stones.md section 15: Binding): one random live affix is bound (<c>ecf_bound</c>). One bound affix
    /// at a time (ITD-3): refused while one is bound, so the item can be bound again only after Unmaking (or never, once
    /// the Serpent has sealed it). The item holds a single bound id, so a <c>max_bound</c> above 1 still binds one
    /// (IMP-66). Dormant affixes are never bound. No sigil steers it.
    /// </summary>
    internal sealed class LockVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (job.State.BoundId != null)
            {
                return StoneResult.Refuse("already_bound");
            }
            List<int> live = new List<int>();
            for (int i = 0; i < job.State.AffixCount; i++)
            {
                if (job.State.IsActiveAt(i))
                {
                    live.Add(i);
                }
            }
            if (live.Count == 0)
            {
                return StoneResult.Refuse("nothing_to_bind");
            }
            string id = job.State.Affixes[live[job.RollContext(null).Random.Next(live.Count)]].Id;
            ItemState bound = job.State.ToBuilder().SetBound(id).Build();
            return StoneResult.Success(bound, "bound", job.ItemName, StoneNames.Affix(job.Rules, id));
        }
    }

    /// <summary>
    /// <c>gamble</c> (stones.md section 16: Chance, on a Common): a rarity drawn by the stone's <c>weights</c> in
    /// ladder order (Mythic 0 by default, independent of <c>drop_weight</c>), then a fresh roll of that rarity. A pool
    /// that cannot fill the drawn rarity lowers it to the highest one it can fill; none refuses. A drawn base rarity
    /// (the owner's <c>common</c> weight) is a fizzle: the stone is spent, nothing changes, a pending sigil stays
    /// (IMP-67). The item is never destroyed; a category sigil steers one rolled affix.
    /// </summary>
    internal sealed class GambleVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            RollContext context = job.RollContext(job.Sigil.CategoryFor(StoneVerb.Gamble));
            RarityDef? drawn = Draw(job, context);
            if (drawn == null)
            {
                return StoneResult.Refuse("stone_disabled", job.StoneName);
            }
            if (drawn.IsBase)
            {
                return StoneResult.Unsteered(job.State, "gamble_fizzle", job.ItemName, job.StoneName);
            }
            return Roll(job, drawn, context);
        }

        // From the drawn rarity down to the first above the base: the highest one the pool can fill.
        private static StoneResult Roll(StoneJob job, RarityDef drawn, RollContext context)
        {
            for (RarityDef? rarity = drawn; rarity != null && !rarity.IsBase; rarity = job.Rules.Economy.Previous(rarity))
            {
                RollOutcome outcome = ItemRoller.RollFresh(job.State, rarity, context);
                if (!outcome.Success)
                {
                    continue;
                }
                if (!VerbSupport.SteerMet(job, outcome.State!, context.Category))
                {
                    return StoneResult.Refuse("sigil_no_match", job.Sigil.Name);
                }
                return StoneResult.Success(outcome.State!, "gambled", job.ItemName, StoneNames.Rarity(rarity));
            }
            return StoneResult.Refuse("no_eligible_affix");
        }

        private static RarityDef? Draw(StoneJob job, RollContext context)
        {
            IReadOnlyList<RarityDef> ladder = job.Rules.Economy.Rarities;
            List<float> weights = new List<float>(ladder.Count);
            for (int i = 0; i < ladder.Count; i++)
            {
                weights.Add(job.Def!.GambleWeights.TryGetValue(ladder[i].Id, out float w) ? w : 0f);
            }
            int index = RollMath.PickWeighted(weights, context.Random);
            return index < 0 ? null : ladder[index];
        }
    }
}
