using EarthWright.Core;

namespace EarthWright.Gear
{
    /// <summary>The Gear module's English words; the tokens are what the refusals carry.</summary>
    public static class GearWords
    {
        /// <summary>Followed by the level number: "Your tool needs level 3".</summary>
        public static string LevelLocked { get; private set; } = "$ew_gear_level_locked";

        public static void Register()
        {
            LevelLocked = Language.Add("ew_gear_level_locked", "Your tool needs level");
        }
    }
}
