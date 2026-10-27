using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The ship's owner checking a sea edit of its rig (<see cref="KwSeaRig"/>) before it is applied: every new mast,
    /// bowsprit or cap text reads and keeps within its tool's limits (the dock's own edit kinds write it again,
    /// <see cref="KwEditKind.Write"/>); a cap or a bowsprit stands on a keel's post top, one cap and one bowsprit a post; a
    /// change keeps the piece where it stands (a bowsprit may turn); a fallen piece may be removed but not changed (the
    /// hammer's repair puts it back for its cost). A fallen piece removed went over the side and gives nothing back
    /// (<see cref="Refund"/>), so removing and adding one again is never a free repair.
    /// </summary>
    public static class KwSeaRigCheck
    {
        public static string? Why(KwSeaShip ship, KwSeaEdit edit)
        {
            HashSet<int> down = Down(ship);
            foreach (KwSeaPart part in edit.Parts)
            {
                KwBpPiece? old = part.Index >= 0 && part.Index < ship.Blueprint.Pieces.Count ? ship.Blueprint.Pieces[part.Index] : null;
                string? why = Part(ship.Blueprint, part, old, down);
                if (why != null)
                    return why;
            }
            return null;
        }

        // The pieces fallen now, from the ship's ZDO.
        private static HashSet<int> Down(KwSeaShip ship)
        {
            HashSet<int> down = new HashSet<int>();
            if (ship.View == null || !ship.View.IsValid())
                return down;
            foreach (KwRigMark mark in KwRigMarks.Read(ship.View.GetZDO().GetString(KwRigMarks.Hash)))
            {
                if (mark.Type == KwRigMark.Fall)
                    down.Add(mark.Piece);
            }
            return down;
        }

        /// <summary>One part of an edit: why not, or null (a part that is not the rig's is another kind's to check).</summary>
        public static string? Part(KwBlueprint blueprint, KwSeaPart part, KwBpPiece? old, ICollection<int> down)
        {
            if (old != null && KwSeaRig.Is(old.Prefab) && part.Piece != null && down.Contains(part.Index))
                return KwSeaRigWords.Down(KwSeaRigWords.What(old.Prefab));
            KwBpPiece? piece = part.Piece;
            if (piece == null || !KwSeaRig.Is(piece.Prefab))
                return null;
            string? why = Reads(piece);
            if (why == null && old != null && (old.Position - piece.Position).sqrMagnitude > 1e-4f)
                why = KwSeaRigWords.Moved;
            return why ?? (piece.Prefab == KwSailTool.Made ? null : OnPost(blueprint, piece, part.Index));
        }

        // Whether a rig piece's text reads within its tool's limits, written again by the dock's own edit kind.
        private static string? Reads(KwBpPiece piece)
        {
            string what = KwSeaRigWords.What(piece.Prefab);
            string tool = piece.Prefab == KwSailTool.Made ? KwSailTool.Tool : piece.Prefab == KwBowsprit.Made ? KwBowsprit.Tool : KwCapSpec.Tool;
            KwEditKind? kind = KwEditKinds.For(tool);
            string text = KwSeaPiece.Text(piece);
            KwPartValues? values = piece.Prefab == KwSailTool.Made || piece.Prefab == KwBowsprit.Made || piece.Prefab == KwCapSpec.Made
                ? kind?.Read(text, KwBiomeTiers.Of(piece.Data)) : null;
            if (kind == null || values == null)
                return KwSeaRigWords.Unread(what, "its text does not read");
            string why = "";
            return kind.Write(values, text, out why) != null ? null : KwSeaRigWords.Unread(what, why);
        }

        // A cap or a bowsprit: on a keel's post top, the only one of its kind there.
        private static string? OnPost(KwBlueprint blueprint, KwBpPiece piece, int index)
        {
            KwSeaPost? post = KwSeaRigSites.Under(blueprint, piece.Position);
            if (post == null)
                return KwSeaRigWords.NotOnPost;
            bool cap = KwCaps.Is(piece.Prefab);
            System.Func<string, bool> same = cap ? KwCaps.Is : (System.Func<string, bool>)(p => p == KwBowsprit.Made);
            int other = KwSeaRigSites.On(blueprint, post.Top, same, index);
            return other < 0 ? null : cap ? KwSeaRigWords.Capped : KwSeaRigWords.Sprit;
        }

        /// <summary>
        /// The cost worked out (on the owner, and on the asker's own try first): a fallen piece removed gives back nothing of
        /// its own (its cost to put back, <see cref="KwRigPiece.Cost"/>, added back into the refund); the rest of the
        /// difference stands (a bowsprit's jibs lost with a fallen foremast are still given back). The rig as this peer read
        /// it before the edit: on the owner the edit's marks are dropped in the ZDO first, but read back only when it redraws.
        /// </summary>
        public static void Refund(KwSeaShip ship, KwSeaChanges mine, List<(string Item, int Amount)> delta)
        {
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            if (damage == null || damage.Condition == null)
                return;
            HashSet<int> down = new HashSet<int>();
            foreach (KwSeaChange change in mine.All)
            {
                if (damage.Condition.Fallen(change.Index))
                    down.Add(change.Index);
            }
            Refund(mine, down, damage.Parts, delta);
        }

        /// <summary>The same in plain data: each removed piece that was down (as the rig was read before the edit) adds its own cost back.</summary>
        public static void Refund(KwSeaChanges mine, ICollection<int> down, KwRigParts before, List<(string Item, int Amount)> delta)
        {
            foreach (KwSeaChange change in mine.All)
            {
                KwRigPiece? piece = change.Removed && down.Contains(change.Index) ? before.Get(change.Index) : null;
                if (piece == null)
                    continue;
                foreach ((string item, int amount) in piece.Cost)
                    KwBpCost.Add(delta, item, amount);
            }
        }
    }
}
