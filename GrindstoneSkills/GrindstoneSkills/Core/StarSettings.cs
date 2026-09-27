using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>Section 2: what each star adds to a dish when it is eaten. Synced.</summary>
    public static class StarSettings
    {
        public const string Section = "2 - Stars";

        /// <summary>Percent more health, stamina and eitr, indexed by stars (index 0 unused, always 0).</summary>
        public static ConfigEntry<float>[] FoodBonus { get; } = new ConfigEntry<float>[Stars.Max + 1];

        /// <summary>Percent longer duration, indexed by stars (index 0 unused, always 0).</summary>
        public static ConfigEntry<float>[] DurationBonus { get; } = new ConfigEntry<float>[Stars.Max + 1];

        private static readonly string[] Names = { "", "One Star", "Two Star", "Three Star" };
        private static readonly float[] FoodDefaults = { 0f, 10f, 20f, 35f };
        private static readonly float[] DurationDefaults = { 0f, 10f, 20f, 30f };

        public static void Initialize(SyncedConfiguration config)
        {
            for (int stars = 1; stars <= Stars.Max; stars++)
            {
                FoodBonus[stars] = config.Bind(Section, $"{Names[stars]} Food Bonus", FoodDefaults[stars],
                    $"Percent more health, stamina and eitr from a {stars}-star dish.", acceptableValues: Settings.UpTo(500f));
                DurationBonus[stars] = config.Bind(Section, $"{Names[stars]} Duration Bonus", DurationDefaults[stars],
                    $"Percent longer a {stars}-star dish lasts. The dish stays at full strength for the extra time, then fades as usual.",
                    acceptableValues: Settings.UpTo(500f));
            }
        }
    }
}
