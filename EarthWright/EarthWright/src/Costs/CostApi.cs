using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;

namespace EarthWright.Costs
{
    /// <summary>
    /// Costs for work that does not go through the game's own placement (special entries: ramps, roads, clearing,
    /// custom entries; or any brush stroke a module sends itself). Brush clicks through the placement are charged by the
    /// game, adjusted by the Costs module's patches. Everything is the local player's: their stamina, the tool in
    /// their hand, their inventory. Call <see cref="TryCharge"/> once per committed piece of work, before sending its
    /// edits; the edits it sends right after are not held back by the cooldown.
    /// </summary>
    public static class CostApi
    {
        /// <summary>
        /// Checks that the player can pay for this work and charges it: stamina, durability, resources and volume costs
        /// as configured. False (with a message) when the player cannot pay; nothing is charged then.
        /// <paramref name="estimate"/> is the work's <c>Engine.Estimate</c> (null: no volume cost).
        /// </summary>
        public static bool TryCharge(Player player, ToolAction action, EditEstimate estimate)
        {
            return Safe.Call("EarthWright costs", () => Charge(player, action, estimate), true);
        }

        /// <summary>Seconds until the cooldown lets the next terrain use start (0: now), for repeating tools that should wait instead of giving up.</summary>
        public static float CooldownRemaining => Safe.Call("EarthWright costs", () => Cooldown.Remaining, 0f);

        /// <summary>The same check without charging, for the preview (null = affordable, else the reason). The cooldown is not part of it.</summary>
        public static string CannotPay(Player player, ToolAction action, EditEstimate estimate)
        {
            return Safe.Call("EarthWright costs", () => Check(player, action, estimate), null);
        }

        private static string Check(Player player, ToolAction action, EditEstimate estimate)
        {
            CostContext ctx = ContextFor(player, action, estimate);
            return ctx != null ? Affordability.Reason(ctx, WorkCost.Of(ctx, estimate)) : null;
        }

        /// <summary>The work's context; work that paves ground counts as paving for the stonecutter rule, whatever paint the brush shows.</summary>
        private static CostContext ContextFor(Player player, ToolAction action, EditEstimate estimate)
        {
            CostContext ctx = CostContext.ForAction(player, action);
            if (ctx != null && estimate != null && estimate.PavedCells > 0)
                ctx.Paint = PaintOp.Paved;
            return ctx;
        }

        private static bool Charge(Player player, ToolAction action, EditEstimate estimate)
        {
            CostContext ctx = ContextFor(player, action, estimate);
            if (ctx == null)
                return true;
            string reason = Cooldown.Waiting();
            WorkCost cost = WorkCost.Of(ctx, estimate);
            reason = reason ?? Affordability.Reason(ctx, cost);
            if (reason != null)
            {
                Refuse(reason);
                return false;
            }
            cost.Charge(ctx);
            Cooldown.MarkUse(true);
            return true;
        }

        /// <summary>Shows the reason; a stamina refusal also flashes the stamina bar, as the game does.</summary>
        private static void Refuse(string reason)
        {
            Messages.Center(reason);
            if (reason == Language.Localize(CostWords.NoStamina) && Hud.instance != null)
                Hud.instance.StaminaBarEmptyFlash();
        }
    }
}
