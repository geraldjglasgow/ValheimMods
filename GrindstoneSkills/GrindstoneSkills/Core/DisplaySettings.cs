using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>Which stacks a recipe takes first when a player has the same ingredient with different stars.</summary>
    public enum IngredientOrder
    {
        LowestStarsFirst,
        HighestStarsFirst,
    }

    /// <summary>Section 7: each player's own display and crafting preferences. Not synced.</summary>
    public static class DisplaySettings
    {
        public const string Section = "7 - Display";

        public static ConfigEntry<bool> StarsOnIcons { get; private set; }
        public static ConfigEntry<IngredientOrder> Order { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            StarsOnIcons = config.Bind(Section, "Stars On Icons", true,
                "Shows a starred dish's stars on its icon in inventories, containers and the hotbar.", synced: false);
            Order = config.Bind(Section, "Ingredient Order", IngredientOrder.LowestStarsFirst,
                "Which stacks a recipe uses first: the lowest stars (keeps your best food) or the highest (the best result).", synced: false);
        }
    }
}
