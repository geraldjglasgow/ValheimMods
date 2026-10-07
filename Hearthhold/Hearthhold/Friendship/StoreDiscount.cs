using System.Collections.Generic;
using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// The friendship discount on buying (<see cref="Friendship.DiscountPercent"/>), selling stays the game's. The game's
    /// store reads one price, <see cref="Trader.TradeItem.m_price"/>: for the list (<see cref="StoreGui.FillList"/>) and
    /// what a purchase takes (<see cref="StoreGui.BuySelectedItem"/>). Around those two calls the open trader's items carry
    /// the discounted price and get their own back after (a finalizer, so even a failing call restores them); the buy
    /// button's every-frame affordability check (<see cref="StoreGui.CanAfford"/>) compares the discounted price itself
    /// instead. Opening a store where the player has a discount says so top-left.
    /// </summary>
    public static class StoreDiscount
    {
        private static readonly List<Trader.TradeItem> lowered = new List<Trader.TradeItem>();
        private static readonly List<int> prices = new List<int>();
        private static int depth;

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.FillList))]
        private static class Fill
        {
            [HarmonyPrefix]
            private static void Prefix(StoreGui __instance) => Enter(__instance);

            [HarmonyFinalizer]
            private static void Finalizer() => Exit();
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.BuySelectedItem))]
        private static class Buy
        {
            [HarmonyPrefix]
            private static void Prefix(StoreGui __instance) => Enter(__instance);

            [HarmonyFinalizer]
            private static void Finalizer() => Exit();
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.CanAfford))]
        private static class Afford
        {
            [HarmonyPrefix]
            private static bool Prefix(StoreGui __instance, Trader.TradeItem item, ref bool __result)
            {
                int percent = depth > 0 || item == null ? 0 : Percent(__instance);
                if (percent <= 0)
                    return true;
                __result = Friendship.Discounted(item.m_price, percent) <= __instance.GetPlayerCoins();
                return false;
            }
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
        private static class Open
        {
            [HarmonyPostfix]
            private static void Postfix(Trader trader) => HookGuard.Run("store discount notice", static t => Tell(t), trader);
        }

        /// <summary>The local player's discount at the store's open trader, 0 without one.</summary>
        private static int Percent(StoreGui gui) =>
            gui.m_trader == null ? 0 : Friendship.DiscountPercent(Player.m_localPlayer, TraderInfo.Of(gui.m_trader));

        /// <summary>Lowers the open trader's prices, on the outermost call only (a purchase refills the list inside).</summary>
        private static void Enter(StoreGui gui)
        {
            if (depth++ > 0)
                return;
            HookGuard.Run("store discount", static g => Lower(g), gui);
        }

        private static void Lower(StoreGui gui)
        {
            int percent = Percent(gui);
            if (percent <= 0)
                return;
            foreach (Trader.TradeItem item in gui.m_trader.GetAvailableItems())
            {
                if (item == null || lowered.Contains(item))
                    continue;
                lowered.Add(item);
                prices.Add(item.m_price);
                item.m_price = Friendship.Discounted(item.m_price, percent);
            }
        }

        /// <summary>Gives every lowered item its own price back once the outermost call is done.</summary>
        private static void Exit()
        {
            if (depth > 0 && --depth > 0)
                return;
            for (int i = 0; i < lowered.Count; i++)
                lowered[i].m_price = prices[i];
            lowered.Clear();
            prices.Clear();
        }

        private static void Tell(Trader trader)
        {
            Player player = Player.m_localPlayer;
            if (trader == null || player == null)
                return;
            TraderInfo info = TraderInfo.Of(trader);
            if (Friendship.DiscountPercent(player, info) > 0)
                player.Message(MessageHud.MessageType.TopLeft, $"{info.DisplayName}: {Friendship.Line(Friendship.Hearts(player, info))}");
        }
    }
}
