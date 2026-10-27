using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// One edit's window open (<see cref="KwEdit"/>): its kind, the player, the part clicked and every part it changes (the
    /// clicked one first; with Shift every part of that kind on the dock). The window's first values, as it opened on the
    /// part clicked, are kept; at every change the keys the player changed from them are found
    /// (<see cref="KwPartValues.Changed"/>) and each part planned again from its own values with only those keys taken
    /// (<see cref="KwPartValues.With"/>), so a part keeps everything else of its own (a rib's height when only the material
    /// changed). A part that would pass its tool's limits is left out, and the note line says so, as it says what a ward
    /// left out, what does not follow the edit (<see cref="KwEditKind.Note"/>) and what the part clicked was fitted to
    /// (<see cref="KwEditKind.Said"/>). While the window is open the parts are hidden on this screen (<see cref="KwEditHide"/>)
    /// and the preview shows every part in place in its real look as last planned within its limits, so a refused value
    /// never leaves an empty space (<see cref="KwEditPreview"/>; the sail and bowsprit windows draw their own). A kind that
    /// holds OK while every part changed is refused keeps the window open then (<see cref="Accepts"/>, the cabin's). What
    /// else moves with the parts (<see cref="KwEditKind.Follow"/>: the cabins on an edited cabin's roof and what stands on
    /// them) is planned with them at every change, shown moved in the preview, its pieces hidden too, and built in the same
    /// undo step. OK builds them again (<see cref="KwEditWork"/>) and tells the player, the old parts kept hidden until they
    /// are gone; Cancel, or any other end, changes nothing and shows them again as they were. Local to this player until OK.
    /// </summary>
    public sealed class KwEditSession
    {
        private readonly Player player;
        private readonly List<KwEditTarget> targets;
        private readonly List<KwEditPlan> plans = new List<KwEditPlan>();
        private readonly Dictionary<KwEditTarget, KwEditPlan> good = new Dictionary<KwEditTarget, KwEditPlan>();
        private readonly KwEditFollow follow = new KwEditFollow();
        private readonly int warded;
        private KwPartValues? first;
        private List<string> changed = new List<string>();
        private int refused;
        private string why = "";

        public KwEditSession(Player player, KwEditKind kind, List<KwEditTarget> targets, int warded)
        {
            this.player = player;
            Kind = kind;
            this.targets = targets;
            this.warded = warded;
        }

        public KwEditKind Kind { get; }

        public KwEditTarget Clicked => targets[0];

        /// <summary>The window's values at its last change; null before the first.</summary>
        public KwPartValues? Now { get; private set; }

        /// <summary>"Edit rib", "Edit 7 ribs".</summary>
        public string Title => "Edit " + Kind.Words(targets.Count);

        /// <summary>The parts hidden, the glass preview gone, then the kind's window opened on the part clicked.</summary>
        public void Open()
        {
            KwPreview.Hide();
            KwEditHide.Hide(Kind.Tool, targets.ConvertAll(target => target.Piece));
            Kind.Open(this);
        }

        /// <summary>The window's values changed (the first time: as it opened): every part planned again; what the note line says.</summary>
        public string Changed(KwPartValues values)
        {
            Plan(values);
            string note = Note();
            if (!Kind.OwnWindow)
            {
                KwForm.Say(note);
                Preview();
            }
            return note;
        }

        /// <summary>OK asked with these values: false (the window stays open, the note saying why) while every part changed is refused.</summary>
        public bool Accepts(KwPartValues values)
        {
            Changed(values);
            return refused == 0 || plans.Exists(plan => !plan.Same);
        }

        // The window's values taken (the first ones kept), and each part from its own values with the changed keys taken,
        // written again; the ones past their limits counted; then what moves with them. Nothing changed yet: every part as it is.
        private void Plan(KwPartValues values)
        {
            first ??= values.Copy();
            Now = values.Copy();
            changed = KwPartValues.Changed(first, Now);
            plans.Clear();
            (refused, why) = (0, "");
            foreach (KwEditTarget target in targets)
            {
                KwPartValues merged = target.Values.With(Now!, changed);
                string reason = "";
                string? text = changed.Count == 0 ? target.Saved : Kind.Write(merged, target.Saved, out reason);
                if (text != null)
                    plans.Add(good[target] = new KwEditPlan(target, text, Kind.Tier(merged)));
                else if (refused++ == 0)
                    why = reason;
            }
            Kind.Follow(plans, changed, follow);
        }

        // What the note line says: who takes the change, what was left out and why, what stays as it was.
        private string Note()
        {
            List<string> lines = new List<string>();
            if (refused > 0)
                lines.Add(targets.Count == 1 ? $"Cannot: {why}. Showing the last that can be built." : $"{refused} of {targets.Count} {Kind.Many} left out: {why}.");
            KwEditPlan? mine = plans.Find(plan => plan.Target == Clicked);
            if (mine != null)
                lines.Add(Kind.Said(mine.Text));
            if (warded > 0)
                lines.Add($"{Kind.Words(warded)} left out: a ward you have no access to.");
            if (changed.Count > 0)
                lines.Add(Kind.Note(changed, targets.Count) + (follow.Note.Length > 0 ? " " + follow.Note : ""));
            else if (targets.Count > 1)
                lines.Add($"Only what you change goes to all {targets.Count} {Kind.Many}; each keeps the rest of its own.");
            return string.Join(" ", lines.FindAll(line => line.Length > 0));
        }

        /// <summary>
        /// The window closed: on OK (values) the parts built again, the old ones hidden until gone, and the player told;
        /// cancelled (null), nothing, and the parts shown again as they were.
        /// </summary>
        public void Done(KwPartValues? values)
        {
            KwEdit.Ended(this);
            KwPreview.Hide();
            KwEditPreview.Clear();
            List<Piece> gone = new List<Piece>();
            try
            {
                if (values != null && player != null)
                    Apply(values, gone);
            }
            finally
            {
                KwEditHide.Ended(gone);
            }
        }

        // OK: the parts that change built again (those taken away into gone), and the player told.
        private void Apply(KwPartValues values, List<Piece> gone)
        {
            Plan(values);
            List<KwEditPlan> work = plans.FindAll(plan => !plan.Same);
            if (changed.Count == 0 || work.Count == 0)
            {
                Say(refused > 0 ? $"Cannot: {why}" : "Nothing changed");
                return;
            }
            int done = 0, kept = 0;
            work.AddRange(follow.Moved);
            Kind.Rebuild(player, work, () => done = KwEditWork.Replace(player, Kind.Tool, work, out kept, gone));
            Say(Result(done, kept));
        }

        // "7 ribs changed", with what was left out and why.
        private string Result(int done, int kept)
        {
            string words = Kind.Words(done);
            string text = done == 0 ? "Nothing changed" : char.ToUpperInvariant(words[0]) + words.Substring(1) + " changed";
            if (done > 0 && follow.Told.Length > 0)
                text += "\n" + follow.Told;
            if (refused > 0)
                text += $"\n{refused} left out: {why}";
            if (kept + warded > 0)
                text += $"\n{kept + warded} left out: a ward you have no access to";
            return text;
        }

        /// <summary>
        /// Every part as OK would build it, in place in its real look (made again only when one changes): one whose values
        /// are refused as it was last planned within its limits (at first, as it stands), so none ever vanishes; and what moves
        /// with them where it would stand, those pieces hidden as the parts are (<see cref="KwEditHide.Follow"/>).
        /// </summary>
        private void Preview()
        {
            if (Clicked.Piece == null)
            {
                KwEditPreview.Clear();
                return;
            }
            List<KwEditPlan> shown = targets.ConvertAll(target => good.TryGetValue(target, out KwEditPlan plan) ? plan : new KwEditPlan(target, target.Saved, null));
            KwEditFollow seen = Seen(shown);
            KwEditHide.Follow(seen.Pieces);
            shown.AddRange(seen.Moved.FindAll(plan => !plan.Take));
            string key = Kind.Tool + "|" + string.Join(";", shown.ConvertAll(plan => plan.Text + "#" + (plan.Tier?.Id ?? "") + "@" + plan.Shift.ToString("F3")));
            KwEditPreview.Show(key, shown, Kind.Tool);
        }

        // What moves with the parts shown: the plans' own when every part could be planned (shown as they were planned
        // again), else worked out from the last plans that could.
        private KwEditFollow Seen(List<KwEditPlan> shown)
        {
            if (refused == 0 && plans.Count == shown.Count)
            {
                shown.Clear();
                shown.AddRange(plans);
                return follow;
            }
            KwEditFollow seen = new KwEditFollow();
            Kind.Follow(shown, changed, seen);
            return seen;
        }

        private void Say(string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
