using Translations;

namespace EarthWright.Extras
{
    /// <summary>The Extras module's words as $tokens for messages and the HUD (English in translations/English.yml).</summary>
    public static class ExtrasWords
    {
        public static string SeedGridOn { get; } = Lang.Token("ew_extras_seedgrid_on");
        public static string SeedGridOff { get; } = Lang.Token("ew_extras_seedgrid_off");
        public static string SeedGridHud { get; } = Lang.Token("ew_extras_seedgrid_hud");
        public static string Uprooted { get; } = Lang.Token("ew_extras_uprooted");
        public static string NothingToUproot { get; } = Lang.Token("ew_extras_nothing_to_uproot");
    }
}
