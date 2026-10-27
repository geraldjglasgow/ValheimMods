using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The bowsprit tool's window (<see cref="KwForm"/>): the bowsprit's length from the post's top to its tip and its rise
    /// over level, how many jibs it sets (none, one or two), their cloth and what is painted on them
    /// (<see cref="KwPatternField"/>; a new pattern builds the window again). The note line says what the jibs set on: the
    /// stay to the foremast found aft of the post (<see cref="KwJibStay"/>) and their sail, or why there are none yet. The
    /// preview shows every change (<see cref="KwBowspritTool.Preview"/>). The window opens with the bowsprit made last; an
    /// edit opens it on a bowsprit already built (<see cref="OpenOn"/>), under the edit's title, its words after the window's
    /// own on the note line, and keeps nothing for next time. Local to this player.
    /// </summary>
    internal static class KwBowspritForm
    {
        private const string Title = "Bowsprit and jibs";
        private static readonly string[] JibLabels = { "None", "One jib", "Two jibs" };
        private static KwBowsprit last = new KwBowsprit(4f, 12f, 1, KwSailCloth.Default);
        private static string title = Title;

        // An edit's hearing of every change, whose words join the note line; null for a new bowsprit.
        private static Func<KwBowsprit, string>? edited;

        /// <summary>On a launched ship: its jibs' stay found in its blueprint, not among the dock's masts (<see cref="KwSeaRigWindows"/>); null on the dock.</summary>
        internal static Func<KwJibStay?>? StayElsewhere;

        /// <summary>A last row the window shows while set (a launched ship's Remove button); null for none.</summary>
        internal static KwField? Extra;

        /// <summary>The bowsprit the window makes.</summary>
        public static KwBowsprit Spec { get; private set; } = last;

        /// <summary>The stay its jibs would set on, found when the window opened; null with no foremast.</summary>
        public static KwJibStay? Stay { get; private set; }

        public static void Open(KwSite site, Action<KwBowsprit?> done) => Start(site, last, Title, null, values => Finish(values, done, true));

        /// <summary>
        /// The window on a bowsprit already built, for an edit (<see cref="KwBowspritEdit"/>): under that title, every change
        /// told to changed (whose words join the note line); OK hands the bowsprit on and keeps nothing for next time.
        /// </summary>
        public static void OpenOn(KwSite site, KwBowsprit spec, string heading, Func<KwBowsprit, string> changed, Action<KwBowsprit?> done) =>
            Start(site, spec, heading, changed, values => Finish(values, done, false));

        // The window opened on a bowsprit (its cloth as the window shows it), the stay its jibs would set on found.
        private static void Start(KwSite site, KwBowsprit from, string heading, Func<KwBowsprit, string>? changed, Action<KwFormValues?> end)
        {
            Spec = from.With(from.Length, from.Rise, from.Jibs, KwClothField.Shown(from.Cloth));
            Stay = StayElsewhere != null ? StayElsewhere() : KwJibStay.OnDock(site.Point, site.Turn, out _);
            (title, edited) = (heading, changed);
            KwForm.Open(title, Fields(), Changed, end, true);
        }

        private static List<KwField> Fields()
        {
            List<KwField> fields = new List<KwField>
            {
                KwField.Number("length", "Length", Spec.Length, KwBowsprit.MinLength, KwBowsprit.MaxLength, 0.25f),
                KwField.Number("rise", "Rise", Spec.Rise, 0f, KwBowsprit.MaxRise, 1f, "°"),
                KwField.Choice("jibs", "Jibs", JibLabels, Spec.Jibs),
                KwClothField.Field("cloth", "Jib cloth", Spec.Cloth),
            };
            fields.AddRange(KwPatternField.Fields(Spec.Pattern, "Jib pattern"));
            if (Extra != null)
                fields.Add(Extra);
            return fields;
        }

        // The rows' values taken; a new pattern builds the window again (its rows follow it), which reports them again.
        private static void Changed(KwFormValues values)
        {
            KwSailPattern was = Spec.Pattern, pattern = KwPatternField.Read(values, was);
            Spec = new KwBowsprit(values.Number("length"), values.Number("rise"), values.ChoiceIndex("jibs"), KwClothField.Read(values, "cloth"), pattern);
            if (pattern.Kind != was.Kind)
            {
                KwForm.Refill(title, Fields());
                return;
            }
            string edit = edited != null ? edited(Spec) : "";
            KwForm.Say((Note() + " " + KwPatternField.Note(pattern) + " " + edit).Trim());
            KwBowspritTool.Preview();
        }

        // What the jibs set on, or why there are none.
        private static string Note()
        {
            if (Spec.Jibs == 0)
                return "";
            if (Stay == null)
                return "No mast stands aft of the post on the bowsprit's line: its jibs set once one does.";
            List<KwJibCut> cuts = KwJibPlan.Of(Spec, Stay);
            if (cuts.Count == 0)
                return "The foremast is too near or too low for a jib: lengthen the bowsprit, or raise or move the mast.";
            string fewer = cuts.Count < Spec.Jibs ? " Room for one jib only." : "";
            return $"Jibs on a stay to the foremast {-Stay.Foot.x:0.#} m aft: {KwJibPlan.Area(Spec, Stay):0.#} m² of sail, set and furled with its sails.{fewer}";
        }

        // OK: the window's last values taken, kept for next time (not an edit's), the bowsprit handed on; cancelled: nothing.
        private static void Finish(KwFormValues? values, Action<KwBowsprit?> done, bool keep)
        {
            if (values != null)
                Changed(values);
            edited = null;
            if (values != null && keep)
                last = Spec;
            done(values != null ? Spec : (KwBowsprit?)null);
        }
    }
}
