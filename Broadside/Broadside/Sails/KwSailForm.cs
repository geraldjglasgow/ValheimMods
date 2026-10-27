using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The sail tool's window (<see cref="KwForm"/>). At the top the mast's height, and under it the buttons of the window's
    /// two pages. "Sails": a quick button for each sail on the mast, by name (the course, topsail, topgallant, royal, a gaff
    /// or triangle sail), the one picked lit, and "+ Add sail" and "Remove sail" (<see cref="KwSailDraft"/>); then the
    /// picked sail's own rows (<see cref="SailRows"/>: its shape, width, height, cloth and what is painted on it,
    /// <see cref="KwPatternField"/>). "Tops and pennant": the mast's
    /// crow's nest, fighting top and pennant (<see cref="KwSailTopsForm"/>). Every change places the sails to fit the mast
    /// again (<see cref="KwMastSpec.Fit"/>): what had to be cut, or the mast raised, or a platform set among the sails
    /// (<see cref="KwMastTopFit"/>), is said on the window's note line, and the preview shows the whole mast as it is
    /// (<see cref="KwSailTool.Preview"/>). Picking a page, picking, adding or removing a sail, or changing one's shape or
    /// pattern, builds the window again. A sail's shape offers only what the mast has room for: square while it has fewer than four,
    /// along the ship while no other sail is. The window opens on its first page with the mast made last; an edit opens it on
    /// a mast already built (<see cref="OpenOn"/>), under the edit's title, its words after the window's own on the note line,
    /// and keeps nothing for next time. Local to this player.
    /// </summary>
    internal static class KwSailForm
    {
        private const string Title = "Mast and sails";
        private static readonly string[] ShapeLabels = { "Square", "Triangle", "Gaff" };
        private static readonly string[] Edits = { "+ Add sail", "Remove sail" };
        private static readonly string[] Pages = { "Sails", "Tops and pennant" };

        private static KwSailDraft draft = Default();
        private static KwSailDraft? last;
        private static List<int> shapes = new List<int>();
        private static string told = "", title = Title;
        private static int page;

        // An edit's hearing of every change, whose words join the note line; null for a new mast.
        private static Func<KwMastSpec, string>? edited;

        /// <summary>A last row the window shows while set (a launched ship's Remove button); null for none.</summary>
        internal static KwField? Extra;

        /// <summary>The mast the window makes, placed to fit, and its saved text.</summary>
        public static KwMastSpec Mast { get; private set; } = draft.Fitted;

        public static string Saved { get; private set; } = "";

        public static void Open(Action<KwMastSpec?> done)
        {
            draft = (last ?? Default()).Copy();
            Start(Title, null, values => Finish(values, done, true));
        }

        /// <summary>
        /// The window on a mast already built, for an edit (<see cref="KwMastEdit"/>): under that title, every change told to
        /// changed (whose words join the note line); OK hands the mast on and keeps nothing for next time.
        /// </summary>
        public static void OpenOn(KwMastSpec mast, string heading, Func<KwMastSpec, string> changed, Action<KwMastSpec?> done)
        {
            draft = new KwSailDraft(mast.Mast, new List<KwSailSpec>(mast.Sails)) { Tops = mast.Tops };
            Start(heading, changed, values => Finish(values, done, false));
        }

        // The draft settled, the window opened on its first page.
        private static void Start(string heading, Func<KwMastSpec, string>? changed, Action<KwFormValues?> end)
        {
            draft.Settle();
            (told, page, title, edited) = ("", 0, heading, changed);
            KwForm.Open(title, Fields(), Changed, end, true);
        }

        // An 8 m mast with a 6 by 5 m raw hide course.
        private static KwSailDraft Default() =>
            new KwSailDraft(8f, new List<KwSailSpec> { new KwSailSpec(0, KwSailSpec.MaxMast, 6f, 5f, KwSailCloth.Default, 0f, 0) });

        // The mast's height and the page buttons, then the page's own rows.
        private static List<KwField> Fields()
        {
            List<KwField> fields = new List<KwField>
            {
                KwField.Number("mast", "Mast height", draft.Mast, KwSailSpec.MinMast, KwSailSpec.MaxMast, 0.5f),
                KwField.Buttons("page", "", Pages, page, PickPage, 2),
            };
            fields.AddRange(page == 0 ? SailPage() : KwSailTopsForm.Fields(draft));
            if (Extra != null)
                fields.Add(Extra);
            return fields;
        }

        // The sails page: a button for each sail, the add and remove buttons, then the picked sail's rows.
        private static List<KwField> SailPage()
        {
            KwMastSpec mast = draft.Fitted;
            string[] names = new string[mast.Sails.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = mast.Name(i);
            List<KwField> fields = new List<KwField>
            {
                KwField.Buttons("sails", "", names, draft.Picked, PickSail, 4),
                KwField.Buttons("edit", "", Edits, -1, Edit, 2),
            };
            fields.AddRange(SailRows());
            return fields;
        }

        // The picked sail's own rows, its cloth as the window shows it (an unknown one as plain), then its pattern's. A new
        // field of each sail's own is one more row here, read back in Asked and saved in the sail's part (KwSailSpec.Part).
        private static List<KwField> SailRows()
        {
            KwSailSpec pick = draft.Sails[draft.Picked] = draft.Pick.Dressed(KwClothField.Shown(draft.Pick.Cloth));
            shapes = draft.Shapes();
            List<KwField> rows = new List<KwField>
            {
                KwField.Choice("shape", "Shape", shapes.ConvertAll(s => ShapeLabels[s]).ToArray(), shapes.IndexOf(pick.Shape)),
                KwField.Number("width", "Sail width", pick.Width, KwSailSpec.MinSize, KwSailSpec.MaxSize, 0.25f),
                KwField.Number("height", "Sail height", pick.Height, KwSailSpec.MinSize, KwSailSpec.MaxSize, 0.25f),
                KwClothField.Field("cloth", "Cloth", pick.Cloth),
            };
            rows.AddRange(KwPatternField.Fields(pick.Pattern, "Pattern"));
            return rows;
        }

        // The picked sail as its rows have it (SailRows), not yet placed on the mast.
        private static KwSailSpec Asked(KwFormValues values, KwSailSpec was)
        {
            int pick = values.ChoiceIndex("shape"), shape = pick >= 0 && pick < shapes.Count ? shapes[pick] : was.Shape;
            KwSailPattern pattern = KwPatternField.Read(values, was.Pattern);
            return new KwSailSpec(shape, KwSailSpec.MaxMast, values.Number("width"), values.Number("height"), KwClothField.Read(values, "cloth"), 0f, 0, pattern);
        }

        // A field changed: the mast takes its height, then the page's rows their values.
        private static void Changed(KwFormValues values)
        {
            float mast = values.Number("mast");
            if (mast != draft.Mast)
                told = "";
            draft.Mast = mast;
            if (page == 0)
            {
                SailChanged(values);
                return;
            }
            KwSailTopsForm.Read(values, draft);
            Show();
        }

        // The picked sail takes its rows' values; a new shape or pattern builds the window again.
        private static void SailChanged(KwFormValues values)
        {
            KwSailSpec was = draft.Pick, asked = Asked(values, was);
            if (asked.Part() != was.Part())
                told = "";
            draft.Sails[draft.Picked] = asked;
            if (asked.Shape != was.Shape || asked.Pattern.Kind != was.Pattern.Kind)
                Rebuild();
            else
                Show();
        }

        // The mast placed to fit again: its notes (or what the last button did) on the window, the preview redrawn.
        private static void Show()
        {
            Mast = draft.Fitted;
            Saved = Mast.ToString();
            string paint = page == 0 ? KwPatternField.Note(draft.Pick.Pattern) : "";
            string note = (Mast.Note + " " + KwMastTopFit.Note(Mast)).Trim(), edit = edited != null ? edited(Mast) : "";
            KwForm.Say(((note.Length > 0 ? note : told) + " " + paint + " " + edit).Trim());
            KwSailTool.Preview();
        }

        // The window built again for the page and sail picked, the sails taken as placed (the form then reports its values: Show).
        private static void Rebuild()
        {
            draft.Settle();
            KwForm.Refill(title, Fields());
        }

        private static void PickPage(int index)
        {
            page = index == 1 ? 1 : 0;
            told = "";
            Rebuild();
        }

        private static void PickSail(int index)
        {
            draft.Picked = Math.Max(0, Math.Min(index, draft.Sails.Count - 1));
            told = "";
            Rebuild();
        }

        // "+ Add sail" (0) or "Remove sail" (1).
        private static void Edit(int button)
        {
            told = button == 0 ? draft.Add() : draft.Remove();
            Rebuild();
        }

        // OK: the window's last values taken, kept for next time (not an edit's), the mast handed on; cancelled: nothing.
        private static void Finish(KwFormValues? values, Action<KwMastSpec?> done, bool keep)
        {
            if (values != null)
                Changed(values);
            edited = null;
            if (values != null && keep)
                last = draft.Copy();
            done(values != null ? Mast : null);
        }
    }
}
