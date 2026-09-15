using BepInEx.Configuration;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The .cfg holds only what the rule file does not: per-player display preferences, none of which the server ever
    /// locks, because they change only what one player sees. Everything that changes what the game does - star chances,
    /// star power, mutation chances and power, and the lock switch itself - lives in the YAML rule file instead.
    /// </summary>
    public static class Configuration
    {
        public const string Display = "8 - Display (per player)";
        public const string Diagnostics = "9 - Diagnostics";

        public static ConfigEntry<bool> ShowTraitNames = null!;
        public static ConfigEntry<bool> Diagnose = null!;
        public static ConfigEntry<bool> ColouredStars = null!;
        public static ConfigEntry<float> NameplateDistance = null!;
        public static ConfigEntry<float> EffectDensity = null!;
        public static ConfigEntry<float> SmallStarSize = null!;
        public static ConfigEntry<float> LargeStarSize = null!;

        public static void BindAll(ConfigFile config)
        {
            ShowTraitNames = config.Bind(Display, "Show trait names", true,
                "Prepend each mutation's word to a creature's name. Client side; never locked.");
            ColouredStars = config.Bind(Display, "Coloured stars", true,
                "Draw a creature's stars coloured one-for-one by its mutations. Client side; never locked.");
            NameplateDistance = config.Bind(Display, "Nameplate distance", 0f,
                "Metres within which a creature's mutation name and colours are drawn. 0 uses the game's own nameplate distance.");
            EffectDensity = config.Bind(Display, "Effect density", 1f,
                new ConfigDescription(
                    "Brightness and density of mutation effects (poison clouds, blast tells, cloaking). 1 is full, "
                    + "0 turns them off for a quieter screen or weaker hardware. WARNING: turning effects off does NOT "
                    + "turn the hazard off - the poison and the blast still hurt you exactly the same, you just cannot "
                    + "see them coming. Client side; never locked.",
                    new AcceptableValueRange<float>(0f, 1f)));
            SmallStarSize = config.Bind(Display, "Small star size", 1.3f,
                "Size of a small star glyph as a multiple of the size vanilla draws a star at. 1.3 = 30% larger, so a "
                + "single star is not a speck. Client side; never locked.");
            LargeStarSize = config.Bind(Display, "Large star size", 2.2f,
                "Size of a large star glyph (worth five) as a multiple of the size vanilla draws a star at. Client side; "
                + "never locked.");
            PaletteSettings.Bind(config);
            BindDiagnostics(config);
        }

        // Not a display preference and never locked: a switch that makes the death/split/absorb chain log every step, so
        // a mutation that quietly does nothing can be diagnosed on a live server without a rebuild. Off by default.
        private static void BindDiagnostics(ConfigFile config)
        {
            Diagnose = config.Bind(Diagnostics, "Log diagnostics", false,
                "Print step-by-step diagnostics for mutation death events (splintering, bloated, devouring) to the log. "
                + "Off by default; turn it on to see why a mutation is not firing. Client side; never locked.");
            Log.Diagnostics = Diagnose.Value;
            Diagnose.SettingChanged += (_, __) => Log.Diagnostics = Diagnose.Value;
        }
    }
}
