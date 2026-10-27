using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// Editing ladders (<see cref="KwEdit"/>): the ladder tool builds a ladder in one click to the floor it finds, with no
    /// window (<see cref="KwLadderTool"/>); its click on a ladder opens a window on the ladder's one size, its length down
    /// from the edge it hangs from, within the ladder's limits.
    /// </summary>
    public sealed class KwLadderEdit : KwEditKind
    {
        // The longest a ladder reads as: the tool's reach down from the hatch and the little it hangs past that.
        private const float Longest = KwLadder.MaxLength + 0.5f;

        public KwLadderEdit() : base(KwLadderTool.Tool, "ladder", "ladders")
        {
        }

        public override KwPartValues? Read(string saved, KwBiomeTier tier)
        {
            if (!KwLadder.TryLength(saved, out float length))
                return null;
            KwPartValues values = new KwPartValues();
            values.Set("length", length);
            return values;
        }

        /// <summary>A ladder's text; as long as a ladder reads (the tool hangs one a little past its reach to stand on a floor).</summary>
        public override string? Write(KwPartValues values, string saved, out string why)
        {
            float length = values.Number("length");
            return Within(length, KwLadder.MinLength, Longest, "a ladder would be {0} m long; ladders are {1} to {2} m", out why)
                ? KwLadder.Save(length) : null;
        }

        public override List<KwField> Fields(KwPartValues values, string saved)
        {
            float length = values.Number("length");
            return new List<KwField> { KwField.Number("length", "Length", length, KwLadder.MinLength, Longest, Step(length, 0.1f)) };
        }

        public override KwPartValues Values(KwFormValues form, KwPartValues was)
        {
            KwPartValues values = was.Copy();
            values.Set("length", form.Number("length"));
            return values;
        }
    }
}
