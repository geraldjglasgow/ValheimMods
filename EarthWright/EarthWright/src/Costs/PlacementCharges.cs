namespace EarthWright.Costs
{
    /// <summary>
    /// Where in the game's placement the local player is, and what the cost patches answer there. The game's click in
    /// <c>Player.UpdatePlacement</c> goes: stamina check (HaveStamina with the tool's attack stamina), requirement check
    /// (HaveRequirements), <c>TryPlacePiece</c> (EarthWright's placement hook builds and guards the edit there, special
    /// entries run their handler there), and only when that placed the piece: ConsumeResources, UseStamina with
    /// GetBuildStamina, and the tool's durability minus GetPlaceDurability. The patches change these answers for the
    /// local player's terrain entries only; special entries pass the game's checks and pay through CostApi when they
    /// commit work.
    /// </summary>
    internal static class PlacementCharges
    {
        /// <summary>Inside the local player's Player.UpdatePlacement.</summary>
        public static bool InUpdate;

        /// <summary>Inside Player.TryPlacePiece (special handlers run there and see the game's own answers).</summary>
        public static int TryDepth;

        /// <summary>A terrain piece was just placed by the game's click; the game is charging it now.</summary>
        public static CostContext Charging;

        public static void BeginUpdate(Player player)
        {
            InUpdate = player == Player.m_localPlayer;
            Charging = null;
            ClickGuard.ForgetCommitted();
        }

        public static void EndUpdate()
        {
            InUpdate = false;
            Charging = null;
        }

        /// <summary>TryPlacePiece postfix: a placed brush entry opens the charging window for the rest of the click.</summary>
        public static void AfterPlace(Player player, Piece piece, bool placed)
        {
            if (!placed || !InUpdate || player != Player.m_localPlayer)
                return;
            CostContext ctx = CostContext.ForPiece(player, piece);
            if (ctx != null && !ctx.Action.IsSpecial)
                Charging = ctx;
        }

        /// <summary>
        /// The game's stamina check before a click: null keeps it, 0 or less lets the click through, else the amount
        /// the player must have more than. Only the check with the tool's own attack stamina is the game's click check.
        /// </summary>
        public static float? StaminaCheck(Player player, float requested)
        {
            ItemDrop.ItemData tool = player.GetRightItem();
            if (tool == null || requested != tool.m_shared.m_attack.m_attackStamina)
                return null;
            CostContext ctx = CostContext.ForSelected(player);
            if (ctx == null)
                return null;
            if (ctx.Action.IsSpecial || FreeBuild.On)
                return 0f;
            if (StaminaCost.IsVanilla(ctx))
                return null;
            return StaminaCost.For(ctx, StaminaCost.Vanilla(player));
        }

        /// <summary>The game's requirement check for a terrain entry: null keeps the game's answer.</summary>
        public static bool? CanStart(Player player, Piece piece)
        {
            CostContext ctx = CostContext.ForPiece(player, piece);
            if (ctx == null)
                return null;
            if (ctx.Action.IsSpecial || FreeBuild.On)
                return true;
            if (piece.m_dlc.Length > 0 && (DLCMan.instance == null || !DLCMan.instance.IsDLCInstalled(piece.m_dlc)))
                return false;
            if (StationRule.Missing(ctx) != null)
                return false;
            return BillBuilder.Items(ctx).FirstShort(player.GetInventory()) < 0;
        }

        /// <summary>What the game's ConsumeResources takes for the placed click: the materials checked by the guard.</summary>
        public static Piece.Requirement[] Materials(CostContext ctx)
        {
            Bill items = ClickGuard.TakeCommittedItems() ?? BillBuilder.Items(ctx);
            return items.ToRequirements();
        }
    }
}
