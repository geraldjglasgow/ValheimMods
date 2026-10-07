namespace Hearthhold
{
    /// <summary>
    /// Every name the Shipping Crate stores or sends: its prefab, its ZDO keys and its RPC. All start with "hearthhold_".
    /// </summary>
    public static class ShippingKeys
    {
        /// <summary>The crate's prefab name (a copy of the game's wooden chest).</summary>
        public const string Prefab = "hearthhold_shipping_crate";

        /// <summary>ZDO int: the last dawn day this crate sold on (or first saw), -1 when never seen.</summary>
        public const string Day = "hearthhold_shipping_day";

        /// <summary>ZDO int: how many items the last sale bought.</summary>
        public const string SoldItems = "hearthhold_shipping_items";

        /// <summary>ZDO int: how many coins the last sale paid.</summary>
        public const string SoldCoins = "hearthhold_shipping_coins";

        /// <summary>RPC (int items, int coins): a sale, owner to every peer that has the crate loaded.</summary>
        public const string RpcSold = "hearthhold_shipping_sold";
    }
}
