using System.Collections.Generic;
using EliteCrafting.Config;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;
using EliteCrafting.Stones;
using EliteCrafting.Text;
using UnityEngine;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// One grind (salvage.md sections 2-4): the checks, the confirm gate in the player's own mode
    /// (<c>Confirm destructive stones</c>, when <c>salvage.confirm</c>), then in one frame the item removed and the
    /// shards added, each chance row rolled on its own with local dice under the synced odds. Local client, own
    /// inventory only; nothing is sent (the inventory is saved and replicated by the game).
    /// </summary>
    internal static class Grinder
    {
        public static void Request(Player player, ItemDrop.ItemData item, bool shiftHeld)
        {
            GrindJob job = new GrindJob(player, item);
            StoneMessage? refusal = GrindChecks.Evaluate(job);
            if (refusal != null)
            {
                StoneFeedback.Show(player, refusal);
                return;
            }
            Confirm(job, shiftHeld);
        }

        // Step 11, as the stones' gate: HoldShift grinds only with Shift; Dialog asks (Shift means nothing there);
        // Off grinds at once. Without a popup host, Dialog falls back to HoldShift.
        private static void Confirm(GrindJob job, bool shiftHeld)
        {
            ConfirmMode mode = ModSettings.ConfirmDestructiveStones?.Value ?? ConfirmMode.HoldShift;
            if (!job.Salvage.Confirm || mode == ConfirmMode.Off || (mode == ConfirmMode.HoldShift && shiftHeld))
            {
                Commit(job);
                return;
            }
            if (mode == ConfirmMode.Dialog && UnifiedPopup.IsAvailable())
            {
                Ask(job);
                return;
            }
            if (shiftHeld)
            {
                Commit(job);
                return;
            }
            StoneFeedback.Show(job.Player, new StoneMessage("salvage_confirm", new[] { job.ItemName }));
        }

        private static void Ask(GrindJob job)
        {
            string title = Words.Localize("$ecf_ui_salvage_title", job.ItemName);
            string body = Words.Localize("$ecf_ui_salvage_body", job.ItemName, GrindText.Preview(job));
            ItemDrop.ItemData item = job.Item;
            UnifiedPopup.Push(new YesNoPopup(title, body, () => Confirmed(item), UnifiedPopup.Pop, localizeText: false));
        }

        // Yes re-runs every check: the inventory may have changed while the dialog was open.
        private static void Confirmed(ItemDrop.ItemData item)
        {
            UnifiedPopup.Pop();
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            GrindJob job = new GrindJob(player, item);
            StoneMessage? refusal = GrindChecks.Evaluate(job);
            if (refusal != null)
            {
                StoneFeedback.Show(player, refusal);
                return;
            }
            Commit(job);
        }

        private static void Commit(GrindJob job)
        {
            Dictionary<string, int> gained = Roll(job.Rows, RollRandom.Create());
            // As the game's own drop does: a pending equip and a sheathed (hidden) hand item let go of it, or the
            // ground weapon would stay drawn on the player's back for every peer. Not equipped (checked), so no unequip effects.
            job.Player.RemoveEquipAction(job.Item);
            job.Player.UnequipItem(job.Item, triggerEquipEffects: false);
            if (!job.Inventory.RemoveItem(job.Item))
            {
                return;
            }
            foreach (KeyValuePair<string, int> kind in gained)
            {
                GameObject? prefab = Items.StonePrefabs.GetShard(kind.Key);
                if (prefab != null)
                {
                    ShardRoom.Add(job.Inventory, prefab, kind.Value);
                }
            }
            StoneFeedback.Show(job.Player, new StoneMessage("ground", new[] { job.ItemName, GrindText.Gained(gained) }));
            GrindText.PlaySound();
        }

        // Each row on its own: `chance` percent to pay its `amount` (salvage.md section 4).
        internal static Dictionary<string, int> Roll(IReadOnlyList<SalvageYield> rows, System.Random random)
        {
            Dictionary<string, int> gained = new Dictionary<string, int>(System.StringComparer.Ordinal);
            foreach (SalvageYield row in rows)
            {
                if (row.Chance >= 100f || random.NextDouble() * 100.0 < row.Chance)
                {
                    gained.TryGetValue(row.Fragment, out int sum);
                    gained[row.Fragment] = sum + row.Amount;
                }
            }
            return gained;
        }
    }
}
