using EarthWright.Terrain;

namespace EarthWright.Costs
{
    /// <summary>
    /// Brush clicks through the game's placement (edits flagged FromPlacement). The sender guard runs in the placement
    /// hook before the game places anything: it refuses a click inside the cooldown, or one the player cannot pay -
    /// the entry's materials, the extra item and the volume stone of <c>Engine.Estimate</c> counted together. When
    /// the edit has been sent, the volume stone is taken and the cooldown starts; the game then charges the rest
    /// (materials, stamina, wear) through the patched ConsumeResources, GetBuildStamina and GetPlaceDurability, using
    /// the materials worked out here. A refused click reaches none of this, so it costs nothing.
    /// </summary>
    internal static class ClickGuard
    {
        private static TerrainEdit pendingEdit;
        private static UseCost pendingCost;
        private static Bill committedItems;

        /// <summary>The sender guard registered as "costs".</summary>
        public static string Check(GuardContext context)
        {
            TerrainEdit edit = context.Edit;
            pendingEdit = null;
            string wait = Cooldown.Check(edit);
            if (wait != null || !edit.Has(EditFlags.FromPlacement))
                return wait;
            CostContext ctx = CostContext.ForSelected(Player.m_localPlayer);
            if (ctx == null)
                return null;
            UseCost cost = CostOf(ctx, edit);
            string reason = Affordability.ShortOf(ctx.Player, cost.Total);
            if (reason != null)
                return reason;
            pendingEdit = edit;
            pendingCost = cost;
            return null;
        }

        /// <summary>EditEvents.Sent: the checked click left, so its volume stone is paid and the cooldown starts.</summary>
        public static void OnSent(TerrainEdit edit)
        {
            if (edit == null)
                return;
            Cooldown.OnSent(edit);
            if (edit != pendingEdit)
                return;
            pendingEdit = null;
            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            pendingCost.PayVolume(player.GetInventory());
            committedItems = pendingCost.Items;
        }

        /// <summary>The materials of the click just sent, once, for the game's ConsumeResources; null when none is waiting.</summary>
        public static Bill TakeCommittedItems()
        {
            Bill items = committedItems;
            committedItems = null;
            return items;
        }

        /// <summary>Forgets a click the game did not finish charging (called when a new placement frame starts).</summary>
        public static void ForgetCommitted() => committedItems = null;

        /// <summary>The click's items; the terrain estimate is only made while a volume cost is set and applies.</summary>
        private static UseCost CostOf(CostContext ctx, TerrainEdit edit)
        {
            if (!VolumeCharge.Enabled || VolumeCharge.Exempt(edit) || BillBuilder.MaterialsFree(ctx.Player))
                return BillBuilder.For(ctx, 0f, 0f);
            // The preview's estimate of this same click when it is fresh, so a held click does not plan twice.
            EditEstimate estimate = LiveEstimate.For(edit, 0, LiveEstimate.ClickAge);
            return BillBuilder.For(ctx, estimate != null ? estimate.Raised : 0f, PavedArea.Of(estimate));
        }
    }
}
