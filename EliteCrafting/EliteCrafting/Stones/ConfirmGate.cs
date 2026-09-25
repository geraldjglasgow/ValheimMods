using EliteCrafting.Config;
using EliteCrafting.Text;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// Pipeline step 13 (applying-stones.md section 4): stones with <c>confirm: true</c> (synced, per stone) ask
    /// first, in the way this player chose (<c>Confirm destructive stones</c>, local and unsynced). Last on purpose:
    /// only a use that would succeed is ever confirmed. Local player only.
    /// <list type="bullet">
    /// <item><c>HoldShift</c>: applies only while Shift / the gamepad left trigger is held - the grid reports that as
    /// <c>Modifier.Split</c>, which vanilla ignores while an item is carried.</item>
    /// <item><c>Dialog</c>: the game's yes/no popup; Yes re-runs steps 1-12 (the inventory may have changed while it was
    /// open) and commits that fresh result. Falls back to HoldShift when no popup host exists.</item>
    /// <item><c>Off</c>: no gate.</item>
    /// </list>
    /// </summary>
    internal static class ConfirmGate
    {
        public static void Pass(StoneJob job, StoneResult result, bool shiftHeld)
        {
            ConfirmMode mode = ModSettings.ConfirmDestructiveStones?.Value ?? ConfirmMode.HoldShift;
            if (!job.Def!.Confirm || mode == ConfirmMode.Off || (mode == ConfirmMode.HoldShift && shiftHeld))
            {
                StoneCommit.Commit(job, result);
                return;
            }
            if (mode == ConfirmMode.Dialog && UnifiedPopup.IsAvailable())
            {
                Ask(job);
                return;
            }
            if (shiftHeld)
            {
                StoneCommit.Commit(job, result);
                return;
            }
            StoneFeedback.Show(job.Player, StoneResult.Refuse("confirm_required", job.StoneName).Refusal!);
        }

        private static void Ask(StoneJob job)
        {
            string title = Words.Localize("$ecf_ui_confirm_title", job.StoneName);
            string body = Words.Localize("$ecf_ui_confirm_body", job.ItemName, job.StoneName);
            ItemDrop.ItemData stone = job.Stone;
            ItemDrop.ItemData target = job.Target;
            UnifiedPopup.Push(new YesNoPopup(title, body, () => Confirmed(stone, target), UnifiedPopup.Pop, localizeText: false));
        }

        private static void Confirmed(ItemDrop.ItemData stone, ItemDrop.ItemData target)
        {
            UnifiedPopup.Pop();
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            StoneJob job = StoneJob.Create(player, stone, target);
            StoneResult result = StonePipeline.Evaluate(job);
            if (result.Refused)
            {
                StoneFeedback.Show(player, result.Refusal!);
                return;
            }
            StoneCommit.Commit(job, result);
        }
    }
}
