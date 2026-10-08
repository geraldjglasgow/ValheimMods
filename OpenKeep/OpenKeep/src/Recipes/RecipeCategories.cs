using ItemType = ItemDrop.ItemData.ItemType;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// Which categories the list shows (asked on GitHub 2026-10-07 for PackPanel, built here beside the search; the
    /// user: "copy how AAA crafting does this icon thing", its behaviour, not its icons). None picked shows every
    /// recipe; a click on a category while none is picked shows only it, a click on another adds it, a click on a picked
    /// one takes it away, and taking the last away shows every recipe again. Kept for the game session (a new world
    /// starts with all). A recipe's category is its item's type: food is a consumable that feeds, armour takes
    /// helmets, chests, legs, capes and utilities, weapons whatever the game calls a weapon that is not a bow (torches
    /// too). Meads, trinkets, trophies and other items belong to none and show only while nothing is picked.
    /// </summary>
    public static class RecipeCategories
    {
        public static readonly RecipeCategory[] All =
        {
            RecipeCategory.Ammo, RecipeCategory.Weapons, RecipeCategory.Bows, RecipeCategory.Armour,
            RecipeCategory.Shields, RecipeCategory.Tools, RecipeCategory.Food, RecipeCategory.Materials,
        };

        private static RecipeCategory picked = RecipeCategory.None;

        public static bool Enabled => RecipeListSettings.Categories.Value;

        public static bool Filtering => Enabled && picked != RecipeCategory.None;

        public static bool IsPicked(RecipeCategory category) => (picked & category) != 0;

        public static bool Shows(Recipe recipe) => !Filtering || (picked & Of(recipe)) != 0;

        /// <summary>A click on a category's button: picked or not, then the open list again.</summary>
        public static void Toggle(RecipeCategory category)
        {
            picked ^= category;
            RecipeFavourites.Rebuild();
        }

        /// <summary>At InventoryGui.Awake: a new world shows every recipe.</summary>
        public static void Reset() => picked = RecipeCategory.None;

        public static RecipeCategory Of(Recipe recipe)
        {
            ItemDrop.ItemData item = recipe != null && recipe.m_item != null ? recipe.m_item.m_itemData : null;
            if (item == null || item.m_shared == null)
                return RecipeCategory.None;
            switch (item.m_shared.m_itemType)
            {
                case ItemType.Consumable: return Feeds(item.m_shared) ? RecipeCategory.Food : RecipeCategory.None;
                case ItemType.Material: return RecipeCategory.Materials;
                case ItemType.Bow: return RecipeCategory.Bows;
                case ItemType.Helmet: case ItemType.Chest: case ItemType.Legs:
                case ItemType.Shoulder: case ItemType.Utility: return RecipeCategory.Armour;
                case ItemType.Ammo: case ItemType.AmmoNonEquipable: return RecipeCategory.Ammo;
                case ItemType.Shield: return RecipeCategory.Shields;
                case ItemType.Tool: return RecipeCategory.Tools;
                default: return item.IsWeapon() ? RecipeCategory.Weapons : RecipeCategory.None;
            }
        }

        private static bool Feeds(ItemDrop.ItemData.SharedData shared) =>
            shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f || shared.m_foodRegen > 0f;
    }
}
