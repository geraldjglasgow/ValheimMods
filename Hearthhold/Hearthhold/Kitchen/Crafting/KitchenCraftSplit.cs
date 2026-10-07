using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// Gives every unit crafted at a kitchen crafting station its own stars. DoCrafting added the whole craft (multi-craft
    /// and bonus food included) at quality 1 in one Inventory.AddItem, possibly into a plain stack the crafter already
    /// had, so the units that arrived are the rise in the plain count. GrindstoneSkills' room check (CanAddItem counts
    /// only stacks of the same stars) makes the game refuse a craft whose plain units do not fit, so none are lost there.
    /// Each unit rolls at the crafter's Cooking level plus <see cref="StarOdds.IngredientBonus"/> of the used-up star
    /// items' average stars, plus <see cref="StarOdds.FishBonus"/> when a fish was cleaned; the floor is the Gourmet one,
    /// or the Angler one (from the Fishing level) when it is higher. The units that rolled stars are taken back out with
    /// Inventory.RemoveItem(name, n, 1), which takes plain units only, and added again at their quality through the
    /// game's own AddItem overload with the crafter, variant and cheated flag the game used. What no longer fits
    /// (starred stacks need their own slots) is dropped at the crafter's feet as a networked item keeping its quality.
    /// </summary>
    public static class KitchenCraftSplit
    {
        public static void Apply(KitchenCraftContext craft)
        {
            Inventory inventory = craft.Player.GetInventory();
            int arrived = KitchenCraftContext.PlainCount(inventory, craft.Shared.m_name) - craft.PlainBefore;
            if (arrived <= 0)
                return;
            int[] rolled = Roll(craft, arrived);
            int starred = arrived - rolled[0];
            if (starred <= 0)
                return;
            inventory.RemoveItem(craft.Shared.m_name, starred, Stars.ToQuality(0));
            for (int stars = 1; stars <= Stars.Max; stars++)
                Give(craft, stars, rolled[stars]);
        }

        /// <summary>How many of <paramref name="units"/> rolled each star count (index = stars).</summary>
        private static int[] Roll(KitchenCraftContext craft, int units)
        {
            int fish = KitchenIngredientRecord.FishLevel;
            float bonus = StarOdds.IngredientBonus(KitchenIngredientRecord.AverageStars) + (fish > 0 ? StarOdds.FishBonus(fish) : 0f);
            int floor = Mathf.Max(Professions.Floor(StarSource.Dish, craft.CookLevel), StarOdds.IngredientFloor(KitchenIngredientRecord.AverageStars));
            if (fish > 0)
                floor = Mathf.Max(floor, Professions.Floor(StarSource.Fillet, craft.FishingLevel));
            int[] rolled = new int[Stars.Max + 1];
            for (int i = 0; i < units; i++)
                rolled[StarOdds.Roll(craft.CookLevel, bonus, floor)]++;
            return rolled;
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
                AtFeet(player, NewItem(craft, quality), count - fits);
        }

        /// <summary>How many units of the item at this quality the inventory still takes, the way Inventory.AddItem stacks them.</summary>
        private static int Room(Inventory inventory, ItemDrop.ItemData.SharedData shared, int quality)
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
            ItemDrop.ItemData item = craft.Product.m_itemData.Clone();
            item.m_dropPrefab = craft.Product.gameObject;
            item.m_quality = quality;
            item.m_variant = craft.Variant;
            item.m_durability = item.GetMaxDurability();
            item.m_crafterID = craft.Player.GetPlayerID();
            item.m_crafterName = craft.Player.GetPlayerName();
            item.m_worldLevel = (byte)Game.m_worldLevel;
            item.m_cheated = craft.Cheated;
            return item;
        }

        /// <summary>
        /// Drops in front of the player the way Humanoid.DropItem does: ItemDrop.DropItem spawns a networked copy that
        /// keeps the quality, in stacks no larger than the stack size.
        /// </summary>
        private static void AtFeet(Player player, ItemDrop.ItemData item, int amount)
        {
            Transform at = player.transform;
            Vector3 position = at.position + at.forward + at.up;
            int perStack = Mathf.Max(1, item.m_shared.m_maxStackSize);
            for (int left = amount; left > 0; left -= perStack)
                ItemDrop.DropItem(item, Mathf.Min(left, perStack), position, at.rotation);
        }
    }
}
