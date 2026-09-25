using BepInEx.Configuration;
using SyncedConfig;

namespace FeastMaster
{
    /// <summary>
    /// The numbered global sections 1 to 9. This file holds the regeneration sections (health, stamina, eitr);
    /// <c>SettingsMore.cs</c> holds costs, base values, skills, world rates and display; <c>SettingsKitchen.cs</c> the kitchen. Every gameplay entry is
    /// synced from the server; the display entries are per client. Values are read at use time so edits, reloads
    /// and server pushes apply at once. BepInEx sorts sections by name, which the numbering relies on.
    /// </summary>
    public static partial class Settings
    {
        public const string HealthRegenSection = "1. Health Regeneration";
        public const string StaminaRegenSection = "2. Stamina Regeneration";
        public const string EitrRegenSection = "3. Eitr Regeneration";
        public const string StaminaCostsSection = "4. Stamina Costs";
        public const string BaseValuesSection = "5. Base Values";
        public const string SkillsSection = "6. Skills";
        public const string WorldRatesSection = "7. World Rates";
        public const string DisplaySection = "8. Display";
        public const string KitchenSection = "9. Kitchen";

        /// <summary>The game's regeneration factor while holding block, replaced by the configured factors.</summary>
        public const float GameBlockingRegenFactor = 0.8f;

        // 1. Health Regeneration
        public static ConfigEntry<bool> ContinuousFoodHealing { get; private set; }

        // 2. Stamina Regeneration
        public static ConfigEntry<float> VigorPerStaminaPoint { get; private set; }
        public static ConfigEntry<float> VigorMultiplier { get; private set; }
        public static ConfigEntry<float> RegenCurveStrength { get; private set; }
        public static ConfigEntry<float> RegenCurvePivot { get; private set; }
        public static ConfigEntry<float> RegenPerExtraStaminaPoint { get; private set; }
        public static ConfigEntry<bool> CountFoodStaminaOnly { get; private set; }
        public static ConfigEntry<float> SneakSkillRegenBonus { get; private set; }
        public static ConfigEntry<float> EncumberedRegenFraction { get; private set; }
        public static ConfigEntry<float> SwimmingRegenFraction { get; private set; }
        public static ConfigEntry<float> SwimmingRegenDelay { get; private set; }
        public static ConfigEntry<float> StaminaRegenMultiplier { get; private set; }
        public static ConfigEntry<float> LowStaminaRegenBonus { get; private set; }
        public static ConfigEntry<float> StaminaRegenDelay { get; private set; }
        public static ConfigEntry<float> BlockingRegenFactor { get; private set; }

        // 3. Eitr Regeneration
        public static ConfigEntry<float> EitrVigorPerEitrPoint { get; private set; }
        public static ConfigEntry<float> EitrVigorMultiplier { get; private set; }
        public static ConfigEntry<float> EitrRegenMultiplier { get; private set; }
        public static ConfigEntry<float> EitrRegenDelay { get; private set; }
        public static ConfigEntry<float> EitrRegenCurveStrength { get; private set; }
        public static ConfigEntry<float> EitrRegenCurvePivot { get; private set; }
        public static ConfigEntry<float> BlockingEitrRegenFactor { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            BindHealthRegen(config);
            BindVigorAndCurve(config);
            BindRegenRules(config);
            BindRegenBasics(config);
            BindEitrRegen(config);
            BindStaminaCosts(config);
            BindOutOfCombat(config);
            BindBaseValues(config);
            BindSkills(config);
            BindWorldRates(config);
            BindDisplay(config);
            BindKitchen(config);
            BindEating(config);
        }

        /// <summary>Called Steady Regeneration up to 4.0.0; the old line's value is carried over.</summary>
        private static void BindHealthRegen(SyncedConfiguration config)
        {
            const string key = "Continuous Food Healing";
            string migrated = ConfigMigration.TakeRenamed(config.Config, HealthRegenSection, "Steady Regeneration", key);
            ContinuousFoodHealing = config.Bind(HealthRegenSection, key, false,
                "Off: food heals its Health Regen once every 10 seconds. On: the same amount is spread over the 10 seconds, applied a little every frame, without floating heal numbers.");
            ConfigMigration.Apply(ContinuousFoodHealing, migrated);
        }

        private static void BindVigorAndCurve(SyncedConfiguration config)
        {
            VigorPerStaminaPoint = config.Bind(StaminaRegenSection, "Vigor Per Stamina Point", 0f,
                "Percent of stamina regeneration every food grants per point of its configured Stamina while it is active (its Vigor). 0 keeps vanilla; 0.1 turns a 60-stamina food into +6% regeneration, three such foods into +18%. Each food's own Vigor entry adds on top. Stacks with Regen Per Extra Stamina Point, which also counts food stamina; prefer one of the two.");
            VigorMultiplier = config.Bind(StaminaRegenSection, "Vigor Multiplier", 1f,
                "Scales the Vigor of every food. Stacks with Regen Per Extra Stamina Point.");
            RegenCurveStrength = config.Bind(StaminaRegenSection, "Regen Curve Strength", 1f,
                "1 = off. Above 1 stamina regenerates faster when the bar is low and slower when it is high: at an empty bar regeneration is multiplied by this value, at a full bar divided by it, 1x at the pivot. Below 1 does the opposite. Stacks with Low Stamina Regen Bonus and the game's own low-bar bonus, all boosting an empty bar.");
            RegenCurvePivot = config.Bind(StaminaRegenSection, "Regen Curve Pivot", 0.5f,
                "Bar fraction (0..1) where the regen curve is exactly 1x.", acceptableValues: new AcceptableValueRange<float>(0f, 1f));
        }

