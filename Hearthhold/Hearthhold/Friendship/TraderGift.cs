using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Gifts: the player uses a kitchen product from the hotbar while hovering a trader (the game's
    /// <see cref="Humanoid.UseItem"/> hands it to <see cref="Trader.UseItem"/>). An item the trader itself takes (Hildir's
    /// chests and any other entry of <see cref="Trader.m_useItems"/>) stays the game's; every other dish is taken over,
    /// so it is not eaten instead: a plain one is refused, a starred one is given once per trader per in-game day for
    /// 20 + 20 per star friendship points, and one is removed from the inventory. Off with the switch: the game's own.
    /// </summary>
    public static class TraderGift
    {
        public const int BasePoints = 20;
        public const int PointsPerStar = 20;

        [HarmonyPatch(typeof(Trader), nameof(Trader.UseItem))]
        private static class Use
        {
            [HarmonyPrefix]
            private static bool Prefix(Trader __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
            {
                if (!Friendship.On || user == null || user != Player.m_localPlayer || !IsDish(item) || TraderTakes(__instance, item))
                    return true;
                Player player = (Player)user;
                bool handled = HookGuard.Run("trader gift", () => Give(__instance, player, item), false);
                if (handled)
                    __result = true;
                return !handled;
            }
        }

        private static bool IsDish(ItemDrop.ItemData item) =>
            item?.m_shared != null && item.m_dropPrefab != null && Kitchen.IsProduct(item.m_dropPrefab.name);

        /// <summary>Whether the trader's own use list takes this item (the game's quest items).</summary>
        private static bool TraderTakes(Trader trader, ItemDrop.ItemData item)
        {
            foreach (Trader.TraderUseItem use in trader.m_useItems)
            {
                if (use?.m_prefab != null && use.m_prefab.m_itemData.m_shared.m_name == item.m_shared.m_name)
                    return true;
            }
            return false;
        }

        /// <summary>Refuses or takes the gift; true once the game's own handling should be skipped.</summary>
        private static bool Give(Trader trader, Player player, ItemDrop.ItemData item)
        {
            if (EnvMan.instance == null)
                return false;
            TraderInfo info = TraderInfo.Of(trader);
            int stars = Stars.Get(item);
            int day = EnvMan.instance.GetDay();
            if (stars < 1)
                player.Message(MessageHud.MessageType.Center, $"{info.DisplayName} only accepts starred food as a gift.");
            else if (Friendship.LastGiftDay(player, info) == day)
                player.Message(MessageHud.MessageType.Center, "Come back tomorrow.");
            else
                Accept(info, player, item, stars, day);
            return true;
        }

        private static void Accept(TraderInfo info, Player player, ItemDrop.ItemData item, int stars, int day)
        {
            string dish = Stars.TierName(stars) + " " + Localization.instance.Localize(item.m_shared.m_name);
            if (!player.GetInventory().RemoveItem(item, 1))
                return;
            int hearts = Friendship.AddGift(player, info, BasePoints + PointsPerStar * stars, day);
            string line = $"{info.DisplayName} likes the {dish}. {Friendship.Line(hearts)}";
            player.Message(MessageHud.MessageType.Center, line);
        }
    }
}
