using EliteCrafting.Config;
using EliteCrafting.Items;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// The check pipeline of applying-stones.md section 2, steps 1-12, in order, for one rune click; the first failure refuses. Every step
    /// only reads: the verb's dry run (steps 11-12) works on a copy, and its result is what <see cref="StoneCommit"/>
    /// writes. The confirm gate (13) and the commit (14) follow in <see cref="ConfirmGate"/>.
    /// <para>
    /// Runs on the local client that owns the inventory (Valheim trusts a client with its own inventory). Every check
    /// reads the item or the synced rules only, so a host and a host-less client on a dedicated server refuse
    /// identically, and a client cannot change the odds by editing its files while the server binds them.
    /// </para>
    /// </summary>
    internal static class StonePipeline
    {
        public static StoneResult Evaluate(StoneJob job)
        {
            return ItemChecks(job) ?? StoneChecks(job) ?? RarityChecks(job) ?? StoneVerbs.Run(job);
        }

        // 1-3: own inventory, magic base, not a newer format.
        private static StoneResult? ItemChecks(StoneJob job)
        {
            if (!job.Inventory.ContainsItem(job.Stone) || !job.Inventory.ContainsItem(job.Target))
            {
                return StoneResult.Refuse("not_own_inventory");
            }
            if (!ItemClasses.IsMagicBase(job.Target))
            {
                return StoneResult.Refuse("not_magic_base");
            }
            return job.State.IsNewerFormat ? StoneResult.Refuse("newer_format") : null;
        }

        // 4-7: live and enabled rune this build can perform, not sealed, equipped rule.
        private static StoneResult? StoneChecks(StoneJob job)
        {
            if (!StoneVerbs.IsUsable(job.Def))
            {
                return StoneResult.Refuse("stone_disabled", job.StoneName);
            }
            // STN-1: a sealed item refuses every rune.
            if (job.State.IsSealed)
            {
                return StoneResult.Refuse("sealed");
            }
            return job.IsEquipped && !ModSettings.ModifyEquippedItems.Value ? StoneResult.Refuse("equipped") : null;
        }

        // 8-10: known rarity, in applies_to, enough runes in the carried stack.
        private static StoneResult? RarityChecks(StoneJob job)
        {
            if (job.Rarity == null)
            {
                return StoneResult.Refuse("unknown_rarity");
            }
            if (!job.Def!.AppliesToRarity(job.Rarity.Id))
            {
                return StoneResult.Refuse("wrong_rarity", job.StoneName, StoneNames.Rarity(job.Rarity));
            }
            if (job.Stone.m_stack < job.Cost)
            {
                return StoneResult.Refuse("not_enough_stones", job.Cost.ToString(), job.StoneName);
            }
            return null;
        }
    }
}
