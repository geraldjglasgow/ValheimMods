using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing bowsprits (<see cref="KwEdit"/>): the bowsprit tool's click on a bowsprit opens the bowsprit window itself on
    /// it (<see cref="KwBowspritTool.Edit"/>): its length, rise, jibs, their cloth and pattern, the jibs cut to the stay
    /// from the foremast found there as the tool cuts them; the preview shows it in place. Its values for the merge:
    /// "length", "rise", "jibs", "cloth" and the pattern's ("pattern", "patternColour", ...,
    /// <see cref="KwMastEdit.PutPattern"/>).
    /// </summary>
    public sealed class KwBowspritEdit : KwEditKind
    {
        public KwBowspritEdit() : base(KwBowsprit.Tool, "bowsprit", "bowsprits")
        {
        }

        public override bool OwnWindow => true;

        public override void Open(KwEditSession session)
        {
            KwEditTarget clicked = session.Clicked;
            if (!KwBowsprit.TryParse(clicked.Saved, out KwBowsprit spec))
            {
                session.Done(null);
                return;
            }
            KwBowspritTool.Edit(clicked.Piece.transform, spec, session.Title, now => session.Changed(Of(now)), done => session.Done(done.HasValue ? Of(done.Value) : null));
        }

        private KwPartValues Of(KwBowsprit spec) => Read(spec.ToString(), KwBiomeTiers.Meadows) ?? new KwPartValues();

        public override KwPartValues? Read(string saved, KwBiomeTier tier)
        {
            if (!KwBowsprit.TryParse(saved, out KwBowsprit spec))
                return null;
            KwPartValues values = new KwPartValues();
            values.Set("length", spec.Length);
            values.Set("rise", spec.Rise);
            values.Set("jibs", spec.Jibs);
            values["cloth"] = spec.Cloth;
            KwMastEdit.PutPattern(values, "", spec.Pattern);
            return values;
        }

        public override string? Write(KwPartValues values, string saved, out string why)
        {
            float length = values.Number("length"), rise = values.Number("rise");
            int jibs = values.Whole("jibs");
            if (!Within(length, KwBowsprit.MinLength, KwBowsprit.MaxLength, "a bowsprit would be {0} m long; bowsprits are {1} to {2} m", out why)
                || !Within(rise, 0f, KwBowsprit.MaxRise, "a bowsprit would rise {0}°; at most {2}°", out why)
                || !Within(jibs, 0f, KwBowsprit.MostJibs, "a bowsprit would set {0} jibs; at most {2}", out why))
                return null;
            return new KwBowsprit(length, rise, Mathf.Clamp(jibs, 0, KwBowsprit.MostJibs), values["cloth"], KwMastEdit.Pattern(values, "")).ToString();
        }
    }
}
