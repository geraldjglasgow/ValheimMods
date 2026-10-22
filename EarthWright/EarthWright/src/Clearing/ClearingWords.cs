using System.Globalization;
using Translations;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The words of the Clearing module as $tokens (English in translations/English.yml). Texts with numbers carry $1, $2
    /// placeholders that <see cref="Lang.Format"/> fills after translating.
    /// </summary>
    public static class ClearingWords
    {
        public static string Disabled { get; } = Lang.Token("ew_clear_disabled");
        public static string ModOff { get; } = Lang.Token("ew_clear_modoff");
        public static string Nothing { get; } = Lang.Token("ew_clear_nothing");
        public static string Done { get; } = Lang.Token("ew_clear_done");
        public static string Warded { get; } = Lang.Token("ew_clear_warded");
        public static string NeedAxe { get; } = Lang.Token("ew_clear_needaxe");
        public static string NeedPickaxe { get; } = Lang.Token("ew_clear_needpickaxe");
        public static string Interior { get; } = Lang.Token("ew_clear_interior");
        public static string ResetAround { get; } = Lang.Token("ew_clear_resetaround");
        public static string NoAim { get; } = Lang.Token("ew_clear_noaim");
        public static string NeedTool { get; } = Lang.Token("ew_clear_needtool");

        /// <summary>A length in metres as shown to players: at most one decimal.</summary>
        public static string Metres(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
