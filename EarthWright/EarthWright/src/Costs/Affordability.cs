using EarthWright.Core;

namespace EarthWright.Costs
{
    /// <summary>
    /// Why a terrain use cannot be paid, worded for the player: a missing station, too little stamina, a broken tool
    /// or too few items. Null when it can be paid. Nothing is ever charged here.
    /// </summary>
    internal static class Affordability
    {
        /// <summary>"Not enough Stone (3 of 5)" for the first item the inventory is short of, or null.</summary>
        public static string ShortOf(Player player, Bill bill)
        {
            Inventory inventory = player != null ? player.GetInventory() : null;
            int index = bill.FirstShort(inventory);
            if (index < 0)
                return null;
            BillLine line = bill.Lines[index];
            return CostWords.Format(CostWords.Short, Language.Localize(line.Name), Bill.Have(inventory, line).ToString(), line.Amount.ToString());
        }

        /// <summary>Everything a use needs, checked in the order a player fixes them: station, stamina, tool, items.</summary>
        public static string Reason(CostContext ctx, WorkCost cost)
        {
            if (FreeBuild.On)
                return null;
            string station = StationRule.Missing(ctx);
            if (station != null)
                return CostWords.Format(CostWords.NeedStation, Language.Localize(station));
            if (StaminaShort(ctx, cost.Stamina))
                return Language.Localize(CostWords.NoStamina);
            if (ToolBroken(ctx, cost.Wear))
                return Language.Localize(CostWords.Broken);
            return ShortOf(ctx.Player, cost.Items.Total);
        }

        /// <summary>The player has no more stamina than the use costs (the game's own rule for placing).</summary>
        public static bool StaminaShort(CostContext ctx, float stamina)
        {
            return stamina > 0f && ctx.Player.GetStamina() <= stamina;
        }

        private static bool ToolBroken(CostContext ctx, float wear)
        {
            ItemDrop.ItemData tool = ctx.Tool;
            return wear > 0f && tool != null && tool.m_shared.m_useDurability && tool.m_durability <= 0f;
        }
    }
}
