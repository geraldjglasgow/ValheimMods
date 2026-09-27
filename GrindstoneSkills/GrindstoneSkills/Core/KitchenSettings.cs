using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 4: the kitchen perks and the trash filter. Perks grow linearly from nothing at Cooking level 0 to the
    /// configured value at level 100. Synced.
    /// </summary>
    public static class KitchenSettings
    {
        public const string Section = "4 - Kitchen";

        public static ConfigEntry<bool> TrashFilter { get; private set; }
        public static ConfigEntry<float> CookingSpeed { get; private set; }
        public static ConfigEntry<float> ExtraBurnTime { get; private set; }
        public static ConfigEntry<float> FermentingSpeed { get; private set; }
        public static ConfigEntry<float> ExtraFoodChance { get; private set; }
        public static ConfigEntry<int> ExtraFoodAmount { get; private set; }
        public static ConfigEntry<float> IngredientSaveChance { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            TrashFilter = config.Bind(Section, "Trash Filter", true,
                "Lets players set a minimum star count on each kitchen; dishes below it are thrown away. Off: every kitchen keeps everything.");
            CookingSpeed = config.Bind(Section, "Cooking Speed At Level 100", 50f,
                "Percent faster cooking on cooking stations and the oven, from the level of the player who put the food on.",
                acceptableValues: Settings.UpTo(500f));
            ExtraBurnTime = config.Bind(Section, "Extra Burn Time At Level 100", 100f,
                "Extra time before a finished dish burns, in percent of its cook time. The game burns a dish at twice its cook time; 100 moves that to three times.",
                acceptableValues: Settings.UpTo(1000f));
            FermentingSpeed = config.Bind(Section, "Fermenting Speed At Level 100", 30f,
                "Percent faster fermenting, from the level of the player who put the mead base in.", acceptableValues: Settings.UpTo(500f));
            ExtraFoodChance = config.Bind(Section, "Extra Food Chance At Level 100", 50f,
                "Chance in percent of extra food from a kitchen (cooking stations, oven, cauldron, mead cauldron, prep table). " +
                "Replaces the game's own bonus chance at kitchens. Grows from 0 at level 0.", acceptableValues: Settings.UpTo(100f));
            ExtraFoodAmount = config.Bind(Section, "Extra Food Amount", 1,
                "How many extra dishes a bonus gives.", acceptableValues: new AcceptableValueRange<int>(1, 10));
            IngredientSaveChance = config.Bind(Section, "Ingredient Save Chance At Level 100", 10f,
                "Chance in percent that crafting at a kitchen gives back one of the ingredients it used.", acceptableValues: Settings.UpTo(100f));
        }
    }
}
