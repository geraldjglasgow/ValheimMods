using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing masts (<see cref="KwEdit"/>): the sail tool's click on a mast opens the sail window itself on that mast
    /// (<see cref="KwSailTool.Edit"/>): its height, every sail (shape, width, height, cloth, pattern) and on the second page
    /// its crow's nest, fighting top and pennant; the preview shows the mast in place as the window has it. Its values for
    /// the merge: "mast", "sails" (how many), each sail's "sail0.shape", "sail0.width", ... "sail0.pattern",
    /// "sail0.patternColour", "sail0.patternStripes", "sail0.patternEmblem" in the mast's order (square sails from the
    /// course up, then the one along the ship), and "nest", "nestAt", "top", "topAt", "pennant". A sail added or taken off
    /// changes how many every mast edited with it carries. Every mast is placed to fit again as the tool places it
    /// (<see cref="KwMastSpec.Fit"/>); a mast that would carry more square sails, or more sails along the ship, than a mast
    /// may is left out. A bowsprit's jibs follow their foremast by themselves (<see cref="KwJibRig"/>).
    /// </summary>
    public sealed class KwMastEdit : KwEditKind
    {
        public KwMastEdit() : base(KwSailTool.Tool, "mast", "masts")
        {
        }

        public override bool OwnWindow => true;

        public override void Open(KwEditSession session)
        {
            KwEditTarget clicked = session.Clicked;
            if (!KwMastSpec.TryParse(clicked.Saved, out KwMastSpec mast))
            {
                session.Done(null);
                return;
            }
            KwSailTool.Edit(clicked.Piece.transform, mast, session.Title, now => session.Changed(Of(now)), done => session.Done(done != null ? Of(done) : null));
        }

        private KwPartValues Of(KwMastSpec mast) => Read(mast.ToString(), KwBiomeTiers.Meadows) ?? new KwPartValues();

        public override KwPartValues? Read(string saved, KwBiomeTier tier)
        {
            if (!KwMastSpec.TryParse(saved, out KwMastSpec mast))
                return null;
            KwPartValues values = new KwPartValues();
            values.Set("mast", mast.Mast);
            values.Set("sails", mast.Sails.Count);
            for (int i = 0; i < mast.Sails.Count; i++)
                PutSail(values, Sail(i), mast.Sails[i]);
            PutTops(values, mast.Tops);
            return values;
        }

        private static string Sail(int index) => "sail" + index + ".";

        private static void PutSail(KwPartValues values, string at, KwSailSpec sail)
        {
            values[at + "shape"] = KwSailSpec.Shapes[sail.Shape];
            values.Set(at + "width", sail.Width);
            values.Set(at + "height", sail.Height);
            values[at + "cloth"] = sail.Cloth;
            PutPattern(values, at, sail.Pattern);
        }

        /// <summary>A pattern into values under a prefix: its kind, and only what that kind has (colour, stripes, emblem).</summary>
        public static void PutPattern(KwPartValues values, string at, KwSailPattern pattern)
        {
            values[at + "pattern"] = KwSailPattern.Kinds[pattern.Kind];
            if (!pattern.IsPlain)
                values[at + "patternColour"] = KwSailDye.Code(pattern.Colour);
            if (pattern.Striped)
                values.Set(at + "patternStripes", pattern.Count);
            if (pattern.HasEmblem)
                values[at + "patternEmblem"] = pattern.Art;
        }

        /// <summary>The pattern values hold under a prefix (plain for none).</summary>
        public static KwSailPattern Pattern(KwPartValues values, string at)
        {
            int kind = Math.Max(0, Array.IndexOf(KwSailPattern.Kinds, values[at + "pattern"]));
            int stripes = values.Whole(at + "patternStripes", KwSailPattern.Stripes);
            return new KwSailPattern(kind, KwSailDye.Index(values[at + "patternColour"]), stripes, values[at + "patternEmblem"]);
        }

        // The platforms' heights only for those the mast carries.
        private static void PutTops(KwPartValues values, KwMastTops tops)
        {
            values["nest"] = tops.Nest ? "1" : "0";
            if (tops.Nest)
                values.Set("nestAt", tops.NestAt);
            values["top"] = KwMastTops.TopShapes[tops.Top];
            if (tops.HasTop)
                values.Set("topAt", tops.TopAt);
            values["pennant"] = tops.Pennant;
        }

        private static KwMastTops Tops(KwPartValues values) => new KwMastTops(values["nest"] == "1", values.Number("nestAt", KwMastTops.MinAt),
            Math.Max(0, Array.IndexOf(KwMastTops.TopShapes, values["top"])), values.Number("topAt", KwMastTops.MinAt), values["pennant"]);

        public override string? Write(KwPartValues values, string saved, out string why)
        {
            float mast = values.Number("mast");
            if (!Within(mast, KwSailSpec.MinMast, KwSailSpec.MaxMast, "a mast would be {0} m tall; masts are {1} to {2} m", out why))
                return null;
            List<KwSailSpec>? sails = Sails(values, out why);
            return sails != null ? KwMastSpec.Fit(mast, sails).With(Tops(values)).ToString() : null;
        }

        // Every sail the values hold, in order; null and why when one does not read or is past a sail's sizes, or the mast
        // would carry more than a mast may.
        private static List<KwSailSpec>? Sails(KwPartValues values, out string why)
        {
            List<KwSailSpec> sails = new List<KwSailSpec>();
            int count = values.Whole("sails"), squares = 0;
            for (int i = 0; i < count; i++)
            {
                KwSailSpec? sail = SailOf(values, Sail(i), out why);
                if (sail == null)
                    return null;
                sails.Add(sail.Value);
                squares += sail.Value.Square ? 1 : 0;
            }
            why = count == 0 ? "a mast keeps one sail at least" : squares > KwMastSpec.MostSquare ? "a mast carries four square sails at most"
                : count - squares > 1 ? "a mast carries one sail along the ship at most" : "";
            return why.Length == 0 ? sails : null;
        }

        private static KwSailSpec? SailOf(KwPartValues values, string at, out string why)
        {
            int shape = Array.IndexOf(KwSailSpec.Shapes, values[at + "shape"]);
            why = "the mast's sails do not read";
            float width = values.Number(at + "width"), height = values.Number(at + "height");
            if (shape < 0 || !Within(width, KwSailSpec.MinSize, KwSailSpec.MaxSize, "a sail would be {0} m wide; sails are {1} to {2} m", out why)
                || !Within(height, KwSailSpec.Least, KwSailSpec.MaxSize, "a sail would be {0} m tall; sails are {1} to {2} m", out why))
                return null;
            return new KwSailSpec(shape, KwSailSpec.MaxMast, width, height, values[at + "cloth"], 0f, 0, Pattern(values, at));
        }
    }
}
