using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// Everything one piece of terrain work costs the local player, worked out without charging: stamina, tool wear
    /// (durability points before the world's rate) and items. Used for special entries through <see cref="CostApi"/>
    /// and for the live cost line.
    /// </summary>
    internal sealed class WorkCost
    {
        public float Stamina;
        public float Wear;
        public UseCost Items;

        /// <summary>The cost of work with this estimate (null: no volume known, only materials).</summary>
        public static WorkCost Of(CostContext ctx, EditEstimate estimate)
        {
            float raised = estimate != null ? estimate.Raised : 0f;
            return new WorkCost
            {
                Stamina = StaminaCost.For(ctx, StaminaCost.Vanilla(ctx.Player)),
                Wear = WearCost.For(ctx, WearCost.Vanilla(ctx.Player, ctx.Tool)),
                Items = BillBuilder.For(ctx, raised, PavedArea.Of(estimate)),
            };
        }

        /// <summary>Takes it all from the local player: stamina, wear on the held tool, items and volume stone.</summary>
        public void Charge(CostContext ctx)
        {
            if (Stamina > 0f)
                ctx.Player.UseStamina(Stamina);
            ItemDrop.ItemData tool = ctx.Tool;
            if (Wear > 0f && tool != null && tool.m_shared.m_useDurability)
                tool.m_durability = Mathf.Max(0f, tool.m_durability - Wear * Game.m_durabilityRate);
            Inventory inventory = ctx.Player.GetInventory();
            Items.Items.PayFrom(inventory);
            Items.PayVolume(inventory);
        }
    }
}
