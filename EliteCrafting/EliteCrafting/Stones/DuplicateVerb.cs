using EliteCrafting.Affixes;
using EliteCrafting.Core;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// <c>duplicate</c> (stones.md section 17: Reflection): the dry run leaves the original as it is and describes the
    /// copy - the same state (rarity, every affix with tier and value, bound and dormant ones, refine bonus, unreadable
    /// segments) without a pending sigil, sealed with reason <c>reflection</c> unless the stone says <c>seal_copy:
    /// false</c>. Needs a free slot in the player's inventory before the use (<c>inventory_full</c>); a stack the use
    /// would empty does not count (IMP-68). The copy item is made at commit by <see cref="ReflectionCopy"/>.
    /// </summary>
    internal sealed class DuplicateVerb : IStoneVerb
    {
        public StoneResult Run(StoneJob job)
        {
            if (!job.Inventory.HaveEmptySlot())
            {
                return StoneResult.Refuse("inventory_full");
            }
            ItemStateBuilder copy = job.State.ToBuilder().SetSigil(null);
            copy.Seal(job.Def!.SealCopy ? ItemKeys.SealedReflection : null);
            return StoneResult.Duplicate(job.State, copy.Build(), "reflected", job.ItemName);
        }
    }

    /// <summary>
    /// The copy Reflection adds (commit side, local client, the player's own inventory only). The game's own clone of
    /// the original keeps its upgrade level, durability, crafter, variant and world level (verified: <c>ItemData.Clone</c>
    /// is a member-wise copy plus a new custom-data dictionary); the per-instance fields that must not carry over are
    /// reset - the equipped flag (the copy is never equipped), the stack, the last attack and projectile. Our keys
    /// are written through the one write path, then <c>Inventory.AddItem(ItemData)</c> places the non-stackable
    /// copy in a free slot and raises the inventory change (weight, UI). The copy then travels in the game's own item
    /// serialization like any other item: no netcode.
    /// </summary>
    internal static class ReflectionCopy
    {
        /// <summary>The copy item, carrying its state, not yet in any inventory; null (logged) when the write is refused.</summary>
        public static ItemDrop.ItemData? Make(ItemDrop.ItemData original, ItemState state)
        {
            ItemDrop.ItemData copy = original.Clone();
            copy.m_equipped = false;
            copy.m_stack = 1;
            copy.m_lastAttackTime = 0f;
            copy.m_lastProjectile = null;
            if (!ItemState.Write(copy, state))
            {
                Log.Error("the Stone of Reflection's copy could not take its state; nothing consumed");
                return null;
            }
            return copy;
        }

        /// <summary>Adds the copy to the inventory; false (logged) when the inventory has no room after all.</summary>
        public static bool Place(Inventory inventory, ItemDrop.ItemData copy)
        {
            if (inventory.HaveEmptySlot() && inventory.AddItem(copy))
            {
                return true;
            }
            Log.Error("no free inventory slot for the Stone of Reflection's copy; nothing consumed");
            return false;
        }
    }
}
