using Translations;

namespace EarthWright.Costs
{
    /// <summary>
    /// The Costs module's words as $tokens (English in translations/English.yml). Texts with $1, $2 ... take numbers or
    /// names through <see cref="Lang.Format"/>.
    /// </summary>
    internal static class CostWords
    {
        public static string Cost { get; } = Lang.Token("ew_costs_cost");
        public static string Stamina { get; } = Lang.Token("ew_costs_stamina");
        public static string Short { get; } = Lang.Token("ew_costs_short");
        public static string NeedStation { get; } = Lang.Token("ew_costs_station");
        public static string NoStamina { get; } = Lang.Token("ew_costs_nostamina");
        public static string Broken { get; } = Lang.Token("ew_costs_broken");
        public static string Wait { get; } = Lang.Token("ew_costs_wait");
        public static string Free { get; } = Lang.Token("ew_costs_free");
        public static string FreeOn { get; } = Lang.Token("ew_costs_free_on");
        public static string FreeOff { get; } = Lang.Token("ew_costs_free_off");
        public static string FreeDenied { get; } = Lang.Token("ew_costs_free_denied");
        public static string Edit { get; } = Lang.Token("ew_costs_edit");
    }
}
