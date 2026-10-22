using Translations;

namespace EarthWright.Gear
{
    /// <summary>The Gear module's words (English in translations/English.yml).</summary>
    public static class GearWords
    {
        /// <summary>"Your tool needs level 3": the refusal of a feature the held tool's level does not unlock yet.</summary>
        public static string LevelLocked(int level) => Lang.Format("ew_gear_level_locked", level);
    }
}