        private static void BindRegenRules(SyncedConfiguration config)
        {
            BindExtraStaminaRegen(config);
            SneakSkillRegenBonus = config.Bind(StaminaRegenSection, "Sneak Skill Regen Bonus", 0f,
                "Percent of stamina regeneration added while crouching and standing still, at Sneak skill 100. Scales with the skill.");
            EncumberedRegenFraction = config.Bind(StaminaRegenSection, "Encumbered Regen Fraction", 0f,
                "0 = vanilla, no regeneration while encumbered. Above 0 the player regenerates at this fraction of the normal rate while encumbered (walking still drains).", acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            SwimmingRegenFraction = config.Bind(StaminaRegenSection, "Swimming Regen Fraction", 0f,
                "0 = vanilla, no regeneration while swimming. Above 0 the player regenerates at this fraction of the normal rate while in the water, after the swimming regen delay.", acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            SwimmingRegenDelay = config.Bind(StaminaRegenSection, "Swimming Regen Delay", 3f,
                "Seconds after the last swimming stroke before swimming regeneration starts (treading water or floating still).");
        }

        /// <summary>
        /// Up to 4.0.0 the percent was per 10 points of extra stamina under the key "Regen Per 10 Extra Stamina" and the
        /// switch was "Extra Stamina From Food Only"; old values are carried over, the percent divided by 10.
        /// </summary>
        private static void BindExtraStaminaRegen(SyncedConfiguration config)
        {
            const string pointKey = "Regen Per Extra Stamina Point";
            string perTen = ConfigMigration.TakeRenamed(config.Config, StaminaRegenSection, "Regen Per 10 Extra Stamina", pointKey);
            RegenPerExtraStaminaPoint = config.Bind(StaminaRegenSection, pointKey, 0f,
                "Percent of stamina regeneration added per point of stamina above Base Stamina (and above the skill stamina of section 5). 0 keeps vanilla; 0.2 with 150 extra stamina gives +30%. Stacks with Vigor, which also counts food stamina; prefer one of the two. Base Stamina in section 5 moves the reference point.");
            ConfigMigration.ApplyDivided(RegenPerExtraStaminaPoint, perTen, 10f);

            const string foodKey = "Count Food Stamina Only";
            string foodOnly = ConfigMigration.TakeRenamed(config.Config, StaminaRegenSection, "Extra Stamina From Food Only", foodKey);
            CountFoodStaminaOnly = config.Bind(StaminaRegenSection, foodKey, false,
                "Off: extra stamina is the whole maximum above the base, from food, meads, gear and other mods. On: only the current stamina of the active foods counts.");
            ConfigMigration.Apply(CountFoodStaminaOnly, foodOnly);
        }

        private static void BindRegenBasics(SyncedConfiguration config)
        {
            StaminaRegenMultiplier = config.Bind(StaminaRegenSection, "Stamina Regen Multiplier", 1f,
                "Scales the base stamina regeneration. The curve, Vigor and extra stamina rules apply on top. Multiplies with the world Stamina Regen Rate in section 7 and with Low Stamina Regen Bonus.");
            LowStaminaRegenBonus = config.Bind(StaminaRegenSection, "Low Stamina Regen Bonus", 1f,
                "Scales the game's own bonus for a low bar (regeneration rises as the bar empties). 0 removes it. Stacks with Regen Curve Strength and Stamina Regen Multiplier.");
            StaminaRegenDelay = config.Bind(StaminaRegenSection, "Stamina Regen Delay", 1f,
                "Seconds after any stamina use before regeneration resumes. The game uses 1.");
            BlockingRegenFactor = config.Bind(StaminaRegenSection, "Blocking Regen Factor", GameBlockingRegenFactor,
                "Stamina regeneration factor while holding block. The game uses 0.8; 1 means no slowdown.");
        }

        private static void BindEitrRegen(SyncedConfiguration config)
        {
            EitrVigorPerEitrPoint = config.Bind(EitrRegenSection, "Eitr Vigor Per Eitr Point", 0f,
                "Percent of eitr regeneration every food grants per point of its configured Eitr while it is active (its Eitr Vigor). 0 keeps vanilla. Each food's own EitrVigor entry adds on top.");
            EitrVigorMultiplier = config.Bind(EitrRegenSection, "Eitr Vigor Multiplier", 1f,
                "Scales the Eitr Vigor of every food.");
            EitrRegenMultiplier = config.Bind(EitrRegenSection, "Eitr Regen Multiplier", 1f,
                "Scales the base eitr regeneration. Eitr Vigor and the eitr curve apply on top.");
            EitrRegenDelay = config.Bind(EitrRegenSection, "Eitr Regen Delay", 1f,
                "Seconds after any eitr use before regeneration resumes. The game uses 1.");
            EitrRegenCurveStrength = config.Bind(EitrRegenSection, "Eitr Regen Curve Strength", 1f,
                "1 = off. Above 1 eitr regenerates faster when the bar is low and slower when it is high, like the stamina curve. Below 1 does the opposite. Stacks with the game's own low-bar eitr bonus.");
            EitrRegenCurvePivot = config.Bind(EitrRegenSection, "Eitr Regen Curve Pivot", 0.5f,
                "Eitr bar fraction (0..1) where the eitr regen curve is exactly 1x.", acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            BlockingEitrRegenFactor = config.Bind(EitrRegenSection, "Blocking Eitr Regen Factor", GameBlockingRegenFactor,
                "Eitr regeneration factor while holding block. The game uses 0.8; 1 means no slowdown.");
        }
    }
}
