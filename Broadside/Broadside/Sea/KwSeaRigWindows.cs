using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The rig's windows on a launched ship, on the editing player's game: the dock's own sail window
    /// (<see cref="KwSailForm"/>) and bowsprit window (<see cref="KwBowspritForm"/>, its jibs cut to the stay to the ship's
    /// own foremast) for a new piece or one already there, and the cap's (<see cref="KwSeaRigCapWindow"/>); a piece already
    /// there hidden on this screen while its window shows its new version on the ship (<see cref="KwSeaRigGhost"/>), with a
    /// Remove button (<see cref="KwSeaWindow.RemoveRow"/>). OK asks the ship's owner (<see cref="KwSeaAsk"/>), which takes
    /// the cost or gives the difference back. The game's building rules hold at the piece (<see cref="KwSeaRules"/>); a
    /// fallen piece is put back with the hammer's repair or removed, never changed. One window at a time.
    /// </summary>
    internal static class KwSeaRigWindows
    {
        private static KwSeaRigSession? open;

        /// <summary>Whether a rig window is open on a launched ship.</summary>
        public static bool IsOpen => open != null && KwForm.IsOpen;

        /// <summary>A rig piece's own window: a mast's, a bowsprit's or a cap's.</summary>
        public static bool Open(Player player, KwSeaShip ship, int index)
        {
            KwBpPiece old = ship.Blueprint.Pieces[index];
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            string? why = KwSeaRules.Why(player, ship, ship.World(old.Position));
            if (why == null && damage != null && damage.Condition != null && damage.Condition.Fallen(index))
                why = KwSeaRigWords.Down(KwSeaRigWords.What(old.Prefab));
            if (why != null)
                return Say(player, why);
            string prefab = KwCaps.Is(old.Prefab) ? KwCapSpec.Made : old.Prefab;
            KwSeaRigSession s = new KwSeaRigSession(player, ship, index, prefab, old.Position, old.Rotation);
            if (old.Prefab == KwSailTool.Made)
                return KwMastSpec.TryParse(KwSeaPiece.Text(old), out KwMastSpec mast) ? Mast(s, mast) : Say(player, KwSeaRigWords.Unread("mast", "its text does not read"));
            if (old.Prefab == KwBowsprit.Made)
                return KwBowsprit.TryParse(KwSeaPiece.Text(old), out KwBowsprit spec) ? Sprit(s, spec, KwKeel.Standard) : Say(player, KwSeaRigWords.Unread("bowsprit", "its text does not read"));
            return KwSeaRigCapWindow.Open(s, old);
        }

        /// <summary>A new mast's window at a place of the ship's frame, opening with the mast made last.</summary>
        public static bool NewMast(Player player, KwSeaShip ship, Vector3 at, Quaternion turn)
        {
            string? why = KwSeaRules.Why(player, ship, ship.World(at));
            return why != null ? Say(player, why) : Mast(new KwSeaRigSession(player, ship, -1, KwSailTool.Made, at, turn), null);
        }

        /// <summary>A new bowsprit's window on a post's top, running out along its keel the way the turn points.</summary>
        public static bool NewSprit(Player player, KwSeaShip ship, KwSeaPost post, Quaternion turn)
        {
            string? why = KwSeaRules.Why(player, ship, ship.World(post.Top));
            return why != null ? Say(player, why) : Sprit(new KwSeaRigSession(player, ship, -1, KwBowsprit.Made, post.Top, turn), null, post.Body);
        }

        // The sail window on a mast there (null: a new mast's).
        private static bool Mast(KwSeaRigSession s, KwMastSpec? mast)
        {
            Begin(s);
            KwSailTool.Elsewhere = spec => KwSeaRigGhost.Mast(s.Ship, s.Piece(spec.ToString()), s.Index);
            KwSailForm.Extra = s.Index >= 0 ? KwSeaWindow.RemoveRow(() => Remove(s)) : null;
            Action<KwMastSpec?> done = made => End(s, made != null ? s.Piece(made.ToString()) : null);
            if (mast == null)
                KwSailForm.Open(done);
            else
                KwSailForm.OpenOn(mast, KwSeaRigWords.Title("mast"), _ => "", done);
            return true;
        }

        // The bowsprit window on a bowsprit there (null: a new one's), its jibs cut to the stay to the ship's own foremast.
        private static bool Sprit(KwSeaRigSession s, KwBowsprit? spec, KwKeelBody body)
        {
            Begin(s);
            KwBowspritForm.StayElsewhere = () => KwJibStay.InBlueprint(s.Ship.Blueprint, new KwBpPiece(KwBowsprit.Made, s.At, s.Turn, new List<KwBpValue>()));
            KwBowspritTool.Elsewhere = (now, stay) => KwSeaRigGhost.Sprit(s.Ship, s.Piece(now.ToString()), now, stay);
            KwBowspritForm.Extra = s.Index >= 0 ? KwSeaWindow.RemoveRow(() => Remove(s)) : null;
            KwSite site = new KwSite(s.At, s.Turn, false, body);
            Action<KwBowsprit?> done = made => End(s, made.HasValue ? s.Piece(made.Value.ToString()) : null);
            if (spec == null)
                KwBowspritForm.Open(site, done);
            else
                KwBowspritForm.OpenOn(site, spec.Value, KwSeaRigWords.Title("bowsprit"), _ => "", done);
            return true;
        }

        /// <summary>A window opens: the old piece hidden on this screen, the ghost held while it is open.</summary>
        public static void Begin(KwSeaRigSession s)
        {
            open = s;
            KwSeaTools.Unhover();
            KwSeaGhost.Held = true;
            if (s.Old != null)
                KwSeaHide.Hide(s.Renderers());
        }

        /// <summary>The window closed: everything shown again; with a new piece (OK) the change asked of the owner.</summary>
        public static void End(KwSeaRigSession s, KwBpPiece? next)
        {
            if (open != s)
                return;
            open = null;
            (KwSailTool.Elsewhere, KwSailForm.Extra, KwBowspritTool.Elsewhere, KwBowspritForm.StayElsewhere, KwBowspritForm.Extra) = (null, null, null, null, null);
            KwSeaGhost.Held = false;
            KwSeaRigGhost.Clear();
            bool same = next != null && s.Old != null && KwSeaPiece.Same(next, s.Old);
            if (same)
                Say(s.Player, KwSeaWords.Unchanged);
            if (next != null && !same && KwSeaAsk.Send(s.Player, s.Ship, Edit(s.Ship, s.Index, next, KwSeaRigWords.What(next.Prefab)), s.At))
                KwSeaHide.Until(s.Ship);
            else
                KwSeaHide.Show();
        }

        /// <summary>The edit adding a piece (index -1) or changing the piece at an index; done its words (else "Mast built", "Mast changed").</summary>
        public static KwSeaEdit Edit(KwSeaShip ship, int index, KwBpPiece piece, string what, string? done = null)
        {
            KwSeaPart part = index < 0 ? KwSeaPart.Add(piece) : KwSeaPart.Change(index, piece);
            done ??= index < 0 ? KwSeaRigWords.Built(what) : KwSeaRigWords.Changed(what);
            return new KwSeaEdit(KwSeaRig.Kind, ship.Revision, done, new List<KwSeaPart> { part });
        }

        // The window's Remove button (the window is closed by then).
        private static void Remove(KwSeaRigSession s)
        {
            if (s.Index >= 0 && s.Index < s.Ship.Blueprint.Pieces.Count && KwSeaRig.Is(s.Ship.Blueprint.Pieces[s.Index].Prefab))
                KwSeaAsk.Send(s.Player, s.Ship, KwSeaRigPick.Removal(s.Ship, s.Index), s.At);
        }

        private static bool Say(Player player, string text)
        {
            player.Message(MessageHud.MessageType.Center, text);
            return true;
        }
    }

    /// <summary>One rig window on a launched ship: whose, on which ship, which piece (-1: a new one) and where, what it makes.</summary>
    internal sealed class KwSeaRigSession
    {
        public KwSeaRigSession(Player player, KwSeaShip ship, int index, string prefab, Vector3 at, Quaternion turn) =>
            (Player, Ship, Index, Prefab, At, Turn) = (player, ship, index, prefab, at, turn);

        public Player Player { get; }
        public KwSeaShip Ship { get; }
        public int Index { get; }

        /// <summary>The prefab it makes: a mast, a bowsprit, the tool's cap (an old hammer cap is built again as one).</summary>
        public string Prefab { get; }
        public Vector3 At { get; }
        public Quaternion Turn { get; }

        /// <summary>The piece there now; null for a new one.</summary>
        public KwBpPiece? Old => Index >= 0 && Index < Ship.Blueprint.Pieces.Count ? Ship.Blueprint.Pieces[Index] : null;

        /// <summary>The piece a text makes: the old one with it (its other values kept), or a new one here.</summary>
        public KwBpPiece Piece(string text, KwBiomeTier? tier = null)
        {
            KwBpPiece? old = Old;
            return old != null && old.Prefab == Prefab ? KwSeaPiece.With(old, text, tier) : KwSeaPiece.Make(Prefab, At, Turn, text, tier);
        }

        /// <summary>The old piece's renderers on this screen: its rig look and its fitting (sails, ropes, jibs).</summary>
        public List<Renderer> Renderers()
        {
            List<Renderer> all = new List<Renderer>();
            KwBpPiece? old = Old;
            Transform? look = Ship.Visual.Find("rig" + Index), fitting = old != null ? KwSeaRigLayers.Fitting(Ship.Fittings, old) : null;
            foreach (Transform? part in new[] { look, fitting })
            {
                if (part != null)
                    all.AddRange(part.GetComponentsInChildren<Renderer>(true));
            }
            return all;
        }
    }
}
