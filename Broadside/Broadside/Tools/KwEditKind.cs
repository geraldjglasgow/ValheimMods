using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One kind of part a tool edits (<see cref="KwEdit"/>): its tool and generated part; which of that tool's clicks on such
    /// a part the tool still takes its own way (<see cref="Keeps"/>); how a part's saved text and tier read as the window's
    /// values (<see cref="Read"/>) and are written again from them, refused with the reason past the tool's limits
    /// (<see cref="Write"/>); the window, by default the tool's rows in a <see cref="KwForm"/> filled from the part clicked
    /// (<see cref="Fields"/>, read back by <see cref="Values"/>; the sail and bowsprit tools open their own windows); what
    /// the note line says of the parts that do not follow an edit; and which part a click on something else stands for
    /// (<see cref="Over"/>). The preview draws every kind's parts in their real looks (<see cref="KwEditPreview"/>). Pure but
    /// for the window.
    /// </summary>
    public abstract class KwEditKind
    {
        protected KwEditKind(string tool, string one, string many)
        {
            Tool = tool;
            One = one;
            Many = many;
        }

        public string Tool { get; }

        /// <summary>One part and several, as the player reads them: "rib", "ribs".</summary>
        public string One { get; }
        public string Many { get; }

        /// <summary>The generated part's prefab: "KW_gen_rib".</summary>
        public string Prefab => KwToolPieces.PartPrefix + Tool;

        /// <summary>"rib" for one, "7 ribs" for more.</summary>
        public string Words(int count) => count == 1 ? One : $"{count} {Many}";

        /// <summary>Whether its tool's own window shows the edit (and draws its preview), rather than a form of rows here.</summary>
        public virtual bool OwnWindow => false;

        /// <summary>A click on such a part the tool still takes its own way (a waiting first click, a keel's open end): no edit.</summary>
        public virtual bool Keeps(KwClick click) => false;

        /// <summary>Whether a piece of this prefab is a part this kind edits: its tool's own (a cabin's, also an old castle's).</summary>
        public virtual bool Edits(string prefab) => prefab == Prefab;

        /// <summary>
        /// The part a click on something else stands for, edited as if it were clicked (a railing over the hull or the roof
        /// clicked, <see cref="KwRailEdit"/>); null for none, the click then the tool's own.
        /// </summary>
        public virtual Piece? Over(KwClick click) => null;

        /// <summary>
        /// The edit's parts built again (replace), with whatever the kind does around that in the same undo step (a cabin
        /// brought back to the hull takes the railing there away).
        /// </summary>
        public virtual void Rebuild(Player player, List<KwEditPlan> work, System.Action replace) => replace();

        /// <summary>
        /// What else moves with the planned parts, into follow (a cabin's stack: the cabins on its roof and what stands on
        /// them, <see cref="KwCabinFollow"/>); a plan of a part standing on another changed part may be planned again in place
        /// in plans. Nothing by default.
        /// </summary>
        public virtual void Follow(List<KwEditPlan> plans, List<string> changed, KwEditFollow follow) => follow.Clear();

        /// <summary>A part's values from its saved text and tier, by the window's keys; null when the text does not read.</summary>
        public abstract KwPartValues? Read(string saved, KwBiomeTier tier);

        /// <summary>
        /// The same, knowing whether the part's ZDO saves a tier of its own: a kind whose parts may save none (railings built
        /// before they had one) leaves the tier out of their values, so an edit gives one only when the player picks it.
        /// </summary>
        public virtual KwPartValues? Read(string saved, KwBiomeTier tier, bool tiered) => Read(saved, tier);

        /// <summary>The same for a part standing in the world: a kind whose values hang on where it stands (a hull railing's port and starboard) reads them here.</summary>
        public virtual KwPartValues? Read(Piece piece, string saved, KwBiomeTier tier, bool tiered) => Read(saved, tier, tiered);

        /// <summary>A part's saved text made from its values (its own saved text for what they do not hold); null and why past the tool's limits.</summary>
        public abstract string? Write(KwPartValues values, string saved, out string why);

        /// <summary>What the note line says of the parts that stay as they are when these keys change ("" for nothing).</summary>
        public virtual string Note(List<string> changed, int count) => "";

        /// <summary>What the note line says of a part as planned, from its new text (a cabin: what was changed to fit it); "" for nothing.</summary>
        public virtual string Said(string text) => "";

        /// <summary>Whether OK waits, the window staying open, while every part changed is refused (the cabin's), rather than ending with "Cannot".</summary>
        public virtual bool HoldsRefused => false;

        /// <summary>The window's rows, filled from a part's values and its saved text.</summary>
        public virtual List<KwField> Fields(KwPartValues values, string saved) => new List<KwField>();

        /// <summary>The window's values as the part's (was: the values before, for what the window does not show).</summary>
        public virtual KwPartValues Values(KwFormValues form, KwPartValues was) => was.Copy();

        /// <summary>The tier values hold, for a kind of part the tiers dress; null for the others.</summary>
        public KwBiomeTier? Tier(KwPartValues values) => values.Has(KwBiomeField.Key) ? KwBiomeTiers.Of(values[KwBiomeField.Key]) : null;

        /// <summary>The edit's window, on the part clicked; every change and the end go to the session.</summary>
        public virtual void Open(KwEditSession session)
        {
            KwEditTarget clicked = session.Clicked;
            Func<KwFormValues, bool>? accept = HoldsRefused ? form => session.Accepts(Values(form, session.Now ?? clicked.Values)) : null;
            KwForm.Open(session.Title, Fields(clicked.Values, clicked.Saved),
                form => session.Changed(Values(form, session.Now ?? clicked.Values)),
                form => session.Done(form != null ? Values(form, session.Now ?? clicked.Values) : null), true, accept);
        }

        /// <summary>A number's step in an edit window: the tool's own, or a centimetre when the part's value is not on it.</summary>
        protected static float Step(float value, float step) =>
            Mathf.Abs(value / step - Mathf.Round(value / step)) < 0.001f ? step : 0.01f;

        /// <summary>The "Material" row, opened on the values' tier.</summary>
        protected static KwField Material(KwPart part, KwPartValues values) => KwBiomeField.Field(part, KwBiomeTiers.Of(values[KwBiomeField.Key]));

        /// <summary>The tier the window's "Material" row holds, into the values.</summary>
        protected static void Material(KwPartValues values, KwFormValues form) => values[KwBiomeField.Key] = KwBiomeField.Shown(form).Id;

        /// <summary>
        /// Within a tool's limits, a centimetre of rounding allowed as the saved texts are read; else false, and the reason
        /// with the value and limits put in.
        /// </summary>
        protected static bool Within(float value, float least, float most, string reason, out string why)
        {
            bool within = value >= least - 0.01f && value <= most + 0.01f;
            why = within ? "" : string.Format(reason, KwBpPartText.Num(value), KwBpPartText.Num(least), KwBpPartText.Num(most));
            return within;
        }
    }

    /// <summary>
    /// Every kind of part the tools edit, one per tool. A new tool's edit plugs in here: a <see cref="KwEditKind"/> for its
    /// part, one more line in <see cref="All"/>; <see cref="KwEdit"/> then takes that tool's clicks on its parts.
    /// </summary>
    public static class KwEditKinds
    {
        public static readonly IReadOnlyList<KwEditKind> All = new KwEditKind[]
        {
            new KwKeelEdit(), new KwRibEdit(), new KwPlankingEdit(), new KwDeckEdit(), new KwLadderEdit(), new KwMastEdit(), new KwBowspritEdit(),
            new KwCabinEdit(),
            new KwRailEdit(),
            new KwCapEdit(),
        };

        /// <summary>The kind a tool edits; null for a tool that edits nothing.</summary>
        public static KwEditKind? For(string tool)
        {
            foreach (KwEditKind kind in All)
            {
                if (kind.Tool == tool)
                    return kind;
            }
            return null;
        }
    }
}
