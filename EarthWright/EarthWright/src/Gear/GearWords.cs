using EarthWright.Core;

namespace EarthWright.Gear
{
    /// <summary>The Gear module's English words; the tokens are what the shovel's item data and the refusals carry.</summary>
    public static class GearWords
    {
        public static string ShovelName { get; private set; } = "$ew_gear_shovel";
        public static string ShovelDescription { get; private set; } = "$ew_gear_shovel_description";

        /// <summary>Followed by the level number: "Your tool needs level 3".</summary>
        public static string LevelLocked { get; private set; } = "$ew_gear_level_locked";

        public static void Register()
        {
            ShovelName = Language.Add("ew_gear_shovel", "Shovel");
            ShovelDescription = Language.Add("ew_gear_shovel_description",
                "A sturdy spade for moving earth: dig, smooth and level the ground. Upgrade it at the workbench for more durability.");
            LevelLocked = Language.Add("ew_gear_level_locked", "Your tool needs level");
        }
    }
}
