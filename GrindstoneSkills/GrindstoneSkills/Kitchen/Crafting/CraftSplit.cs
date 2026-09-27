using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Gives every crafted unit its own star. The game added the whole craft at quality 1 in one Inventory.AddItem,
    /// possibly into a 0-star stack the crafter already had, so the units that arrived are the rise in the 0-star
    /// count, plus what <see cref="CraftOverflow"/> held back for lack of room. Units that stay at 0 stars stay where
    /// the game put them. The others are taken back out with Inventory.RemoveItem(name, n, 1), which takes 0-star
    /// units only, and added again at their star's quality through the game's own AddItem overload with the crafter,
    /// variant and cheated flag the game used. Units below the station's trash filter are not added again. Units that
    /// no longer fit (starred stacks need their own slots) go to the ground at the crafter's feet.
    /// </summary>
    public static class CraftSplit
    {
        public static void Apply(KitchenCraftContext craft, float averageIngredientStars)
        {
            Inventory inventory = craft.Player.GetInventory();
            int arrived = Mathf.Max(0, inventory.CountItems(craft.Shared.m_name, Stars.ToQuality(0)) - craft.ZeroStarsBefore);
            int crafted = arrived + craft.Overflow;
            if (crafted <= 0)
                return;
            int[] rolled = CraftStars.Roll(crafted, averageIngredientStars);
            int minStars = KitchenFilter.MinStars(craft.StationView);
            int zeroKept = minStars == 0 ? rolled[0] : 0;
            if (arrived > zeroKept)
                inventory.RemoveItem(craft.Shared.m_name, arrived - zeroKept, Stars.ToQuality(0));
            else
                Give(craft, 0, zeroKept - arrived);
            for (int stars = Mathf.Max(1, minStars); stars <= Stars.Max; stars++)
                Give(craft, stars, rolled[stars]);
            CraftStars.ReportTrashed(craft, rolled, minStars);
        }

        private static void Give(KitchenCraftContext craft, int stars, int count)
        {
            if (count <= 0)
                return;
            int quality = Stars.ToQuality(stars);
            Player player = craft.Player;
            Inventory inventory = player.GetInventory();
            int fits = Mathf.Min(count, Room(inventory, craft.Shared, quality));
            if (fits > 0)
                inventory.AddItem(craft.Prefab, fits, quality, craft.Variant, player.GetPlayerID(), player.GetPlayerName(), new Vector2i(-1, -1), craft.Cheated);
            if (count > fits)
                CraftDrop.AtFeet(player, NewItem(craft, quality), count - fits);
        }

        /// <summary>How many units of the item at this quality the inventory still takes, the way Inventory.AddItem stacks them.</summary>
        public static int Room(Inventory inventory, ItemDrop.ItemData.SharedData shared, int quality)
        {
            int worldLevel = (byte)Game.m_worldLevel;
            int room = inventory.GetEmptySlots() * Mathf.Max(1, shared.m_maxStackSize);
            foreach (ItemDrop.ItemData item in inventory.m_inventory)
            {
                if (item.m_shared.m_name == shared.m_name && item.m_quality == quality && item.m_worldLevel == worldLevel)
                    room += Mathf.Max(0, item.m_shared.m_maxStackSize - item.m_stack);
            }
            return room;
        }

        /// <summary>A crafted unit as Inventory.AddItem would have made it, for dropping.</summary>
        private static ItemDrop.ItemData NewItem(KitchenCraftContext craft, int quality)
        {
            ItemDrop.ItemData item = craft.Recipe.m_item.m_itemData.Clone();
            item.m_dropPrefab = craft.Recipe.m_item.gameObject;
            item.m_quality = quality;
            item.m_variant = craft.Variant;
            item.m_durability = item.GetMaxDurability();
            item.m_crafterID = craft.Player.GetPlayerID();
            item.m_crafterName = craft.Player.GetPlayerName();
            item.m_worldLevel = (byte)Game.m_worldLevel;
            item.m_cheated = craft.Cheated;
            return item;
        }
    }
}
