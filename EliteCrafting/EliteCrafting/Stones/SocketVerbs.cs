using System;
using EliteCrafting.Affixes;
using EliteCrafting.Core;
using EliteCrafting.Items;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>socket</c> (sockets.md section 3, the Jeweller's Chisel): one more socket, while the item has fewer than the
    /// stone's <c>max_sockets</c> (default 2). A dropped item may carry more; the chisel never adds to those. Any rarity,
    /// Common included; affixes, gems and catalyst are untouched.
    /// </summary>
    internal sealed class SocketVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            int sockets = job.State.SocketCount;
            if (sockets >= Math.Min(job.Def!.MaxSockets, StoneDef.SocketLimit))
            {
                return StoneResult.Refuse("sockets_full", job.StoneName);
            }
            ItemState next = job.State.ToBuilder().SetSockets(sockets + 1).Build();
            return StoneResult.Success(next, "socket_added", job.ItemName, Numbers.Format(sockets + 1));
        }
    }

    /// <summary>
    /// <c>gem</c> (sockets.md section 4): the gem's affix for the item's slot, rolled in the item's own tier window, goes
    /// into the first empty socket. With every socket full the oldest gem breaks and the others move up, and the use
    /// asks first. Refused without a socket, or when the gem names no affix this item can roll.
    /// </summary>
    internal sealed class GemVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (job.State.SocketCount == 0)
            {
                return StoneResult.Refuse("no_sockets");
            }
            AffixDef? affix = job.Rules.Affix(job.Def!.GemAffix(job.Slot.Slot));
            bool fits = affix != null && affix.Enabled && ItemSlots.Satisfies(job.Slot, affix.Requires);
            AffixRoll? roll = fits ? GemRolls.Roll(affix!, job.RollContext(null)) : null;
            if (roll == null)
            {
                return StoneResult.Refuse("gem_no_fit", job.StoneName);
            }
            ItemStateBuilder builder = job.State.ToBuilder();
            SocketGem? broken = builder.SetGem(new SocketGem(job.Def.Id, roll.Value));
            ItemState next = builder.Build();
            return broken == null
                ? StoneResult.Success(next, "gem_set", job.ItemName, job.StoneName)
                : StoneResult.Breaking(next, "gem_replaced", job.ItemName, GemName(job, broken.Value), job.StoneName);
        }

        private static string GemName(StoneJob job, SocketGem gem) =>
            Words.Localize(job.Rules.Stone(gem.GemId)?.Name ?? "$ecf_stone_" + gem.GemId);
    }

    /// <summary>
    /// <c>catalyse</c> (sockets.md section 5): catalyst quality on the item rises by <c>step</c> up to <c>cap</c>, and
    /// the item's affixes and gems of the catalyst's family count that much more. A catalyst of another family replaces
    /// the item's catalyst and starts again from one step; when that throws quality away, the use asks first. Refused
    /// when nothing on the item belongs to the family, and at the cap.
    /// </summary>
    internal sealed class CatalyseVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            StoneDef def = job.Def!;
            EssenceFamilyDef? family = job.Rules.Economy.Family(def.Family);
            string familyName = family == null ? def.Family ?? "" : Words.Localize(family.Name);
            if (family == null || !HasMember(job.State, family))
            {
                return StoneResult.Refuse("catalyst_no_match", job.ItemName, familyName);
            }
            bool same = job.State.CatalystFamily == def.Family;
            float current = same ? job.State.CatalystQuality : 0f;
            if (current >= def.Cap)
            {
                return StoneResult.Refuse("catalyst_capped", job.StoneName);
            }
            float next = Math.Min(current + def.Step, def.Cap);
            ItemState state = job.State.ToBuilder().SetCatalyst(def.Family, next).Build();
            bool resets = !same && job.State.CatalystFamily != null && job.State.CatalystQuality > 0f;
            return resets
                ? StoneResult.Breaking(state, "catalyst_switched", job.ItemName, familyName, Numbers.Format(next))
                : StoneResult.Success(state, "catalysed", job.ItemName, familyName, Numbers.Format(next));
        }

        // An affix or a gem of the family, active or not: the catalyst waits on a dormant one rather than refusing.
        private static bool HasMember(ItemState state, EssenceFamilyDef family)
        {
            foreach (string id in family.Affixes)
            {
                if (state.HasAffix(id) || HasGem(state, id))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasGem(ItemState state, string affixId)
        {
            for (int i = 0; i < state.Gems.Count; i++)
            {
                if (state.Gems[i].Roll.Id == affixId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
