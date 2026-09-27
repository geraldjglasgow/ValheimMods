using EarthWright.Core;

namespace EarthWright.Costs
{
    /// <summary>
    /// The Costs module's words. Texts with $1, $2 ... take numbers or names through <see cref="Format"/>, the game's
    /// own way of inserting words into a translated text.
    /// </summary>
    internal static class CostWords
    {
        public static string Cost { get; private set; }
        public static string Stamina { get; private set; }
        public static string Short { get; private set; }
        public static string NeedStation { get; private set; }
        public static string NoStamina { get; private set; }
        public static string Broken { get; private set; }
        public static string Wait { get; private set; }
        public static string Free { get; private set; }
        public static string FreeOn { get; private set; }
        public static string FreeOff { get; private set; }
        public static string FreeDenied { get; private set; }
        public static string Edit { get; private set; }

        public static void Register()
        {
            Cost = Language.Add("ew_costs_cost", "Cost");
            Stamina = Language.Add("ew_costs_stamina", "Stamina");
            Short = Language.Add("ew_costs_short", "Not enough $1 ($2 of $3)");
            NeedStation = Language.Add("ew_costs_station", "Needs $1 nearby");
            NoStamina = Language.Add("ew_costs_nostamina", "Not enough stamina");
            Broken = Language.Add("ew_costs_broken", "Your tool is broken");
            Wait = Language.Add("ew_costs_wait", "Wait $1 s");
            Free = Language.Add("ew_costs_free", "Free build");
            FreeOn = Language.Add("ew_costs_free_on", "Free build on: terrain work costs nothing");
            FreeOff = Language.Add("ew_costs_free_off", "Free build off");
            FreeDenied = Language.Add("ew_costs_free_denied", "Free build is not allowed on this server");
            Edit = Language.Add("ew_costs_edit", "Edit EarthWright.Costs.yml");
        }

        /// <summary>The localized text with $1, $2 ... replaced by the words.</summary>
        public static string Format(string token, params string[] words) => Language.Format(token, words);
    }
}
