using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The sail window's second page (<see cref="KwSailForm"/>): what the mast carries besides its sails
    /// (<see cref="KwMastTops"/>). A crow's nest, none or round and railed, and the height of its floor; a fighting top, none,
    /// square or round, and the height of its floor; and a pennant at the masthead, none or in the sails' cloth (a dyed
    /// colour only on a mast saved with one, <see cref="KwPennantLook"/>). A platform not put up shows the height it would be offered at, clear of the sails
    /// (<see cref="KwMastTopFit"/>), so putting it up takes that height unless the player types another.
    /// </summary>
    internal static class KwSailTopsForm
    {
        private static readonly string[] Nests = { "None", "Round, railed" };
        private static readonly string[] TopLabels = { "None", "Square", "Round" };

        public static List<KwField> Fields(KwSailDraft draft)
        {
            KwMastSpec mast = draft.Fitted;
            KwMastTops tops = mast.Tops;
            return new List<KwField>
            {
                KwField.Choice("nest", "Crow's nest", Nests, tops.Nest ? 1 : 0),
                KwField.Number("nestAt", "Nest height", tops.Nest ? tops.NestAt : KwMastTopFit.Nest(mast), KwMastTops.MinAt, Most(mast, 0.15f), 0.25f),
                KwField.Choice("top", "Fighting top", TopLabels, tops.Top),
                KwField.Number("topAt", "Top height", tops.HasTop ? tops.TopAt : KwMastTopFit.Top(mast), KwMastTops.MinAt, Most(mast, 0.6f), 0.25f),
                Pennant(tops.Pennant),
            };
        }

        /// <summary>The page's values into the draft's platforms and pennant.</summary>
        public static void Read(KwFormValues values, KwSailDraft draft)
        {
            bool nest = values.ChoiceIndex("nest") == 1;
            int top = Mathf.Clamp(values.ChoiceIndex("top"), 0, KwMastTops.TopShapes.Length - 1);
            string pennant = KwPennantLook.CodeOf(values.Choice("pennant"));
            draft.Tops = new KwMastTops(nest, values.Number("nestAt"), top, values.Number("topAt"), pennant);
        }

        // The pennant row: the colours offered, and the mast's own after them when it is another (a dyed one saved before).
        private static KwField Pennant(string code)
        {
            string[] labels = KwPennantLook.Choices(code);
            return KwField.Choice("pennant", "Pennant", labels, System.Math.Max(0, System.Array.IndexOf(labels, KwPennantLook.Label(code))));
        }

        // The highest a floor may be: under the masthead by that much.
        private static float Most(KwMastSpec mast, float under) => Mathf.Max(KwMastTops.MinAt, mast.Mast - under);
    }
}
