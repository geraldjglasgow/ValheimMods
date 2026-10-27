using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The cloth choice on the sail and bowsprit windows (<see cref="KwForm"/>): the cloths offered for new work by name
    /// (<see cref="KwSailCloth.Offered"/>: raw hide), and a sail's or jib's own cloth after them when it is another (linen or
    /// scale hide saved before), picked, so an edit keeps it unless the player picks an offered one. A cloth the list does not
    /// know is shown as the plain one. Read back by the option's name, whichever options the window has.
    /// </summary>
    internal static class KwClothField
    {
        public static KwField Field(string key, string label, string cloth)
        {
            string shown = Shown(cloth);
            List<string> cloths = KwSailCloth.Offered;
            if (!cloths.Contains(shown))
                cloths.Add(shown);
            return KwField.Choice(key, label, cloths.ConvertAll(KwSailCloth.Label).ToArray(), Math.Max(0, cloths.IndexOf(shown)));
        }

        /// <summary>The cloth picked on the window, by its name; empty for a name no cloth has.</summary>
        public static string Read(KwFormValues values, string key)
        {
            string label = values.Choice(key);
            return KwSailCloth.Names.Find(cloth => KwSailCloth.Label(cloth) == label) ?? "";
        }

        /// <summary>A cloth as the window shows it: itself when the list knows it, else the plain one.</summary>
        public static string Shown(string cloth) => KwSailCloth.Names.Contains(cloth) ? cloth : KwSailCloth.Plain;
    }
}
