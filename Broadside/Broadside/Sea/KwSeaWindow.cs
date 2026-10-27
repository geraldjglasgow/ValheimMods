using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A part's edit window on a launched ship: the dock's own window of its kind (<see cref="KwEditKind"/>: its rows filled
    /// from the blueprint piece's text and tier, its values written back to a text, refused past the tool's limits), with a
    /// Remove button (<see cref="RemoveRow"/>) when the kind removes parts. While it is open the part is hidden on this
    /// screen (<see cref="KwSeaHide"/>) and its new version shown solid on the ship (<see cref="KwSeaGhost"/>); OK asks the
    /// ship's owner for the change (<see cref="KwSeaAsk"/>). The tier is given only when the piece saves one or the
    /// player picked another (a railing built before railings had one keeps none).
    /// </summary>
    public sealed class KwSeaWindow
    {
        private static KwSeaWindow? open;
        private readonly Player player;
        private readonly KwSeaShip ship;
        private readonly int index;
        private readonly KwEditKind kind;
        private readonly KwPartValues values;
        private readonly string sea, done, saved;
        private readonly Vector3 point;
        private KwPartValues? shown;

        private KwSeaWindow(Player player, KwSeaShip ship, int index, KwEditKind kind, KwPartValues values, string sea, string done, Vector3 point)
        {
            (this.player, this.ship, this.index, this.kind, this.values) = (player, ship, index, kind, values);
            (this.sea, this.done, this.point) = (sea, done, point);
            saved = KwSeaPiece.Text(ship.Blueprint.Pieces[index]);
        }

        private static bool held;

        /// <summary>Whether a sea edit's window is open (the tools' hover waits meanwhile).</summary>
        public static bool IsOpen => (open != null || held) && KwForm.IsOpen;

        /// <summary>
        /// Any other sea window (a new part's, a kind's own) opened or ended: the tools' hover waits and the ghost is kept
        /// while it is open; ended, the ghost goes.
        /// </summary>
        public static void Hold(bool on)
        {
            (held, KwSeaGhost.Held) = (on, on);
            if (on)
                KwSeaTools.Unhover();
            else
                KwSeaGhost.Clear();
        }

        /// <summary>
        /// The window on a piece: its kind's rows from these values (read by the caller, <see cref="KwEditKind.Read(string, KwBiomeTier, bool)"/>),
        /// the renderers hidden meanwhile, remove (or null) behind a Remove button, sea the sea kind's name, done what OK says.
        /// </summary>
        public static void Open(Player player, KwSeaShip ship, int index, KwEditKind kind, KwPartValues values, string title,
            IEnumerable<Renderer> hide, Action? remove, string sea, string done, Vector3 point)
        {
            KwSeaWindow window = new KwSeaWindow(player, ship, index, kind, values, sea, done, point);
            List<KwField> fields = kind.Fields(values, window.saved);
            if (remove != null)
                fields.Add(RemoveRow(remove));
            KwSeaTools.Unhover();
            KwSeaHide.Hide(hide);
            (open, KwSeaGhost.Held) = (window, true);
            KwForm.Open(title, fields, window.Changed, window.Done, true, window.Accepts);
        }

        /// <summary>A "Remove" button row for any <see cref="KwForm"/> window: the window closed, then remove run.</summary>
        public static KwField RemoveRow(Action remove) =>
            KwField.Buttons("kw_sea_remove", "", new[] { "Remove" }, -1, _ =>
            {
                KwForm.Close();
                KwShipBody.Step("Broadside sea remove", remove);
            });

        // The window changed: the new version shown on the ship, or why not on the note line.
        private void Changed(KwFormValues form)
        {
            KwBpPiece? next = Plan(form, out string why);
            KwForm.Say(next == null ? why : "");
            KwBpPiece now = next ?? ship.Blueprint.Pieces[index];
            KwSeaGhost.Show(ship, "edit|" + index + "|" + KwSeaPiece.Text(now) + "|" + KwBiomeTiers.Of(now.Data).Id, new[] { now }, true);
        }

        private bool Accepts(KwFormValues form) => Plan(form, out _) != null;

        // OK or cancel: the part shown again; on OK the change asked of the owner.
        private void Done(KwFormValues? form)
        {
            (open, KwSeaGhost.Held) = (null, false);
            KwSeaGhost.Clear();
            KwBpPiece? next = form != null ? Plan(form, out _) : null;
            bool same = next == null || index >= ship.Blueprint.Pieces.Count || KwSeaPiece.Same(next, ship.Blueprint.Pieces[index]);
            if (form != null && next != null && same)
                player.Message(MessageHud.MessageType.Center, KwSeaWords.Unchanged);
            if (same || !KwSeaAsk.Send(player, ship, new KwSeaEdit(sea, ship.Revision, done, new List<KwSeaPart> { KwSeaPart.Change(index, next!) }), point))
                KwSeaHide.Show();
            else
                KwSeaHide.Until(ship);
        }

        // The piece the window's values make; null and why past the tool's limits.
        private KwBpPiece? Plan(KwFormValues form, out string why)
        {
            KwPartValues now = kind.Values(form, shown ?? values);
            shown ??= now;
            string? text = kind.Write(now, saved, out why);
            if (text == null)
                return null;
            KwBpPiece piece = ship.Blueprint.Pieces[index];
            KwBiomeTier? tier = kind.Tier(now);
            bool give = tier != null && (KwSeaPiece.Tiered(piece) || kind.Tier(shown) != tier);
            return KwSeaPiece.With(piece, text, give ? tier : null);
        }
    }
}
