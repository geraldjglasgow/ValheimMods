using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A masthead pennant's colours (<see cref="KwPennant"/>): none, the mast's sails' own cloth (its first sail's), or a
    /// colour dyed into the linen sail's cloth (the game's own sail material, tinted, made once each and kept). Saved by
    /// its code in the mast's text (<see cref="KwMastTops.Pennant"/>); a code this list does not know shows as the sails'
    /// cloth. New work is offered none and the sails' cloth only (<see cref="Offered"/>): a dyed pennant is linen, a Plains
    /// cloth (<see cref="KwSailCloth.Offered"/>), shown only on a mast saved with one and kept unless changed.
    /// </summary>
    public static class KwPennantLook
    {
        public const string Cloth = "cloth";

        // How many of the colours, from the first, new work is offered: none and the sails' cloth.
        private const int Offered = 2;

        private static readonly (string Code, string Label, Color Tint)[] Colours =
        {
            ("", "None", Color.white),
            (Cloth, "As the sails", Color.white),
            ("red", "Red", new Color(0.62f, 0.13f, 0.1f)),
            ("blue", "Blue", new Color(0.16f, 0.26f, 0.56f)),
            ("green", "Green", new Color(0.2f, 0.42f, 0.2f)),
            ("yellow", "Yellow", new Color(0.82f, 0.66f, 0.2f)),
            ("black", "Black", new Color(0.12f, 0.11f, 0.1f)),
            ("white", "White", new Color(1f, 0.98f, 0.94f)),
        };

        private static readonly Dictionary<string, Material> Dyed = new Dictionary<string, Material>();

        /// <summary>The window's names for the colours offered (the first is none), and a pennant's own colour after them when it is another.</summary>
        public static string[] Choices(string code)
        {
            int own = Index(code);
            List<string> labels = new List<string>();
            for (int i = 0; i < Colours.Length; i++)
            {
                if (i < Offered || i == own)
                    labels.Add(Colours[i].Label);
            }
            return labels.ToArray();
        }

        /// <summary>A code's name on the window.</summary>
        public static string Label(string code) => Colours[Index(code)].Label;

        /// <summary>A code's place in the list: none for empty, the sails' cloth for one the list does not know.</summary>
        public static int Index(string code)
        {
            if (string.IsNullOrEmpty(code))
                return 0;
            for (int i = 1; i < Colours.Length; i++)
            {
                if (Colours[i].Code == code)
                    return i;
            }
            return 1;
        }

        /// <summary>The code of a colour picked on the window, by its name; the sails' cloth for a name no colour has.</summary>
        public static string CodeOf(string label)
        {
            foreach (var colour in Colours)
            {
                if (colour.Label == label)
                    return colour.Code;
            }
            return Cloth;
        }

        /// <summary>The material a pennant of this code wears on a mast: its sails' cloth, or the linen dyed.</summary>
        public static Material? Of(string code, KwMastSpec mast)
        {
            int index = Index(code);
            if (index <= 1)
                return KwSailCloth.Of(mast.Sails.Count > 0 ? mast.Sails[0].Cloth : KwSailCloth.Plain);
            string key = Colours[index].Code;
            if (Dyed.TryGetValue(key, out Material made) && made != null)
                return made;
            Material? linen = KwSailCloth.Of(KwSailCloth.Plain);
            if (linen == null)
                return null;
            made = new Material(linen) { name = "broadside_pennant_" + key, color = Colours[index].Tint };
            Dyed[key] = made;
            return made;
        }
    }
}
