using System.Globalization;
using EarthWright.Terrain;
using Translations;

namespace EarthWright.Paths
{
    /// <summary>
    /// Helpers for the module's words ("ew_paths_*" in translations/English.yml): the names of ramp profiles and paints,
    /// and numbers as the HUD writes them, with a dot as decimal sign, for the $1, $2 ... of <see cref="Lang.Format"/>.
    /// </summary>
    public static class PathWords
    {
        private static readonly string[] profiles =
        {
            "ew_paths_profile_straight", "ew_paths_profile_softjoins", "ew_paths_profile_softends", "ew_paths_profile_scurve",
        };

        private static readonly string[] paints =
        {
            "ew_paths_paint_none", "ew_paths_paint_dirt", "ew_paths_paint_cultivated", "ew_paths_paint_paved",
            "ew_paths_paint_grass", "ew_paths_paint_clearvegetation", "ew_paths_paint_vegetation", "ew_paths_paint_deepsnow",
            "ew_paths_paint_original",
        };

        public static string ProfileName(RampProfile profile) => Pick(profiles, (int)profile);

        public static string PaintName(PaintOp paint) => Pick(paints, (int)paint);

        /// <summary>A number in a .NET format ("0.0", "+0.0;-0.0;0.0"), with a dot as decimal sign.</summary>
        public static string Number(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        private static string Pick(string[] keys, int index) => index >= 0 && index < keys.Length ? Lang.Get(keys[index]) : "?";
    }
}
