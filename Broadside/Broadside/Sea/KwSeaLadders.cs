using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// Ladders edited on a launched ship where it floats (the user, 2026-10-26: "let's just do it with the ladders, windows,
    /// doors and railings"): the ladder tool adds one under a hatch or down from a deck's or a cabin roof's open edge as on
    /// the dock, a click on one opens its length window with a Remove button, the hammer's remove aimed at one removes it
    /// (<see cref="KwSeaLadderTool"/>). A ladder whose top comes up on a roof railing cuts that railing's gap in the same
    /// edit. The owner checks every ladder's text; every peer makes only the changed ladders' boxes and looks again
    /// (<see cref="KwSeaLadderLook"/>), the ladder over the side being placed again by the core (it keeps clear of ladders
    /// and openings, <see cref="KwSeaRedraw"/>).
    /// </summary>
    public sealed class KwSeaLadders : KwSeaKind
    {
        public const string Made = KwToolPieces.PartPrefix + KwLadderTool.Tool, Kind = "ladder";

        public override string Name => Kind;

        public override bool Owns(KwBpPiece piece) => piece.Prefab == Made;

        public override bool Serves(string tool) => tool == KwLadderTool.Tool;

        /// <summary>Each ladder is drawn on its own with its climb box (<see cref="KwSeaLadderLook"/>), so an edit redraws only it.</summary>
        public override bool DrawsOwn(KwBpPiece piece) => Owns(piece);

        public override bool Click(Player player, string tool, KwSeaAim aim) => KwSeaLadderTool.Click(this, player, aim);

        public override bool Hover(Player player, string tool, KwSeaAim aim) => KwSeaLadderTool.Hover(aim);

        public override void Unhover() => KwSeaLadderTool.Unhover();

        public override KwSeaPick? Pick(KwSeaAim aim) => KwSeaLadderTool.Pick(this, aim);

        public override KwSeaEdit? Removal(KwSeaShip ship, KwSeaPick pick) => Removed(pick.Index, ship.Revision);

        /// <summary>The edit that removes the ladder at a place in the blueprint, asked on a revision (its roof railing's gap stays, as on the dock).</summary>
        public static KwSeaEdit Removed(int index, int revision) =>
            new KwSeaEdit(Kind, revision, "Ladder removed", new List<KwSeaPart> { KwSeaPart.Remove(index) });

        /// <summary>Every ladder the edit makes reads as one, within the ladders' limits; a ladder changed stays a ladder in its place.</summary>
        public override string? Check(KwSeaShip ship, KwSeaEdit edit)
        {
            foreach (KwSeaPart part in edit.Parts)
            {
                KwBpPiece? old = ship != null && part.Index >= 0 && part.Index < ship.Blueprint.Pieces.Count ? ship.Blueprint.Pieces[part.Index] : null;
                if (part.Piece != null && Owns(part.Piece) && !KwLadder.TryLength(KwSeaPiece.Text(part.Piece), out _))
                    return "That ladder does not read";
                if (old != null && Owns(old) && part.Piece != null && (!Owns(part.Piece) || (part.Piece.Position - old.Position).sqrMagnitude > 1e-4f))
                    return "A ladder changed at sea stays a ladder where it is";
            }
            return null;
        }

        public override void Redraw(KwSeaShip ship, KwSeaChanges mine) => KwSeaLadderLook.Redraw(ship, mine);
    }
}
