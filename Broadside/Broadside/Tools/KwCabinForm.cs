using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Cabin tool's window rows, the same for a new cabin and an edit (<see cref="KwCabinTool"/>, <see cref="KwCabinEdit"/>):
    /// its height over the deck (about 2 m to stand inside), how far its front, back, left and right are drawn in (0 or
    /// more: the user's "shorten from either back, front, left or right individually", leaving a walkway on the deck
    /// outside), and its material. No doorway: cabins are built with solid walls. And the note under them.
    /// </summary>
    public static class KwCabinForm
    {
        public const string Height = "height", Front = "front", Back = "back", Left = "left", Right = "right";

        /// <summary>The rows, filled from sizes; the insets up to the cabin's length along (less its shortest) and its widest across.</summary>
        public static List<KwField> Fields(KwCabinSizes s, float length, float wide, KwBiomeTier tier)
        {
            float along = Mathf.Max(0f, length - KwCabinOutline.MinLength), across = Mathf.Max(0f, wide - KwCabinOutline.MinWide);
            return new List<KwField>
            {
                KwField.Number(Height, "Height over the deck", Mathf.Clamp(s.Height, KwCabinSpec.MinHeight, KwCabinSpec.MaxHeight), KwCabinSpec.MinHeight, KwCabinSpec.MaxHeight, Step(s.Height, 0.1f)),
                KwField.Number(Front, "Front inset", Mathf.Min(s.Front, along), 0f, along, Step(s.Front, 0.05f)),
                KwField.Number(Back, "Back inset", Mathf.Min(s.Back, along), 0f, along, Step(s.Back, 0.05f)),
                KwField.Number(Left, "Left inset", Mathf.Min(s.Left, across), 0f, across, Step(s.Left, 0.05f)),
                KwField.Number(Right, "Right inset", Mathf.Min(s.Right, across), 0f, across, Step(s.Right, 0.05f)),
                KwBiomeField.Field(KwPart.Hull, tier),
            };
        }

        // A number's step: the window's own, or a centimetre for a value not on it (a cabin built back scaled).
        private static float Step(float value, float step) => Mathf.Abs(value / step - Mathf.Round(value / step)) < 0.001f ? step : 0.01f;

        /// <summary>The window's sizes.</summary>
        public static KwCabinSizes Read(KwFormValues values) =>
            new KwCabinSizes(values.Number(Height), values.Number(Back), values.Number(Front), values.Number(Left), values.Number(Right));

        /// <summary>The widest a cabin's deck is across, at the deck, along its hull's sections.</summary>
        public static float Wide(List<Station> stations, float floor)
        {
            float most = 0f;
            foreach (Station s in stations)
                most = Mathf.Max(most, 2f * KwShapes.InsideWidth(KwCabinOutline.Extend(s, floor + 1f).Half, floor));
            return most;
        }

        /// <summary>
        /// What building the cabin does: what was changed to fit it to the hull, first and alone with its front's way when
        /// anything was (<see cref="KwCabinSpec.Fitted"/>: the note is short); else which way its front is, the hull's sides
        /// raised, the railing taken away along it (only from a side at the hull: a walkway keeps its railing), and that its
        /// walls are solid.
        /// </summary>
        public static string Note(KwCabinSpec spec, string front, int rails)
        {
            if (spec.Fitted.Count > 0)
                return string.Join(" ", spec.Fitted) + " " + front + ".";
            List<string> parts = new List<string> { front };
            float raise = KwCabinOutline.Raise(spec);
            if (raise > 0.05f)
                parts.Add($"the hull's sides are raised up to {raise:0.0} m with planking");
            if (rails > 0)
                parts.Add($"the railing along it goes from {(spec.Left.AtHull != spec.Right.AtHull ? "the side at the hull (the walkway keeps its own)" : "both sides")}"
                    + $", {rails} part{(rails == 1 ? "" : "s")} changed");
            parts.Add("its walls are solid: cut doorways with the Doorway tool");
            string text = string.Join("; ", parts);
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
