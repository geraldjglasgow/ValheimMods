using System;

namespace EliteCraftingLink
{
    /// <summary>
    /// An item's EliteCrafting state (api.md section 4): rarity (<c>normal</c>, <c>magic</c>, <c>rare</c>) and its
    /// colour, the inscriptions as JSON (<c>[{ "id", "tier", "value", "affix", "active" }]</c>, tier 1 the strongest),
    /// the decorated name, and two writes: a fresh roll and a cleanse, on the item object handed in (call them where the
    /// item is owned, like a rune; a sealed item is never changed). Without EliteCrafting every call answers false or null.
    /// </summary>
    public static class CraftingItems
    {
        private static readonly Endpoint<Func<ItemDrop.ItemData, bool>> isMagic = new Endpoint<Func<ItemDrop.ItemData, bool>>("IsMagic");
        private static readonly Endpoint<Func<ItemDrop.ItemData, string?>> getRarity = new Endpoint<Func<ItemDrop.ItemData, string?>>("GetRarity");
        private static readonly Endpoint<Func<ItemDrop.ItemData, string?>> getRarityColor =
            new Endpoint<Func<ItemDrop.ItemData, string?>>("GetRarityColor");
        private static readonly Endpoint<Func<ItemDrop.ItemData, string?>> getInscriptionsJson =
            new Endpoint<Func<ItemDrop.ItemData, string?>>("GetInscriptionsJson");
        private static readonly Endpoint<Func<ItemDrop.ItemData, string?>> getDecoratedName =
            new Endpoint<Func<ItemDrop.ItemData, string?>>("GetDecoratedName");
        private static readonly Endpoint<Func<ItemDrop.ItemData, bool>> canBeMagic = new Endpoint<Func<ItemDrop.ItemData, bool>>("CanBeMagic");
        private static readonly Endpoint<Func<ItemDrop.ItemData, string, bool>> rollMagic =
            new Endpoint<Func<ItemDrop.ItemData, string, bool>>("RollMagic");
        private static readonly Endpoint<Func<ItemDrop.ItemData, bool>> cleanse = new Endpoint<Func<ItemDrop.ItemData, bool>>("Cleanse");

        public static bool IsMagic(ItemDrop.ItemData item) => Safe.Call(isMagic.Call, item, false);

        /// <summary>The rarity id; null without EliteCrafting.</summary>
        public static string? GetRarity(ItemDrop.ItemData item) => Safe.Call(getRarity.Call, item, null);

        /// <summary>The rarity's colour as <c>#RRGGBB</c>; null without EliteCrafting.</summary>
        public static string? GetRarityColor(ItemDrop.ItemData item) => Safe.Call(getRarityColor.Call, item, null);

        public static string? GetInscriptionsJson(ItemDrop.ItemData item) => Safe.Call(getInscriptionsJson.Call, item, null);

        /// <summary>The localized name as EliteCrafting's tooltip titles it, rarity colour included.</summary>
        public static string? GetDecoratedName(ItemDrop.ItemData item) => Safe.Call(getDecoratedName.Call, item, null);

        /// <summary>A magic base under the current rules and filters.</summary>
        public static bool CanBeMagic(ItemDrop.ItemData item) => Safe.Call(canBeMagic.Call, item, false);

        /// <summary>A fresh roll at the rarity id (the item must be a magic base).</summary>
        public static bool RollMagic(ItemDrop.ItemData item, string rarity) => Safe.Call(rollMagic.Call, item, rarity, false);

        /// <summary>Back to Normal.</summary>
        public static bool Cleanse(ItemDrop.ItemData item) => Safe.Call(cleanse.Call, item, false);
    }
}
