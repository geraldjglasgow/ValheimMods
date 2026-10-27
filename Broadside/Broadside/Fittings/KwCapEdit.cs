using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Editing post caps (<see cref="KwEdit"/>): the Post cap tool's click on a cap, its own or an old hammer cap
    /// (<see cref="KwCaps.Prefix"/>), or on the top of the post it stands on, opens a window on its style, its length, its
    /// size against its post ("1.00 × post" matches the post's own width and depth, the post measured where the cap stands
    /// now, <see cref="KwTierCap.KeelUnder"/>), its lean (drawn by the long necks only, <see cref="KwCapStyle.Leans"/>) and
    /// which way it looks, out along the keel or inboard;
    /// Shift: every cap on the dock, each given only what changed, each sized against its own post. A cap whose size is left
    /// alone keeps its own base. An old hammer cap reads as a cap of its style matched to its post, and OK builds it again as
    /// the tool's cap (the "old" mark, given at open and dropped by the window, counts as a change), in the same undo step
    /// as any other edit. The tier stays the cap's own. The edit's style, length, lean and facing become the tool's.
    /// </summary>
    public sealed class KwCapEdit : KwEditKind
    {
        private const string Old = "old", Facing = "facing", Inboard = "in";
        private const float MinSize = 0.5f, MaxSize = 2f, LeanStep = 5f;
        private static readonly string[] Facings = { "Out along the keel", "Inboard" };

        public KwCapEdit() : base(KwCapSpec.Tool, "post cap", "post caps")
        {
        }

        public override bool Edits(string prefab) => KwCaps.Is(prefab);

        /// <summary>A click on a keel's post top that has a cap edits that cap.</summary>
        public override Piece? Over(KwClick click) => KwCapTool.CapOn(click);

        /// <summary>A cap's values from its text alone, as on a post its own base's size.</summary>
        public override KwPartValues? Read(string saved, KwBiomeTier tier) =>
            KwCapSpec.TryParse(saved, out KwCapSpec spec) ? Of(spec, spec.Width, spec.Depth, tier, false) : null;

        /// <summary>A cap standing on a post: measured against the post under it; an old hammer cap as its style on that post.</summary>
        public override KwPartValues? Read(Piece piece, string saved, KwBiomeTier tier, bool tiered)
        {
            Piece? keel = KwTierCap.KeelUnder(piece.transform.position);
            KwKeelBody? post = keel != null ? KwKeel.BodyOf(keel) : (KwKeelBody?)null;
            if (KwCapSpec.TryParse(saved, out KwCapSpec spec))
                return Of(spec, post?.PostHalf * 2f ?? spec.Width, post?.PostDepth ?? spec.Depth, tier, false);
            if (!KwCapSpec.Old(Utils.GetPrefabName(piece.gameObject), out string style))
                return null;
            KwCapSpec old = KwCapSpec.On(style, post ?? KwKeel.Standard, 1f);
            return Of(old, old.Width, old.Depth, tier, true);
        }

        /// <summary>
        /// A cap's values: "style", "length", "size" (its base against the post's), "lean", "facing" ("out" or "in"), and
        /// kept out of the window the post's section ("postW", "postD"), its own base and size as read ("width", "depth",
        /// "size0"), its tier, and the old mark.
        /// </summary>
        public static KwPartValues Of(KwCapSpec spec, float postWidth, float postDepth, KwBiomeTier tier, bool old)
        {
            KwPartValues values = new KwPartValues();
            values["style"] = spec.Style;
            values.Set("length", spec.Length);
            float size = Mathf.Round(spec.Width / Mathf.Max(postWidth, 1e-3f) * 1000f) / 1000f;
            values.Set("size", size);
            values.Set("size0", size);
            values.Set("lean", spec.Lean);
            values[Facing] = spec.Inboard ? Inboard : "out";
            values.Set("postW", postWidth);
            values.Set("postD", postDepth);
            values.Set("width", spec.Width);
            values.Set("depth", spec.Depth);
            values[KwBiomeField.Key] = tier.Id;
            if (old)
                values[Old] = "1";
            return values;
        }

        /// <summary>The cap's text: its size changed, its base the post's section times it; else its own base.</summary>
        public override string? Write(KwPartValues values, string saved, out string why)
        {
            float length = values.Number("length"), size = values.Number("size"), lean = values.Number("lean");
            if (!Within(length, KwCapSpec.MinLength, KwCapSpec.MaxLength, "a post cap would be {0}× long; post caps are {1}× to {2}×", out why)
                || !Within(lean, -KwCapSpec.MaxLean, KwCapSpec.MaxLean, "a post cap would lean {0}°; post caps lean {1}° to {2}°", out why))
                return null;
            bool resized = Mathf.Abs(size - values.Number("size0")) > 1e-4f;
            float width = resized ? values.Number("postW") * size : values.Number("width");
            float depth = resized ? values.Number("postD") * size : values.Number("depth");
            if (!Within(width, KwCapSpec.MinBase, KwCapSpec.MaxBase, "a post cap would be {0} m wide; post caps are {1} to {2} m", out why)
                || !Within(depth, KwCapSpec.MinBase, KwCapSpec.MaxBase, "a post cap would be {0} m deep; post caps are {1} to {2} m", out why))
                return null;
            return new KwCapSpec(values["style"], width, depth, length, lean, values[Facing] == Inboard).ToString();
        }

        public override List<KwField> Fields(KwPartValues values, string saved)
        {
            string[] names = System.Array.ConvertAll(KwCaps.Styles, s => s.Name);
            float length = values.Number("length"), size = values.Number("size"), lean = values.Number("lean");
            return new List<KwField>
            {
                KwField.Choice("style", "Style", names, Mathf.Max(0, KwCaps.IndexOf(values["style"]))),
                KwField.Number("length", "Length", length, KwCapSpec.MinLength, KwCapSpec.MaxLength, Step(length, 0.1f), "×"),
                KwField.Number("size", "Size", size, MinSize, MaxSize, Step(size, 0.05f), "× post"),
                KwField.Number("lean", "Lean", lean, -KwCapSpec.MaxLean, KwCapSpec.MaxLean, Step(lean, LeanStep), "°"),
                KwField.Choice(Facing, "Facing", Facings, values[Facing] == Inboard ? 1 : 0),
            };
        }

        /// <summary>The window's values as the part's; the old mark dropped, so an old cap counts as changed.</summary>
        public override KwPartValues Values(KwFormValues form, KwPartValues was)
        {
            KwPartValues values = was.Copy();
            values["style"] = KwCaps.Styles[Mathf.Clamp(form.ChoiceIndex("style"), 0, KwCaps.Styles.Length - 1)].Id;
            values.Set("length", form.Number("length"));
            values.Set("size", form.Number("size"));
            values.Set("lean", form.Number("lean"));
            values[Facing] = form.ChoiceIndex(Facing) == 1 ? Inboard : "out";
            values.Remove(Old);
            return values;
        }

        public override string Note(List<string> changed, int count) =>
            changed.Contains(Old) ? "A cap placed with the hammer is built again as the Post cap tool's, matched to its post."
            : changed.Contains("lean") ? "Lean bends the long necks: the heads, the scroll and the tail curl." : "";

        /// <summary>The window, with an old cap marked first (so OK builds it again); its style, length, lean and facing become the tool's on OK.</summary>
        public override void Open(KwEditSession session)
        {
            KwEditTarget clicked = session.Clicked;
            if (clicked.Values.Has(Old))
                session.Changed(clicked.Values);
            KwForm.Open(session.Title, Fields(clicked.Values, clicked.Saved),
                form => session.Changed(Values(form, session.Now ?? clicked.Values)),
                form => Done(session, form != null ? Values(form, session.Now ?? clicked.Values) : null), true);
        }

        private static void Done(KwEditSession session, KwPartValues? values)
        {
            if (values != null)
                KwCapTool.Keep(new KwCapSpec(values["style"], KwCapForm.PostWidth, KwCapForm.PostDepth, values.Number("length"), values.Number("lean"), values[Facing] == Inboard));
            session.Done(values);
        }

        /// <summary>
        /// An old hammer cap carrying a tool cap's text (an edit's preview of it, <see cref="KwEditPreview"/>) drawn as the
        /// tool's cap it will be built as; every other piece as it is (an old cap in a blueprint saves no text).
        /// </summary>
        public static KwBpPiece AsBuilt(KwBpPiece piece)
        {
            if (!KwCapSpec.Old(piece.Prefab, out _) || !KwCapSpec.TryParse(KwBpData.Text(piece.Data, KwBpPartText.SpanHash) ?? "", out _))
                return piece;
            return new KwBpPiece(KwCapSpec.Made, piece.Position, piece.Rotation, piece.Data);
        }
    }
}
