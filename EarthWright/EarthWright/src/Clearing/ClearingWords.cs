using System;
using System.Globalization;
using EarthWright.Core;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The words of the Clearing module, registered with the game's localization. Texts with numbers carry $1, $2
    /// placeholders that <see cref="Format"/> fills after translating.
    /// </summary>
    public static class ClearingWords
    {
        public static string Disabled { get; private set; }
        public static string ModOff { get; private set; }
        public static string Nothing { get; private set; }
        public static string Done { get; private set; }
        public static string Warded { get; private set; }
        public static string NeedAxe { get; private set; }
        public static string NeedPickaxe { get; private set; }
        public static string Interior { get; private set; }
        public static string ResetAround { get; private set; }
        public static string NoAim { get; private set; }
        public static string NeedTool { get; private set; }

        public static void Register()
        {
            Disabled = Language.Add("ew_clear_disabled", "Clearing is switched off on this server");
            ModOff = Language.Add("ew_clear_modoff", "EarthWright is switched off on this server");
            Nothing = Language.Add("ew_clear_nothing", "Nothing to clear here");
            Done = Language.Add("ew_clear_done", "Cleared $1 objects");
            Warded = Language.Add("ew_clear_warded", "$1 objects inside wards or protected places were left alone");
            NeedAxe = Language.Add("ew_clear_needaxe", "$1 objects need an axe (or a stronger one) in your inventory");
            NeedPickaxe = Language.Add("ew_clear_needpickaxe", "$1 objects need a pickaxe (or a stronger one) in your inventory");
            Interior = Language.Add("ew_clear_interior", "Clearing does not work in here");
            ResetAround = Language.Add("ew_clear_resetaround", "Ground reset within $1 m");
            NoAim = Language.Add("ew_clear_noaim", "Aim at the ground first");
            NeedTool = Language.Add("ew_clear_needtool", "Take your hoe in hand to clear with a command");
        }

        /// <summary>Translates a $token and puts the values into its $1, $2 ... placeholders.</summary>
        public static string Format(string token, params object[] values)
        {
            string text = Language.Localize(token);
            for (int i = values.Length - 1; i >= 0; i--)
                text = text.Replace("$" + (i + 1), Convert.ToString(values[i], CultureInfo.InvariantCulture));
            return text;
        }

        /// <summary>A length in metres as shown to players: at most one decimal.</summary>
        public static string Metres(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
