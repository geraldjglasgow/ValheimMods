using EarthWright.Core;

namespace EarthWright.Extras
{
    /// <summary>The Extras module's English words, as $tokens for messages and the HUD.</summary>
    public static class ExtrasWords
    {
        public static string SeedGridOn { get; private set; } = "$ew_extras_seedgrid_on";
        public static string SeedGridOff { get; private set; } = "$ew_extras_seedgrid_off";
        public static string SeedGridHud { get; private set; } = "$ew_extras_seedgrid_hud";
        public static string Uprooted { get; private set; } = "$ew_extras_uprooted";
        public static string NothingToUproot { get; private set; } = "$ew_extras_nothing_to_uproot";

        public static void Register()
        {
            SeedGridOn = Language.Add("ew_extras_seedgrid_on", "Seed grid on");
            SeedGridOff = Language.Add("ew_extras_seedgrid_off", "Seed grid off");
            SeedGridHud = Language.Add("ew_extras_seedgrid_hud", "Seed grid:");
            Uprooted = Language.Add("ew_extras_uprooted", "Wild plants uprooted:");
            NothingToUproot = Language.Add("ew_extras_nothing_to_uproot", "Nothing wild to uproot here");
        }
    }
}
