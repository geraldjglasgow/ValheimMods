using EliteCrafting.Affixes;
using EliteCrafting.Core;
using EliteCrafting.Text;
using UnityEngine;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// Pipeline step 14 (applying-stones.md section 3), in one frame on the owning client: write the dry-run state
    /// (one write; it already carries a spent sigil's clearing), place a Reflection copy, take the cost from the
    /// carried stack, keep the rest of the stack on the cursor, then the feedback. An equipped target needs nothing more here: the write raises
    /// <see cref="ItemStateCache.Written"/>, which the effects area listens to for its rebuild.
    /// <para>
    /// Multiplayer: nothing is sent anywhere. The new state lives in the item's <c>m_customData</c>, which the game
    /// saves with the player and serializes into chests, the ground, tombstones and trades, so other players see it
    /// when the item next leaves this inventory. No RPC is needed (Phase 3's transient visuals will add one).
    /// </para>
    /// </summary>
    internal static class StoneCommit
    {
        public static void Commit(StoneJob job, StoneResult result)
        {
            ItemDrop.ItemData? copy = result.CopyState == null ? null : ReflectionCopy.Make(job.Target, result.CopyState);
            if (result.CopyState != null && copy == null)
            {
                return;
            }
            if (!ItemState.Write(job.Target, result.State!))
            {
                Log.Error($"stone '{job.Def?.Id}' passed every check but the item state write was refused; nothing consumed");
                return;
            }
            if (copy != null && !ReflectionCopy.Place(job.Inventory, copy))
            {
                return;
            }
            Pay(job);
            KeepCarrying(job);
            StoneFeedback.Success(job.Player, result);
        }

        // The cost leaves the carried stack in the same frame as the write (and a Reflection copy's placement), so no
        // use can ever land without being paid for.
        private static void Pay(StoneJob job)
        {
            if (job.Cost > 0)
            {
                job.Inventory.RemoveItem(job.Stone, job.Cost);
            }
        }

        // The stack stays on the cursor for the next click, its carried amount clamped to what is left; used up, the drag clears.
        private static void KeepCarrying(StoneJob job)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_dragItem != job.Stone)
            {
                return;
            }
            if (job.Stone.m_stack <= 0 || !job.Inventory.ContainsItem(job.Stone))
            {
                // As vanilla does when a split drag ends: without it the grids' drop-focus overlay stays on.
                gui.SetupDragItem(null, null, 1);
                gui.IsSplitDropping = false;
                return;
            }
            gui.m_dragAmount = Mathf.Clamp(gui.m_dragAmount, 1, job.Stone.m_stack);
        }
    }

    /// <summary>
    /// What the player sees (applying-stones.md section 6, Phase 1): the message in the center of the screen, the
    /// vanilla item-move sound on success, and a top-left line when a pending sigil was spent. Local player only.
    /// A distinct refusal sound, slot flash and stone-family sounds are Phase 3.
    /// </summary>
    internal static class StoneFeedback
    {
        public static void Show(Player player, StoneMessage message)
        {
            player.Message(MessageHud.MessageType.Center, Words.Localize(message.Key, message.Words));
        }

        public static void Success(Player player, StoneResult result)
        {
            if (result.Feedback != null)
            {
                Show(player, result.Feedback);
            }
            if (result.SpentSigilName != null)
            {
                string text = Words.Localize("$ecf_msg_sigil_spent", "", result.SpentSigilName);
                player.Message(MessageHud.MessageType.TopLeft, text);
            }
            InventoryGui gui = InventoryGui.instance;
            if (gui != null)
            {
                gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
            }
        }
    }
}
