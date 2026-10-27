using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a saved ship is to be built back onto the dock, as the table's window takes it before "Build on the dock"
    /// (<see cref="KwBpFiguresPanel"/>): so many percent longer (amidships, <see cref="KwBpStretch"/>; less than 0 shorter),
    /// so many percent wider (the beam, <see cref="KwBpResize.Widen"/>; less than 0 narrower) and a uniform scale in percent
    /// (<see cref="KwBpResize.Scale"/>), in that order.
    /// </summary>
    public readonly struct KwBpShape
    {
        public const float LongerLeast = -50f, LongerMost = 200f, WiderLeast = -50f, WiderMost = 100f, ScaleLeast = 25f, ScaleMost = 300f;

        public KwBpShape(float longer, float wider, float scale)
        {
            Longer = longer;
            Wider = wider;
            Scale = scale;
        }

        public static readonly KwBpShape Same = new KwBpShape(0f, 0f, 100f);

        public float Longer { get; }
        public float Wider { get; }
        public float Scale { get; }

        public bool Lengthens => Mathf.Abs(Longer) >= 0.05f;
        public bool Widens => Mathf.Abs(Wider) >= 0.05f;
        public bool Scales => Mathf.Abs(Scale - 100f) >= 0.05f;
        public bool IsSame => !Lengthens && !Widens && !Scales;

        /// <summary>"20% longer, 10% narrower, at 120%"; empty when the ship is built as it was saved.</summary>
        public string Label
        {
            get
            {
                List<string> words = new List<string>();
                if (Lengthens)
                    words.Add($"{P(Mathf.Abs(Longer))}% {(Longer > 0f ? "longer" : "shorter")}");
                if (Widens)
                    words.Add($"{P(Mathf.Abs(Wider))}% {(Wider > 0f ? "wider" : "narrower")}");
                if (Scales)
                    words.Add($"at {P(Scale)}%");
                return string.Join(", ", words);
            }
        }

        /// <summary>Why the numbers cannot be used, or null: each within its range.</summary>
        public string? Refused()
        {
            if (Longer < LongerLeast || Longer > LongerMost)
                return $"Longer goes from {P(LongerLeast)} to {P(LongerMost)}%";
            if (Wider < WiderLeast || Wider > WiderMost)
                return $"Wider goes from {P(WiderLeast)} to {P(WiderMost)}%";
            if (Scale < ScaleLeast || Scale > ScaleMost)
                return $"Scale goes from {P(ScaleLeast)} to {P(ScaleMost)}%";
            return null;
        }

        private static string P(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A saved ship built to a shape (<see cref="KwBpShape"/>): the blueprint's pieces moved, and their saved part texts
    /// (keel lengths, rib frames' beam and height, span lengths, ends and deck heights, masts and sails, ladders) made
    /// again to the new sizes, so every part is still one the tools could have made. A value that would take a part past
    /// what its tool allows is refused, with the part and its limits named, and nothing is built.
    /// </summary>
    public static class KwBpReshape
    {
        /// <summary>The blueprint built to the shape (itself when the shape changes nothing); null and the reason when refused.</summary>
        public static KwBlueprint? Apply(KwBlueprint blueprint, KwBpShape shape, out string? error)
        {
            error = shape.Refused();
            if (error != null || shape.IsSame)
                return error != null ? null : blueprint;
            KwBlueprint? shaped = blueprint;
            if (shape.Lengthens)
                shaped = KwBpStretch.Apply(shaped, shape.Longer / 100f, out error);
            if (shaped != null && shape.Widens)
                shaped = KwBpResize.Widen(shaped, 1f + shape.Wider / 100f, out error);
            if (shaped != null && shape.Scales)
                shaped = KwBpResize.Scale(shaped, shape.Scale / 100f, out error);
            if (shaped != null && (error = KwBpPartText.Unreadable(blueprint, shaped)) != null)
                return null;
            return shaped;
        }
    }
}
