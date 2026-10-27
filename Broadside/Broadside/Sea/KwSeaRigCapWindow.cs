using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// A post cap's window on a launched ship (<see cref="KwSeaRigWindows"/>): the dock's own cap edit's rows
    /// (<see cref="KwCapEdit"/>: style, length, size against its post, lean, facing), its post measured from the ship's
    /// blueprint (<see cref="KwSeaRigSites.Under"/>), with a Remove button; the cap shown solid in place as it changes. An
    /// old hammer cap reads as its style matched to its post and OK builds it again as the tool's cap, in the cap's tier (one
    /// that saves none: its keel's), as on the dock; the edit's style, length, lean and facing become the tool's.
    /// </summary>
    internal sealed class KwSeaRigCapWindow
    {
        private readonly KwSeaRigSession s;
        private readonly KwCapEdit kind = new KwCapEdit();
        private readonly KwPartValues values;
        private readonly string saved;
        private readonly KwBiomeTier tier;

        private KwSeaRigCapWindow(KwSeaRigSession s, KwPartValues values, string saved, KwBiomeTier tier) =>
            (this.s, this.values, this.saved, this.tier) = (s, values, saved, tier);

        public static bool Open(KwSeaRigSession s, KwBpPiece old)
        {
            KwSeaPost? post = KwSeaRigSites.Under(s.Ship.Blueprint, old.Position);
            KwBiomeTier tier = KwSeaPiece.Tiered(old) || post == null ? KwBiomeTiers.Of(old.Data) : post.Tier;
            KwPartValues? values = Values(old, post, tier);
            if (values == null)
            {
                s.Player.Message(MessageHud.MessageType.Center, KwSeaRigWords.Unread("post cap", "its text does not read"));
                return true;
            }
            KwSeaRigCapWindow window = new KwSeaRigCapWindow(s, values, KwSeaPiece.Text(old), tier);
            List<KwField> fields = window.kind.Fields(values, window.saved);
            fields.Add(KwSeaWindow.RemoveRow(window.Remove));
            KwSeaRigWindows.Begin(s);
            KwForm.Open(KwSeaRigWords.Title("post cap"), fields, window.Changed, window.Done, true, form => window.Plan(form, out _) != null);
            return true;
        }

        // A cap's values against the post it stands on (its own base with none found); an old hammer cap as its style on that post.
        private static KwPartValues? Values(KwBpPiece old, KwSeaPost? post, KwBiomeTier tier)
        {
            if (KwCapSpec.TryParse(KwSeaPiece.Text(old), out KwCapSpec spec))
                return KwCapEdit.Of(spec, post?.Width ?? spec.Width, post?.Depth ?? spec.Depth, tier, false);
            if (!KwCapSpec.Old(old.Prefab, out string style))
                return null;
            KwCapSpec made = KwCapSpec.On(style, post?.Body ?? KwKeel.Standard, 1f);
            return KwCapEdit.Of(made, made.Width, made.Depth, tier, true);
        }

        // The window changed: the cap as it would be built shown solid in place, or why not on the note line.
        private void Changed(KwFormValues form)
        {
            KwBpPiece? next = Plan(form, out string why);
            KwForm.Say(next == null ? why : kind.Note(KwPartValues.Changed(values, kind.Values(form, values)), 1));
            KwSeaRigGhost.Cap(s.Ship, next ?? s.Old!, true);
        }

        private void Done(KwFormValues? form)
        {
            KwBpPiece? next = form != null ? Plan(form, out _) : null;
            if (next != null && KwCapSpec.TryParse(KwSeaPiece.Text(next), out KwCapSpec spec))
                KwCapTool.Keep(spec);
            KwSeaRigWindows.End(s, next);
        }

        // The cap the window's values make (the tool's, in the cap's tier); null and why past the tool's limits.
        private KwBpPiece? Plan(KwFormValues form, out string why)
        {
            string? text = kind.Write(kind.Values(form, values), saved, out why);
            return text != null ? s.Piece(text, tier) : null;
        }

        private void Remove()
        {
            if (s.Index >= 0 && s.Index < s.Ship.Blueprint.Pieces.Count && KwSeaRig.Is(s.Ship.Blueprint.Pieces[s.Index].Prefab))
                KwSeaAsk.Send(s.Player, s.Ship, KwSeaRigPick.Removal(s.Ship, s.Index), s.At);
        }
    }
}
